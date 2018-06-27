#region Dapplo 2016-2018 - GNU Lesser General Public License

// Dapplo - building blocks for .NET applications
// Copyright (C) 2016-2018 Dapplo
// 
// For more information see: http://dapplo.net/
// Dapplo repositories are hosted on GitHub: https://github.com/dapplo
// 
// This file is part of Dapplo.CaliburnMicro
// 
// Dapplo.CaliburnMicro is free software: you can redistribute it and/or modify
// it under the terms of the GNU Lesser General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// Dapplo.CaliburnMicro is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU Lesser General Public License for more details.
// 
// You should have a copy of the GNU Lesser General Public License
// along with Dapplo.CaliburnMicro. If not, see <http://www.gnu.org/licenses/lgpl.txt>.

#endregion

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using Dapplo.Addons.Bootstrapper.Extensions;
using Dapplo.Addons.Bootstrapper.Internal;
using Dapplo.Log;

namespace Dapplo.Addons.Bootstrapper.Resolving
{
    /// <summary>
    /// This class supports the resolving of assemblies
    /// </summary>
    public class AssemblyResolver : IDisposable, IAssemblyResolver
    {
        private readonly ApplicationConfig _applicationConfig;
        private static readonly LogSource Log = new LogSource();
        private readonly Regex _assemblyResourceNameRegex;
        private readonly Regex _assemblyFilenameRegex;
        private readonly ISet<string> _resolving = new HashSet<string>();
        private readonly ISet<string> _assembliesToDeleteAtExit = new HashSet<string>();

