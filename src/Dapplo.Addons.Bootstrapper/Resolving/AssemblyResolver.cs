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
        private readonly ISet<AssemblyName> _resolving = new HashSet<AssemblyName>();
        private readonly IList<string> _assembliesToDeleteAtExit = new List<string>();

        /// <summary>
        /// A regex with all the assemblies which we should ignore
        /// </summary>
        public Regex AssembliesToIgnore { get; } = new Regex(@"^(xunit.*|microsoft\..*|mscorlib|UIAutomationProvider|PresentationFramework|PresentationCore|WindowsBase|autofac.*|Dapplo\.Log.*|Dapplo\.Ini|Dapplo\.Language|Dapplo\.Utils|Dapplo\.Addons|Dapplo\.Addons\.Bootstrapper|Dapplo\.Windows.*|system.*|.*resources|Dapplo\.InterfaceImpl.*)$", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        /// <summary>
        /// A dictionary with all the loaded assemblies, for caching and analysing
        /// </summary>
        public IDictionary<string, Assembly> LoadedAssemblies { get; } = new ConcurrentDictionary<string, Assembly>();

        /// <summary>
        /// Gives access to the resources in assemblies
        /// </summary>
        public ManifestResources Resources { get; }

        /// <summary>
        /// Specify if embedded assemblies need to be written to disk before using, this solves some compatiblity issues
        /// </summary>
        public bool UseDiskCache { get; set; } = true;

        /// <summary>
        /// The directories (normalized) to scan for DLL files
        /// </summary>
        public ISet<string> ScanDirectories { get; } = new HashSet<string>();

        /// <summary>
        /// The constructor of the Assembly Resolver
        /// </summary>
        /// <param name="applicationName">string</param>
        public AssemblyResolver(string applicationName = null)
        {
            _applicationName = applicationName;
            Resources = new ManifestResources(assemblyName => LoadedAssemblies.ContainsKey(assemblyName) ? LoadedAssemblies[assemblyName] : null);

            foreach (var loadedAssembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                LoadedAssemblies[loadedAssembly.GetName().Name] = loadedAssembly;
            }

            // Register assembly loading
            AppDomain.CurrentDomain.AssemblyLoad += AssemblyLoad;
            // Register assembly resolving
            AppDomain.CurrentDomain.AssemblyResolve += AssemblyResolve;
            AppDomain.CurrentDomain.ProcessExit += (sender, args) =>
            {
                if (_assembliesToDeleteAtExit.Count == 0)
                {
                    return;
                }
                Log.Debug().WriteLine("Removing cached assembly files {0}", string.Join(" ", _assembliesToDeleteAtExit.Select(a => $"\"{FileTools.NormalizeDirectory(a)}\"")));
                var info = new ProcessStartInfo
                {
                    Arguments = "/C choice /C Y /N /D Y /T 3 & Del " + string.Join(" ", _assembliesToDeleteAtExit),
                    WindowStyle = ProcessWindowStyle.Hidden,
                    CreateNoWindow = true,
                    FileName = "cmd.exe"
                };
                Process.Start(info);
            };

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
            var assemblyName = Path.GetFileNameWithoutExtension(filename);
            if (string.IsNullOrEmpty(assemblyName))
            {
                return null;
            }
            if (LoadedAssemblies.ContainsKey(assemblyName))
            {
                Log.Debug().WriteLine("Skipping {0} as the assembly was already loaded.", filename);
                return null;
            }

            // Use local file if possible
            try
            {
                return LoadOrLoadFrom(filename);
            }
            catch (Exception ex)
            {
                Log.Error().WriteLine(ex, "Couldn't load assembly from file {0}", filename);
            }

            return null;
        }

        /// <summary>
        /// Use Assembly.Load or Assembly.LoadFrom
        /// </summary>
        /// <param name="filename">string</param>
        /// <param name="allowCopy">bool </param>
        /// <returns>Assembly</returns>
        private Assembly LoadOrLoadFrom(string filename, bool allowCopy = true)
        {
            var assemblyName = Path.GetFileNameWithoutExtension(filename) ?? throw new ArgumentNullException(nameof(filename));

            if (LoadedAssemblies.TryGetValue(assemblyName, out var assembly))
            {
                Log.Info().WriteLine("Returned {0} from cache.", assemblyName);
                return assembly;
            }
            var directoryToLoadFrom = Path.GetDirectoryName(filename) ?? string.Empty;
            AddScanDirectory(directoryToLoadFrom);

            foreach (var assemblyResolveDirectory in FileLocations.AssemblyResolveDirectories)
            {
                var prefferedLocation = $@"{assemblyResolveDirectory}\{assemblyName}.dll";
                if (File.Exists(prefferedLocation))
                {
                    return Assembly.Load(assemblyName);
                }
            }

            if (allowCopy && UseDiskCache)
            {
                if (!directoryToLoadFrom.Equals(FileLocations.StartupDirectory, StringComparison.InvariantCultureIgnoreCase))
                {
                    foreach (var assemblyResolveDirectory in FileLocations.AssemblyResolveDirectories)
                    {
                        var newLocation = $@"{assemblyResolveDirectory}\{assemblyName}.dll";
                        try
                        {
                            if (!Directory.Exists(assemblyResolveDirectory))
                            {
                                Directory.CreateDirectory(assemblyResolveDirectory);
                            }
                            Log.Warn().WriteLine("Creating a copy of {0} to {1}, solving loading issues.", filename, newLocation);
                            File.Copy(filename, newLocation);
                            _assembliesToDeleteAtExit.Add(newLocation);
                            return Assembly.Load(assemblyName);
                        }
                        catch (Exception ex)
                        {
                            Log.Warn().WriteLine(ex, "Couldn't create a copy of {0} to {1}.", filename, newLocation);
                        }
                    }
                }
            }
            return Assembly.LoadFrom(filename);
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
            if (_resolving.Contains(assemblyName))
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
                Log.Info().WriteLine("Returned {0} from cache.", assemblyName.Name);
            }
            else
            {
                try
                {
                    _resolving.Add(assemblyName);
                    // Check files before embedded
                    var assemblyFile = FileLocations.Scan(ScanDirectories, assemblyName.Name + ".dll").FirstOrDefault();
                    if (assemblyFile != null)
                    {
                        assemblyResult = LoadOrLoadFrom(assemblyFile);
                    }

                    if (assemblyResult == null)
                    {
                        assemblyResult = LoadEmbeddedAssembly(assemblyName.Name);
                    }
                }
                finally
                {
                    _resolving.Remove(assemblyName);
                }

            }

            return assemblyResult;
        }

        /// <summary>
        /// Get a list of all embedded assemblies
        /// </summary>
        /// <returns>IEnumerable with a tutple containing the name of the resource and of the assemblie</returns>
        public IEnumerable<string> EmbeddedAssemblyNames()
        {
            foreach (var loadedAssembly in LoadedAssemblies.Where(pair => !AssembliesToIgnore.IsMatch(pair.Key)).Select(pair => pair.Value))
            {
                var resources = Resources.GetCachedManifestResourceNames(loadedAssembly);
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
                Log.Info().WriteLine("Returned {0} from cache.", assemblyName);
                return cachedAssembly;
            }
            foreach (var assemblyWithResources in LoadedAssemblies.Where(pair => !AssembliesToIgnore.IsMatch(pair.Key)).Select(pair => pair.Value))
            {
                var resources = Resources.GetCachedManifestResourceNames(assemblyWithResources);
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
            try
            {
                var assemblyFileName = $@"{FileLocations.StartupDirectory}\{assemblyName}.dll";
                using (var stream = Resources.GetEmbeddedResourceAsStream(containingAssembly, resource, false))
                {
                    var bytes = stream.ToByteArray();
                    try
                    {
                        using (var fileStream = new FileStream(assemblyFileName, FileMode.OpenOrCreate, FileAccess.Write, FileShare.None))
                        {
                            fileStream.Write(bytes, 0, bytes.Length);
                        }
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
                        File.WriteAllBytes(assemblyFileName, bytes);
                    }
                }

                // Register delete on exit, this is done by calling a command
                _assembliesToDeleteAtExit.Add(assemblyFileName);

                Log.Verbose().WriteLine("Loading {0} from temporary assembly file {1}", assemblyName, assemblyFileName);

                // Best case, we can load it now by name, let the LoadOrLoadFrom decide
                return LoadOrLoadFrom(assemblyFileName, false);
            }
            catch (Exception ex)
            {
                Log.Warn().WriteLine(ex, "Couldn't load assembly via a file {0}", assemblyName);
                return null;
            }
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
        /// Remove event registrations
        /// </summary>
        public void Dispose()
        {
            // Unregister assembly loading
            AppDomain.CurrentDomain.AssemblyLoad -= AssemblyLoad;
            // Register assembly resolving
            AppDomain.CurrentDomain.AssemblyResolve -= AssemblyResolve;
        }
    }
}
