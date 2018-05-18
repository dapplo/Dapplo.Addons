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
using Dapplo.Log;

namespace Dapplo.Addons.Bootstrapper.Resolving
{
    /// <summary>
    /// This class supports the resolving of assemblies
    /// </summary>
    public class AssemblyResolver : IDisposable
    {
        private static readonly LogSource Log = new LogSource();
        private static readonly Regex AssemblyResourceNameRegex = new Regex(@"^(costura\.)*(?<assembly>.*)\.dll(\.compressed|\*.gz)*$", RegexOptions.Compiled);
        private string _applicationName;
        private readonly ISet<string> _resolving = new HashSet<string>();
        private readonly IList<string> _assembliesToDeleteAtExit = new List<string>();

        /// <summary>
        /// A regex with all the assemblies which we should ignore
        /// </summary>
        public Regex AssembliesToIgnore { get; } = new Regex(@"^(microsoft\..*|mscorlib|UIAutomationProvider|PresentationFramework|PresentationCore|WindowsBase|system.*|.*resources)$", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        /// <summary>
        /// A dictionary with all the loaded assemblies, for caching and analysing
        /// </summary>
        public IDictionary<string, Assembly> LoadedAssemblies { get; } = new ConcurrentDictionary<string, Assembly>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Gives access to the resources in assemblies
        /// </summary>
        public ManifestResources Resources { get; }

        /// <summary>
        /// Specify if embedded assemblies need to be written to disk before using, this solves some compatiblity issues
        /// </summary>
        public bool UseDiskCache { get; set; } = true;

        /// <summary>
        /// Specify if embedded assemblies written to disk before using will be removed again when the process exits
        /// </summary>
        public bool CleanupAfterExit { get; set; } = true;

        /// <summary>
        /// The directories (normalized) to scan for addon files
        /// </summary>
        public ISet<string> ScanDirectories { get; } = new HashSet<string>(FileLocations.AssemblyResolveDirectories);

        /// <summary>
        /// The constructor of the Assembly Resolver
        /// </summary>
        /// <param name="applicationName">string</param>
        public AssemblyResolver(string applicationName = null)
        {
            _applicationName = applicationName;
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
        }

        /// <summary>
        /// Add an additional scan directory
        /// </summary>
        /// <param name="scanDirectory">string</param>
        public AssemblyResolver AddScanDirectory(string scanDirectory)
        {
            if (string.IsNullOrEmpty(scanDirectory))
            {
                return this;
            }
            scanDirectory = FileTools.NormalizeDirectory(scanDirectory);
            if (!ScanDirectories.Contains(scanDirectory))
            {
                ScanDirectories.Add(scanDirectory);
            }
            return this;
        }

        /// <summary>
        /// Load an assembly from the specified filename, if the assembly was already loaded skip it.
        /// </summary>
        /// <param name="filename">string</param>
        /// <returns>bool</returns>
        public Assembly LoadAssembly(string filename)
        {
            TryLoadOrLoadFrom(filename, out var assembly);
            return assembly;
        }

        /// <summary>
        /// Try to load an assembly via Assembly.Load or Assembly.LoadFrom
        /// </summary>
        /// <param name="filename">string</param>
        /// <param name="assembly">Assembly returned or null if it couldn't be loaded</param>
        /// <param name="allowCopy">bool which specifies if it's allowed to make a copy of the file to improve compatibility</param>
        /// <returns>bool true if the assembly was loaded, false if it was already in the cache or a loading problem occured</returns>
        private bool TryLoadOrLoadFrom(string filename, out Assembly assembly, bool allowCopy = true)
        {
            // Get a simple assembly name via the filename
            var simpleAssemblyName = Path.GetFileNameWithoutExtension(filename) ?? throw new ArgumentNullException(nameof(filename));
            if (string.IsNullOrEmpty(simpleAssemblyName))
            {
                assembly = null;
                return false;
            }
            // Check if the simple name can be found in the cache
            if (LoadedAssemblies.TryGetValue(simpleAssemblyName, out assembly))
            {
                Log.Info().WriteLine("Returned {0} from cache.", simpleAssemblyName);
                return false;
            }
            // Get the assembly name from the file
            var assemblyName = AssemblyName.GetAssemblyName(filename);
            // Check the cache again
            if (LoadedAssemblies.TryGetValue(assemblyName.Name, out assembly))
            {
                Log.Info().WriteLine("Returned {0} from cache.", assemblyName.Name);
                return false;
            }

            // Add the directory of the DLL to the scan path, to find other DLLs too
            AddScanDirectory(Path.GetDirectoryName(filename));

            foreach (var assemblyResolveDirectory in FileLocations.AssemblyResolveDirectories)
            {
                var preferredLocation = $@"{assemblyResolveDirectory}\{assemblyName.Name}.dll";
                if (!File.Exists(preferredLocation))
                {
                    continue;
                }

                Log.Verbose().WriteLine("Loading {0} from preferred location {1} via the assembly name.", assemblyName.Name, preferredLocation);
                assembly = Assembly.Load(assemblyName);
                if (assembly != null)
                {
                    return true;
                }
            }

            if (allowCopy && UseDiskCache && FileLocations.AddonsLocation != null)
            {
                var newLocation = $@"{FileLocations.AddonsLocation}\{assemblyName.Name}.dll";
                try
                {
                    if (!Directory.Exists(FileLocations.AddonsLocation))
                    {
                        Directory.CreateDirectory(FileLocations.AddonsLocation);
                    }
                    if (!File.Exists(newLocation))
                    {
                        Log.Warn().WriteLine("Creating a copy of {0} to {1}, solving loading issues.", filename, newLocation);
                        File.Copy(filename, newLocation);
                        _assembliesToDeleteAtExit.Add(newLocation);
                        assembly = Assembly.Load(assemblyName);
                        if (assembly != null)
                        {
                            return true;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Log.Warn().WriteLine(ex, "Couldn't create a copy of {0} to {1}.", filename, newLocation);
                }
            }
            Log.Verbose().WriteLine("Loading {0} from {1}.", simpleAssemblyName, filename);
            try
            {
                assembly = Assembly.LoadFrom(filename);
                return true;
            }
            catch (Exception ex)
            {
                assembly = null;
                Log.Error().WriteLine(ex, "Couldn't load assembly from file {0}", filename);
                return false;
            }
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
                Log.Verbose().WriteLine("Returned {0} from cache.", assemblyName.Name);
                return assemblyResult;
            }

            try
            {
                _resolving.Add(assemblyName.Name);
                // Check files before embedded
                var assemblyFile = FileLocations.Scan(ScanDirectories, assemblyName.Name + ".dll").FirstOrDefault();
                if (assemblyFile == null || (!TryLoadOrLoadFrom(assemblyFile, out assemblyResult) && assemblyResult == null))
                {
                    assemblyResult = LoadEmbeddedAssembly(assemblyName.Name);
                }
                return assemblyResult;
            }
            finally
            {
                _resolving.Remove(assemblyName.Name);
            }
        }

        /// <summary>
        /// Get a list of all embedded assemblies
        /// </summary>
        /// <returns>IEnumerable with a tutple containing the name of the resource and of the assemblie</returns>
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
                    var resourceMatch = AssemblyResourceNameRegex.Match(resource);
                    if (resourceMatch.Success)
                    {
                        yield return resourceMatch.Groups["assembly"].Value;
                    }
                }
            }
        }