        /// <summary>
        /// A regex with all the assemblies which we should ignore
        /// </summary>
        public Regex AssembliesToIgnore { get; } = new Regex(@"^(autofac.*|microsoft\..*|mscorlib|UIAutomationProvider|PresentationFramework|PresentationCore|WindowsBase|system.*|.*resources)$", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        /// <summary>
        /// A dictionary with all the loaded assemblies, for caching and analysing
        /// </summary>
        public IDictionary<string, AssemblyLocationInformation> AvailableAssemblies { get; } = new ConcurrentDictionary<string, AssemblyLocationInformation>(StringComparer.OrdinalIgnoreCase);


        /// <summary>
        /// A dictionary with all the loaded assemblies, for caching and analysing
        /// </summary>
        public IDictionary<string, Assembly> LoadedAssemblies { get; } = new ConcurrentDictionary<string, Assembly>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Gives access to the resources in assemblies
        /// </summary>
        public IResourceProvider Resources { get; }

        /// <summary>
        /// Specify if embedded assemblies written to disk before using will be removed again when the process exits
        /// </summary>
        public bool CleanupAfterExit { get; set; } = true;

        /// <summary>
        /// The constructor of the Assembly Resolver
        /// </summary>
        /// <param name="applicationConfig">ApplicationConfig</param>
        public AssemblyResolver(ApplicationConfig applicationConfig)
        {
            _applicationConfig = applicationConfig;
            // setup the regex
            var regexExtensions = string.Join("|", applicationConfig.Extensions.Select(e => e.Replace(".", @"\.")));
            _assemblyResourceNameRegex = new Regex($@"^(costura\.)*(?<assembly>.*)({regexExtensions})$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
            _assemblyFilenameRegex = new Regex($@".*\\(?<assembly>.*)({regexExtensions})$", RegexOptions.Compiled | RegexOptions.IgnoreCase);

            Resources = new ManifestResources(simpleAssemblyName => LoadedAssemblies.ContainsKey(simpleAssemblyName) ? LoadedAssemblies[simpleAssemblyName] : null);

            foreach (var loadedAssembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                LoadedAssemblies[loadedAssembly.GetName().Name] = loadedAssembly;
            }

            // Register assembly loading
            AppDomain.CurrentDomain.AssemblyLoad += AssemblyLoad;
            // Register assembly resolving
            AppDomain.CurrentDomain.AssemblyResolve += AssemblyResolve;
            AppDomain.CurrentDomain.ProcessExit += (sender, args) => RemoveCopiedAssemblies();

            ScanForAssemblies();
        }

        /// <summary>
        /// Do the one time scan of all the assemblies
        /// </summary>
        private void ScanForAssemblies()
        {
            var assemblies = new HashSet<AssemblyLocationInformation>();
            if (_applicationConfig.ScanForEmbeddedAssemblies)
            {
                foreach (var loadedAssembly in LoadedAssemblies.Where(pair => !AssembliesToIgnore.IsMatch(pair.Key)).Select(pair => pair.Value).ToList())
                {
                    string[] resources;
                    try
                    {
                        resources = Resources.GetCachedManifestResourceNames(loadedAssembly);
                    }
                    catch (Exception ex)
                    {
                        Log.Warn().WriteLine(ex, "Couldn't retrieve resources from {0}", loadedAssembly.GetName().Name);
                        continue;
                    }
                    foreach (var resource in resources)
                    {
                        var resourceMatch = _assemblyResourceNameRegex.Match(resource);
                        if (resourceMatch.Success)
                        {
                            assemblies.Add(new AssemblyLocationInformation(resourceMatch.Groups["assembly"].Value, loadedAssembly, resource));
                        }
                    }
                }
            }

            foreach (var fileLocation in FileLocations.Scan(_applicationConfig.ScanDirectories, _assemblyFilenameRegex, SearchOption.TopDirectoryOnly))
            {
                assemblies.Add(new AssemblyLocationInformation(fileLocation.Item2.Groups["assembly"].Value, fileLocation.Item1));
            }

            // Reduce step 1) Take from the double assemblies only those which are embedded & on the file system in the probing path
            foreach (var assemblyGroup in assemblies.GroupBy(information => information.Name).ToList())
            {
                var groupList = assemblyGroup.ToList();
                if (groupList.Count <=1)
                {
                    continue;
                }

                // Remove filesystem assemblies from the list which are not in the AssemblyResolveDirectories
                var unneededAssemblies = groupList.Where(info => !info.IsEmbedded && !info.IsOnProbingPath).ToList();
                if (groupList.Count - unneededAssemblies.Count < 1)
                {
                    continue;
                }

                foreach (var unneededAssemblyInformation in unneededAssemblies)
                {
                    assemblies.Remove(unneededAssemblyInformation);
                }
            }

            // Reduce step 2)
            foreach (var assemblyGroup in assemblies.GroupBy(information => information.Name).ToList())
            {
                var groupList = assemblyGroup.ToList();
                if (groupList.Count <=1)
                {
                    continue;
                }
                // Remove assemblies which are older
                foreach (var unneededAssemblyInformation in groupList.OrderBy(info => info.FileDate).Skip(1).ToList())
                {
                    assemblies.Remove(unneededAssemblyInformation);
                }
            }

            // Create the assembly locations
            foreach (var assemblyLocationInformation in assemblies)
            {
                AvailableAssemblies[assemblyLocationInformation.Name] = assemblyLocationInformation;
            }
        }

        /// <summary>
        /// Load a named assembly
        /// </summary>
        /// <param name="assemblyName">string</param>
        /// <returns>Assembly</returns>
        public Assembly LoadAssembly(string assemblyName)
        {
            // Check if the simple name can be found in the cache
            if (LoadedAssemblies.TryGetValue(assemblyName, out var assembly))
            {
                Log.Info().WriteLine("Returned {0} from cache.", assemblyName);
                return assembly;
            }

            if (AvailableAssemblies.TryGetValue(assemblyName, out var assemblyLocationInformation))
            {
                return LoadAssembly(assemblyLocationInformation);
            }

            return Assembly.Load(assemblyName);
        }

        /// <summary>
        /// Load an assembly from the specified location
        /// </summary>
        /// <param name="assemblyLocationInformation">AssemblyLocationInformation</param>
        /// <returns>Assembly</returns>
        public Assembly LoadAssembly(AssemblyLocationInformation assemblyLocationInformation)
        {
            // Check if the simple name can be found in the cache
            if (LoadedAssemblies.TryGetValue(assemblyLocationInformation.Name, out var assembly))
            {
                Log.Info().WriteLine("Returned {0} from cache.", assemblyLocationInformation.Name);
                return assembly;
            }

            if (assemblyLocationInformation.IsEmbedded)
            {
                return LoadEmbeddedAssembly(assemblyLocationInformation);
            }
            // Load from file
            return LoadFromFile(assemblyLocationInformation);
        }

        /// <summary>
        /// Logic to load an embedded assembly
        /// </summary>
        /// <param name="assemblyLocationInformation"></param>
        /// <returns>Assembly</returns>
        private Assembly LoadEmbeddedAssembly(AssemblyLocationInformation assemblyLocationInformation)
        {
            if (!assemblyLocationInformation.IsEmbedded)
            {
                return null;
            }

            // Check if we can work with temporary files
            if (_applicationConfig.CopyEmbeddedAssembliesToFileSystem)
            {
                var assembly = LoadEmbeddedAssemblyViaTmpFile(assemblyLocationInformation);
                if (assembly != null)
                {
                    return assembly;
                }
            }

            if (Log.IsVerboseEnabled())
            {
                Log.Verbose().WriteLine("Loading {0} internally, this COULD cause assembly load context issues...", assemblyLocationInformation.Name);
            }
            using (var stream = Resources.AbsoluteResourceAsStream(assemblyLocationInformation.ContainingAssembly, assemblyLocationInformation.Filename))
            {
                return Assembly.Load(stream.ToByteArray());
            }
        }

        /// <summary>
        /// Load an assembly via a file, this used via Assembly.Load or Assembly.LoadFrom depending on where the file is or can be stored
        /// </summary>
        /// <param name="additionalInformation">AssemblyLocationInformation used for some decisions</param>
        /// <returns>Assembly</returns>
        private Assembly LoadFromFile(AssemblyLocationInformation additionalInformation)
        {
            // Get the assembly name from the file
            var assemblyName = AssemblyName.GetAssemblyName(additionalInformation.Filename);
            // Check the cache again, this time with the "real" name
            if (LoadedAssemblies.TryGetValue(assemblyName.Name, out var assembly))
            {
                Log.Info().WriteLine("Returned {0} from cache.", assemblyName.Name);
                return assembly;
            }

            if (_applicationConfig.CopyAssembliesToProbingPath && FileLocations.AddonsLocation != null)
            {
                var destination = $@"{FileLocations.AddonsLocation}\{assemblyName.Name}.dll";
                try
                {
                    if (ShouldWrite(additionalInformation, destination))
                    {
                        Log.Verbose().WriteLine("Creating a copy of {0} to {1}, solving potential context loading issues.", additionalInformation.Filename, destination);
                        File.Copy(additionalInformation.Filename, destination);
                        // Register delete on exit, this is done by calling a command
                        _assembliesToDeleteAtExit.Add(destination);
                    }
                    // Load via the assembly name, it's not inside the probing path
                    assembly = Assembly.Load(assemblyName);
                    if (assembly != null)
                    {
                        return assembly;
                    }
                }
                catch (Exception ex)
                {
                    Log.Warn().WriteLine(ex, "Couldn't create a copy of {0} to {1}.", additionalInformation.Filename, destination);
                }
            }

            if (Log.IsVerboseEnabled())
            {
                Log.Verbose().WriteLine("Loading {0} from {1}.", additionalInformation.Name, additionalInformation.Filename);
            }

            try
            {
                return Assembly.LoadFrom(additionalInformation.Filename);
            }
            catch (Exception ex)
            {
                Log.Error().WriteLine(ex, "Couldn't load assembly from file {0}", additionalInformation.Filename);
            }
            return null;
        }

        /// <summary>
        /// This will try to resolve the requested assembly by looking into the cache
        /// </summary>
        /// <param name="sender">object</param>
        /// <param name="resolveEventArgs">ResolveEventArgs</param>
        /// <returns>Assembly</returns>
        private Assembly AssemblyResolve(object sender, ResolveEventArgs resolveEventArgs)
        {
            var assemblyName = new AssemblyName(resolveEventArgs.Name);
            Log.Verbose().WriteLine("Resolving {0}", assemblyName.FullName);
            if (_resolving.Contains(assemblyName.Name))
            {
                Log.Warn().WriteLine("Ignoring recursive resolve event for {0}", assemblyName.Name);
                return null;
            }

            if (assemblyName.Name.EndsWith(".resources"))
            {
                // Ignore to prevent resolving logic which doesn't find anything anyway
                if (Log.IsVerboseEnabled())
                {
                    Log.Verbose().WriteLine("Ignoring resolve event for {0}", assemblyName.Name);
                }
                return null;
            }

            if (LoadedAssemblies.TryGetValue(assemblyName.Name, out var assemblyResult))
            {
                if (Log.IsVerboseEnabled())
                {
                    Log.Verbose().WriteLine("Returned {0} from cache.", assemblyName.Name);
                }

                return assemblyResult;
            }

            try
            {
                _resolving.Add(assemblyName.Name);
                if (!AvailableAssemblies.TryGetValue(assemblyName.Name, out var assemblyLocationInformation))
                {
                    return null;
                }

                if (Log.IsVerboseEnabled())
                {
                    Log.Verbose().WriteLine("Found {0} at {1}.", assemblyName.Name, assemblyLocationInformation.ToString());
                }

                return LoadAssembly(assemblyLocationInformation);
            }
            finally
            {
                _resolving.Remove(assemblyName.Name);
            }
        }

        /// <summary>
        /// Get a list of all embedded assemblies
        /// </summary>
        /// <returns>IEnumerable with a containing the names of the resource and of the assemblie</returns>
        public IEnumerable<string> EmbeddedAssemblyNames(IEnumerable<Assembly> assembliesToCheck = null)
        {
            foreach (var loadedAssembly in assembliesToCheck ?? LoadedAssemblies.Where(pair => !AssembliesToIgnore.IsMatch(pair.Key)).Select(pair => pair.Value).ToList())
            {
                string[] resources;
                try
                {
                    resources = Resources.GetCachedManifestResourceNames(loadedAssembly);
                }
                catch (Exception ex)
                {
                    Log.Warn().WriteLine(ex, "Couldn't retrieve resources from {0}", loadedAssembly.GetName().Name);
                    continue;
                }
                foreach (var resource in resources)
                {
                    var resourceMatch = _assemblyResourceNameRegex.Match(resource);
                    if (resourceMatch.Success)
                    {
                        yield return resourceMatch.Groups["assembly"].Value;
                    }
                }
            }
        }

        /// <summary>
        /// This is a workaround where an embedded assembly is written to a tmp file, which solves some issues
        /// </summary>
        /// <param name="assemblyLocationInformation">AssemblyLocationInformation</param>
        /// <returns>Assembly</returns>
        private Assembly LoadEmbeddedAssemblyViaTmpFile(AssemblyLocationInformation assemblyLocationInformation)
        {
            var assemblyFileName = $@"{FileLocations.AddonsLocation}\{assemblyLocationInformation.Name}.dll";
            using (var stream = Resources.AbsoluteResourceAsStream(assemblyLocationInformation.ContainingAssembly, assemblyLocationInformation.Filename))
            {
                try
                {
                    if (ShouldWrite(assemblyLocationInformation, assemblyFileName))
                    {
                        using (var fileStream = new FileStream(assemblyFileName, FileMode.OpenOrCreate, FileAccess.Write, FileShare.None))
                        {
                            stream.CopyTo(fileStream);
                        }
                        // Register delete on exit, this is done by calling a command
                        _assembliesToDeleteAtExit.Add(assemblyFileName);
                    }

                    // Get the assembly name from the file
                    var assemblyName = AssemblyName.GetAssemblyName(assemblyFileName);
                    Log.Verbose().WriteLine("Loading {0} from {1}", assemblyLocationInformation.Name, assemblyFileName);
                    // Use load, as it's now in the probing path
                    return Assembly.Load(assemblyName);
                }
                catch (Exception)
                {
                    stream.Seek(0, SeekOrigin.Begin);
                    var appdataDirectory = $@"{Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)}\{_applicationConfig.ApplicationName}";
                    assemblyFileName = $@"{appdataDirectory}\{assemblyLocationInformation.Name}.dll";
                    if (ShouldWrite(assemblyLocationInformation, assemblyFileName))
                    {
                        using (var fileStream = new FileStream(assemblyFileName, FileMode.OpenOrCreate, FileAccess.Write, FileShare.None))
                        {
                            stream.CopyTo(fileStream);
                        }
                        // Register delete on exit, this is done by calling a command
                        _assembliesToDeleteAtExit.Add(assemblyFileName);
                    }
                    Log.Verbose().WriteLine("Loading {0} from {1}", assemblyLocationInformation.Name, assemblyFileName);
                    // Use load-from, as it's on a different place
                    return Assembly.LoadFrom(assemblyFileName);
                }
            }
        }

        /// <summary>
        /// Test if the source should be written to the destination (true) or if the destination is newer/same (false)
        /// </summary>
        /// <param name="source">AssemblyLocationInformation</param>
        /// <param name="destination">string with destination path</param>
        /// <returns>bool</returns>
        private bool ShouldWrite(AssemblyLocationInformation source, string destination)
        {
            if (source.Filename.Equals(destination))
            {
                return false;
            }
            string path = Path.GetDirectoryName(destination);
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(FileLocations.AddonsLocation);
            }

            if (!File.Exists(destination))
            {
                return true;
            }

            if (File.GetLastWriteTime(destination) >= source.FileDate)
            {
                return false;
            }

            Log.Warn().WriteLine("Overwriting {0} with {1}, as the later is newer.", destination, source.Filename);
            File.Delete(destination);
            return true;
        }

        /// <summary>
        /// This is called when a new assembly is loaded, we need to know this
        /// </summary>
        /// <param name="sender">object</param>
        /// <param name="args">AssemblyLoadEventArgs</param>
        private void AssemblyLoad(object sender, AssemblyLoadEventArgs args)
        {
            var loadedAssembly = args.LoadedAssembly;
            var assemblyName = loadedAssembly.GetName().Name;
            Log.Verbose().WriteLine("Loaded {0}", assemblyName);
            LoadedAssemblies[assemblyName] = loadedAssembly;

            if (!_applicationConfig.ScanForEmbeddedAssemblies)
            {
                return;
            }

            string[] resources;
            try
            {
                resources = Resources.GetCachedManifestResourceNames(loadedAssembly);
            }
            catch (Exception ex)
            {
                Log.Warn().WriteLine(ex, "Couldn't retrieve resources from {0}", loadedAssembly.GetName().Name);
                return;
            }
            foreach (var resource in resources)
            {
                var resourceMatch = _assemblyResourceNameRegex.Match(resource);
                if (!resourceMatch.Success)
                {
                    continue;
                }

                var embeddedAssemblyName = resourceMatch.Groups["assembly"].Value;
                if (LoadedAssemblies.ContainsKey(embeddedAssemblyName))
                {
                    // Ignoring already loaded assembly, as we cannot unload.
                    continue;
                }
                var newAssemblyLocation = new AssemblyLocationInformation(embeddedAssemblyName, loadedAssembly, resource);
                if (AvailableAssemblies.TryGetValue(embeddedAssemblyName, out var availableAssemblyLocationInformation))
                {
                    if (availableAssemblyLocationInformation.FileDate > newAssemblyLocation.FileDate)
                    {
                        continue;
                    }
                }
                if (Log.IsVerboseEnabled())
                {
                    Log.Verbose().WriteLine("Detected additional assembly {0} in {1}", embeddedAssemblyName, loadedAssembly.GetName().Name);
                }
                AvailableAssemblies[embeddedAssemblyName] = newAssemblyLocation;
            }
        }

        /// <summary>
        /// Helper method to clean up
        /// </summary>
        private void RemoveCopiedAssemblies()
        {
            if (!CleanupAfterExit ||_assembliesToDeleteAtExit.Count == 0)
            {
                return;
            }

            if (Log.IsVerboseEnabled())
            {
                Log.Verbose().WriteLine("Removing cached assembly files: \r\n\t{0}", string.Join("\r\n\t", _assembliesToDeleteAtExit.Select(a => $"\"{FileTools.NormalizeDirectory(a)}\"")));
            }

            var info = new ProcessStartInfo
            {
                Arguments = "/C choice /C Y /N /D Y /T 3 & Del " + string.Join(" ", _assembliesToDeleteAtExit),
                WindowStyle = ProcessWindowStyle.Hidden,
                CreateNoWindow = true,
                FileName = "cmd.exe"
            };
            _assembliesToDeleteAtExit.Clear();
            Process.Start(info);
        }

        /// <summary>
        /// Remove event registrations
        /// </summary>
        public void Dispose()
        {
            // Unregister assembly loading
            AppDomain.CurrentDomain.AssemblyLoad -= AssemblyLoad;
            // Register assembly resolving
            AppDomain.CurrentDomain.AssemblyResolve -= AssemblyResolve;

            RemoveCopiedAssemblies();
        }
    }
}