        /// <summary>
        /// This loads an assembly which is embedded (manually or by costura) in one of the already loaded assemblies
        /// </summary>
        /// <param name="assemblyName">Simple name of the assembly</param>
        /// <returns>Assembly or null when not found</returns>
        public Assembly LoadEmbeddedAssembly(string assemblyName)
        {
            // Do not load again if already loaded
            if (LoadedAssemblies.TryGetValue(assemblyName, out var cachedAssembly))
            {
                Log.Verbose().WriteLine("Returned {0} from cache.", assemblyName);
                return cachedAssembly;
            }
            foreach (var assemblyWithResources in LoadedAssemblies.Where(pair => !AssembliesToIgnore.IsMatch(pair.Key)).Select(pair => pair.Value).ToList())
            {
                string[] resources;
                try
                {
                    resources = Resources.GetCachedManifestResourceNames(assemblyWithResources);
                }
                catch (Exception ex)
                {
                    Log.Warn().WriteLine(ex, "Couldn't retrieve resources from {0}", assemblyWithResources.GetName().Name);
                    continue;
                }
                foreach (var resource in resources)
                {
                    var resourceMatch = AssemblyResourceNameRegex.Match(resource);
                    if (!resourceMatch.Success)
                    {
                        continue;
                    }

                    if (!string.Equals(assemblyName, resourceMatch.Groups["assembly"].Value, StringComparison.InvariantCultureIgnoreCase))
                    {
                        continue;
                    }
                    // Match
                    Log.Verbose().WriteLine("Resolved {0} from {1}", assemblyName, assemblyWithResources.GetName().Name);

                    // Check if we work with temporary files
                    if (UseDiskCache)
                    {
                        var loadedAssembly = LoadEmbeddedAssemblyViaTmpFile(assemblyWithResources, resource, assemblyName);
                        if (loadedAssembly != null)
                        {
                            return loadedAssembly;
                        }
                    }

                    Log.Verbose().WriteLine("Loading {0} internally", assemblyName);
                    using (var stream = Resources.GetEmbeddedResourceAsStream(assemblyWithResources, resource, false))
                    {
                        return Assembly.Load(stream.ToByteArray());
                    }
                }
            }
            Log.Warn().WriteLine("Couldn't locate {0}", assemblyName);
            return null;
        }

        /// <summary>
        /// This is a workaround where an embedded assembly is written to a tmp file, which solves some issues
        /// </summary>
        /// <param name="containingAssembly">Assembly which contains the resource</param>
        /// <param name="resource">string with the resource</param>
        /// <param name="assemblyName">string with name of the assembly</param>
        /// <returns></returns>
        private Assembly LoadEmbeddedAssemblyViaTmpFile(Assembly containingAssembly, string resource, string assemblyName)
        {
            var assemblyFileName = $@"{FileLocations.AddonsLocation}\{assemblyName}.dll";
            using (var stream = Resources.GetEmbeddedResourceAsStream(containingAssembly, resource, false))
            {
                var bytes = stream.ToByteArray();
                try
                {
                    Log.Verbose().WriteLine("Creating temporary assembly file {0}", assemblyFileName);
                    using (var fileStream = new FileStream(assemblyFileName, FileMode.OpenOrCreate, FileAccess.Write, FileShare.None))
                    {
                        fileStream.Write(bytes, 0, bytes.Length);
                    }
                    _assembliesToDeleteAtExit.Add(assemblyFileName);

                }
                catch (Exception)
                {
                    // Redirecting to APPDATA local
                    if (string.IsNullOrEmpty(_applicationName))
                    {
                        using (var process = Process.GetCurrentProcess())
                        {
                            _applicationName = process.ProcessName;
                        }
                    }

                    var appdataDirectory = $@"{Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)}\{_applicationName}";
                    if (!Directory.Exists(appdataDirectory))
                    {
                        Directory.CreateDirectory(appdataDirectory);
                    }

                    assemblyFileName = $@"{appdataDirectory}\{assemblyName}.dll";
                    Log.Verbose().WriteLine("Creating temporary assembly file {0}", assemblyFileName);
                    using (var fileStream = new FileStream(assemblyFileName, FileMode.OpenOrCreate, FileAccess.Write, FileShare.None))
                    {
                        fileStream.Write(bytes, 0, bytes.Length);
                    }
                    // Register delete on exit, this is done by calling a command
                    _assembliesToDeleteAtExit.Add(assemblyFileName);
                }
            }


            Log.Verbose().WriteLine("Loading {0} from temporary assembly file {1}", assemblyName, assemblyFileName);
            // Best case, we can load it now by name, let the LoadOrLoadFrom decide
            TryLoadOrLoadFrom(assemblyFileName,  out var assembly, false);
            return assembly;
        }

        /// <summary>
        /// This is called when a new assembly is loaded, we need to know this
        /// </summary>
        /// <param name="sender">object</param>
        /// <param name="args">AssemblyLoadEventArgs</param>
        private void AssemblyLoad(object sender, AssemblyLoadEventArgs args)
        {
            var assembly = args.LoadedAssembly;
            var assemblyName = assembly.GetName().Name;
            Log.Verbose().WriteLine("Loaded {0}", assemblyName);
            LoadedAssemblies[assemblyName] = assembly;
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
            Log.Verbose().WriteLine("Removing cached assembly files {0}", string.Join(" ", _assembliesToDeleteAtExit.Select(a => $"\"{FileTools.NormalizeDirectory(a)}\"")));
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
