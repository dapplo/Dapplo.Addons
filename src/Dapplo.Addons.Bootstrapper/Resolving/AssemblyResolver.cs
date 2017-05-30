#region Dapplo 2016 - GNU Lesser General Public License

// Dapplo - building blocks for .NET applications
// Copyright (C) 2016 Dapplo
// 
// For more information see: http://dapplo.net/
// Dapplo repositories are hosted on GitHub: https://github.com/dapplo
// 
// This file is part of Dapplo.Addons
// 
// Dapplo.Addons is free software: you can redistribute it and/or modify
// it under the terms of the GNU Lesser General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// Dapplo.Addons is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU Lesser General Public License for more details.
// 
// You should have a copy of the GNU Lesser General Public License
// along with Dapplo.Addons. If not, see <http://www.gnu.org/licenses/lgpl.txt>.

#endregion

#region Usings

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using Dapplo.Addons.Bootstrapper.Extensions;
using Dapplo.Addons.Bootstrapper.Internal;
using Dapplo.Log;

#endregion

namespace Dapplo.Addons.Bootstrapper.Resolving
{
    /// <summary>
    ///     This is a static Assembly resolver and Assembly loader
    ///     It takes care of caching and prevents that an Assembly is loaded twice (which would cause issues!)
    /// </summary>
    public static class AssemblyResolver
    {
        private static readonly LogSource Log = new LogSource();
        private static readonly ISet<string> AppDomainRegistrations = new HashSet<string>();
        private static readonly ISet<string> ResolveDirectories = new HashSet<string>();
        private static readonly IDictionary<string, Assembly> AssembliesByName = new ConcurrentDictionary<string, Assembly>(StringComparer.OrdinalIgnoreCase);
        private static readonly IDictionary<string, Assembly> AssembliesByPath = new ConcurrentDictionary<string, Assembly>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        ///     Setup and Register some of the default assemblies in the assembly cache
        /// </summary>
        static AssemblyResolver()
        {
            Assembly.GetCallingAssembly().Register();
            Assembly.GetEntryAssembly().Register();
            Assembly.GetExecutingAssembly().Register();
            AddDirectory(".");
        }

        /// <summary>
        /// The extensions used for finding assemblies, you can add your own.
        /// Extensions can end on .gz when such a file/resource is used it will automatically be decompresed
        /// </summary>
        public static ISet<string> Extensions { get; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase) {"dll", "dll.gz", "dll.compressed"};

        /// <summary>
        /// Directories which this AssemblyResolver uses to find assemblies
        /// </summary>
        public static IEnumerable<string> Directories => ResolveDirectories;

        /// <summary>
        ///     IEnumerable with all cached assemblies
        /// </summary>
        public static IEnumerable<Assembly> AssemblyCache => AssembliesByName.Values;

        /// <summary>
        ///     Defines if the resolving is first loading internal files, if nothing was found check the file system
        ///     There might be security reasons for not doing this.
        /// </summary>
        public static bool ResolveEmbeddedBeforeFiles { get; set; } = true;

        /// <summary>
        ///     Defines if before loading an assembly from a resource, the Assembly names from the cache are checked against the resource name.
        ///     This speeds up the loading, BUT might have a problem that an assembly "x.y.z.dll" is skipped as "y.z.dll" was already loaded.
        /// </summary>
        public static bool CheckEmbeddedResourceNameAgainstCache { get; set; } = true;

        /// <summary>
        /// Add the specified directory, by converting it to an absolute directory
        /// </summary>
        /// <param name="directory">Directory to add for resolving</param>
        public static void AddDirectory(string directory)
        {
            lock (ResolveDirectories)
            {
                foreach (var absoluteDirectory in FileLocations.DirectoriesFor(directory))
                {
                    ResolveDirectories.Add(absoluteDirectory);
                }
            }
        }

        /// <summary>
        /// Extension to register an assembly to the AssemblyResolver, this is used for resolving embedded assemblies
        /// </summary>
        /// <param name="assembly">Assembly</param>
        /// <param name="filepath">Path to assembly, or null if it isn't loaded from the file system</param>
        public static void Register(this Assembly assembly, string filepath = null)
        {
            if (assembly == null)
            {
                Log.Verbose().WriteLine("Register was callled with null.");
                return;
            }
            var assemblyName = assembly.GetName().Name;
            lock (AssembliesByName)
            {

                if (!AssembliesByName.ContainsKey(assembly.GetName().Name))
                {
                    Log.Verbose().WriteLine("Registering Assembly {0}", assemblyName);
                    AssembliesByName[assembly.GetName().Name] = assembly;
                }
            }
            filepath = filepath ?? assembly.GetLocation();
            if (string.IsNullOrEmpty(filepath))
            {
                return;
            }
            lock (AssembliesByPath)
            {
                // Make sure the name is always the same.
                filepath = FileTools.RemoveExtensions(Path.GetFullPath(filepath), Extensions) + ".dll";
                if (AssembliesByPath.ContainsKey(filepath))
                {
                    return;
                }
                AssembliesByPath[filepath] = assembly;
                Log.Verbose().WriteLine("Registering Assembly {0} to file {1}", assemblyName, filepath);
            }
        }

        /// <summary>
        ///     Register the AssemblyResolve event for the specified AppDomain
        ///     This can be called multiple times, it detect this.
        /// </summary>
        /// <returns>IDisposable, when disposing this the event registration is removed</returns>
        public static IDisposable RegisterAssemblyResolve(this AppDomain appDomain)
        {
            lock (AppDomainRegistrations)
            {
                if (!AppDomainRegistrations.Contains(appDomain.FriendlyName))
                {
                    AppDomainRegistrations.Add(appDomain.FriendlyName);
                    appDomain.AssemblyResolve += ResolveEventHandler;
                    Log.Verbose().WriteLine("Registered Assembly-Resolving functionality to AppDomain {0}", appDomain.FriendlyName);
                }
                return SimpleDisposable.Create(() => UnregisterAssemblyResolve(appDomain));
            }
        }

        /// <summary>
        ///     Register AssemblyResolve on the current AppDomain
        /// </summary>
        /// <returns>IDisposable, when disposing this the event registration is removed</returns>
        public static IDisposable RegisterAssemblyResolve()
        {
            return AppDomain.CurrentDomain.RegisterAssemblyResolve();
        }

        /// <summary>
        ///     Unregister the AssemblyResolve event for the specified AppDomain
        ///     This can be called multiple times, it detect this.
        /// </summary>
        public static void UnregisterAssemblyResolve(this AppDomain appDomain)
        {
            lock (AppDomainRegistrations)
            {
                if (!AppDomainRegistrations.Contains(appDomain.FriendlyName))
                {
                    return;
                }
                AppDomainRegistrations.Remove(appDomain.FriendlyName);
                appDomain.AssemblyResolve -= ResolveEventHandler;
                Log.Verbose().WriteLine("Unregistered Assembly-Resolving functionality from AppDomain {0}", appDomain.FriendlyName);
            }
        }

        /// <summary>
        ///     Unregister AssemblyResolve from the current AppDomain
        /// </summary>
        public static void UnregisterAssemblyResolve()
        {
            AppDomain.CurrentDomain.UnregisterAssemblyResolve();
        }

        /// <summary>
        ///     A resolver which takes care of loading DLL's which are referenced from AddOns but not found
        /// </summary>
        /// <param name="sender">object</param>
        /// <param name="resolveEventArgs">ResolveEventArgs</param>
        /// <returns>Assembly</returns>
        private static Assembly ResolveEventHandler(object sender, ResolveEventArgs resolveEventArgs)
        {
            var assemblyName = new AssemblyName(resolveEventArgs.Name);

            // Check if it is an resources resolve event, see http://stackoverflow.com/questions/4368201 for more info
            if (assemblyName.Name.EndsWith(".resources"))
            {
                // Ignore to prevent resolving logic which doesn't find anything anyway
                Log.Verbose().WriteLine("Ignoring resolve event for {0}", assemblyName.FullName);
                return null;
            }
            Log.Verbose().WriteLine("Resolve event for {0}", assemblyName.FullName);
            var assembly = FindAssembly(assemblyName.Name);
            if (assembly != null && assembly.FullName != assemblyName.FullName)
            {
                Log.Warn().WriteLine("Requested was {0} returned was {1}, this might cause issues but loading the same assembly would be worse.", assemblyName.FullName, assembly.FullName);
            }
            return assembly;
        }

        /// <summary>
        /// This goes over the know assemblies from this AssemblyResolver, but also checks the current AppDomain so assemblies are not loaded double!
        /// </summary>
        /// <param name="assemblyName">string with the name (not full name) of the assembly</param>
        /// <returns>Assembly</returns>
        private static Assembly FindCachedAssemblyByAssemblyName(string assemblyName)
        {
            // check if the file was already loaded, this assumes that the filename (without extension) IS the assembly name
            Assembly assembly;

            lock (AssembliesByName)
            {
                AssembliesByName.TryGetValue(assemblyName, out assembly);
            }
            if (assembly != null)
            {
                Log.Verbose().WriteLine("Using cached assembly {0}.", assembly.FullName);
            }
            else 
            {
                // The assembly was not found in our own cache, find it in the current AppDomain
                assembly = AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(x => string.Equals(x.GetName().Name, assemblyName, StringComparison.InvariantCultureIgnoreCase));
                if (assembly != null)
                {
                    Log.Verbose().WriteLine("Using already loaded assembly {1} for requested {0}.", assemblyName, assembly.FullName);
                    // Register the assembly, so the Dapplo.Addons Bootstrapper knows it too
                    assembly.Register();
                }
                else
                {
                    Log.Verbose().WriteLine("Couldn't find an available assembly called {0}.", assemblyName);

                }
            }
            return assembly;
        }

        /// <summary>
        /// check the caches to see if the assembly was already loaded
        /// </summary>
        /// <param name="filepath">string with the path where the assembly should be loaded from</param>
        /// <returns>Assembly when it was cached, or null when it was not cached</returns>
        private static Assembly FindCachedAssemblyByFilepath(string filepath)
        {
            filepath = FileTools.RemoveExtensions(Path.GetFullPath(filepath), Extensions) + ".dll";
            Assembly assembly;

            lock (AssembliesByName)
            {
                // Dynamic assemblies don't have a location, skip them, it would cause a NotSupportedException
                assembly = AssembliesByName.Values.FirstOrDefault(x => string.Equals(x.GetLocation(), filepath, StringComparison.InvariantCultureIgnoreCase));
            }

            // Check for assemblies by path
            if (assembly == null)
            {
                lock (AssembliesByPath)
                {
                    AssembliesByPath.TryGetValue(filepath, out assembly);
                }
            }
            if (assembly == null)
            {
                lock (AssembliesByPath)
                {
                    assembly = AssembliesByPath.Where(x => string.Equals(Path.GetFileNameWithoutExtension(x.Key), Path.GetFileNameWithoutExtension(filepath), StringComparison.InvariantCultureIgnoreCase)).Select(x => x.Value).FirstOrDefault();
                }
                if (assembly != null)
                {
                    return assembly;
                }
            }
            // The assembly wasn't found in our internal cache, now we go through the AppDomain
            // Dynamic assemblies don't have a location, skip them, it would cause a NotSupportedException
            assembly = AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(x => string.Equals(x.GetLocation(), filepath, StringComparison.InvariantCultureIgnoreCase));
            if (assembly != null)
            {
                // found something, cache it for later usage
                assembly.Register(filepath);
            }
            return assembly;
        }

        /// <summary>
        ///     Simple method to load an assembly from a file path (or returned a cached version).
        ///     If it was loaded new, it will be added to the cache
        /// </summary>
        /// <param name="filepath">string with the path to the file</param>
        /// <returns>Assembly</returns>
        [SuppressMessage("Sonar Code Smell", "S3885:Assembly.Load should be used", Justification = "Assembly.Load doesn't work on paths outside of the AppDomain.CurrentDomain.BaseDirectory")]
        public static Assembly LoadAssemblyFromFile(string filepath)
        {
            if (string.IsNullOrEmpty(filepath))
            {
                throw new ArgumentNullException(nameof(filepath));
            }
            var assembly = FindCachedAssemblyByFilepath(filepath);

            if (assembly != null)
            {
                Log.Verbose().WriteLine("Returning cached assembly for {0}, as {1} was already loaded.", filepath, assembly.FullName);
                return assembly;
            }
            // check if the file was already loaded, this assumes that the filename (without extension) IS the assembly name
            var assemblyNameFromPath = FileTools.RemoveExtensions(Path.GetFileName(filepath), Extensions);
            assembly = FindCachedAssemblyByAssemblyName(assemblyNameFromPath);
            if (assembly != null)
            {
                Log.Verbose().WriteLine("Skipping loading assembly-file from {0}, as {1} was already loaded.", filepath, assembly.FullName);
                return assembly;
            }

            Log.Verbose().WriteLine("Loading assembly from {0}", filepath);
            if (filepath.EndsWith(".gz") || filepath.EndsWith(".compressed"))
            {
                using (var fileStream = new FileStream(filepath, FileMode.Open, FileAccess.ReadWrite))
                {
                    assembly = LoadAssemblyFromStream(fileStream, filepath.EndsWith(".gz") ? CompressionTypes.Gzip : CompressionTypes.Deflate);
                    assembly.Register(filepath);
                }
            }
            else
            {
                // Use Assembly.LoadFrom or Assembly.Load, as Assembly.LoadFile ignores the fact that an assembly was already loaded (and just loads it double).
                if (File.Exists(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, Path.GetFileName(filepath))))
                {
                    assembly = Assembly.Load(Path.GetFileNameWithoutExtension(filepath));
                }
                else
                {
                    // The file is outside of the AppDomain.CurrentDomain.BaseDirectory, so we need to use LoadFrom
                    assembly = Assembly.LoadFrom(filepath);
                }

                // Register the assembly in the cache, by name and by path
                assembly.Register(filepath);
            }

            // Make sure the directory of the file is known to the resolver
            // this takes care of dlls which are in the same directory as this assembly.
            // It only makes sense if this method was called directly, but as the ResolveDirectories is a set, it doesn't hurt.
            var assemblyDirectory = Path.GetDirectoryName(filepath);
            if (!string.IsNullOrEmpty(assemblyDirectory))
            {
                lock (ResolveDirectories)
                {
                    Log.Verbose().WriteLine("Added {0} for resolving relative to {1}", assemblyDirectory, filepath);
                    ResolveDirectories.Add(assemblyDirectory);
                }
            }
            return assembly;
        }

        /// <summary>
        ///     Simple method to load an assembly from a stream
        /// </summary>
        /// <param name="assemblyStream">Stream</param>
        /// <param name="compressionType">specify the compression type for the stream</param>
        /// <param name="checkCache">specify if the cache needs to be checked, this costs performance</param>
        /// <returns>Assembly or null when the stream is null</returns>
        public static Assembly LoadAssemblyFromStream(Stream assemblyStream, CompressionTypes compressionType = CompressionTypes.None, bool checkCache = false)
        {
            if (assemblyStream == null)
            {
                return null;
            }

            byte[] assemblyBytes;

            if (compressionType == CompressionTypes.Gzip)
            {
                using (var stream = new GZipStream(assemblyStream, CompressionMode.Decompress, true))
                {
                    assemblyBytes = stream.ToByteArray();
                }
            }
            else if (compressionType == CompressionTypes.Deflate)
            {
                using (var stream = new DeflateStream(assemblyStream, CompressionMode.Decompress, true))
                {
                    assemblyBytes = stream.ToByteArray();
                }
            }
            else
            {
                assemblyBytes = assemblyStream.ToByteArray();
            }

            if (checkCache)
            {
                // Create a temp-file to write the "stream" to, so the assembly name can be read
                string fileName = Path.GetTempPath() + Guid.NewGuid() + ".dll";
                try
                {
                    using (var tmpFileStream = new FileStream(fileName, FileMode.CreateNew))
                    {
                        tmpFileStream.Write(assemblyBytes, 0, assemblyBytes.Length);
                    }
                    var assemblyName = AssemblyName.GetAssemblyName(fileName);
                    lock (AssembliesByName)
                    {
                        Assembly cachedAssembly = FindCachedAssemblyByAssemblyName(assemblyName.Name);
                        if (cachedAssembly != null)
                        {
                            return cachedAssembly;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Log.Warn().WriteLine(ex, "Couldn't get assembly name, skipping cache.");
                }
                finally
                {
                    File.Delete(fileName);
                }
            }
            var assembly = Assembly.Load(assemblyBytes);
            assembly.Register();
            return assembly;
        }

        /// <summary>
        ///     Find the specified assemblies from a manifest resource or from the file system.
        ///     It is possible to use wildcards but the first match will be loaded!
        /// </summary>
        /// <param name="assemblyNames">IEnumerable with the assembly names, e.g. from AssemblyName.Name, do not specify an extension</param>
        /// <param name="extensions">IEnumerable with extensions to look for, defaults will be set if null was passed</param>
        /// <returns>IEnumerable with Assembly</returns>
        public static IEnumerable<Assembly> FindAssemblies(IEnumerable<string> assemblyNames, IEnumerable<string> extensions = null)
        {
            var extensionsList = extensions?.ToList();
            foreach (var assemblyName in assemblyNames)
            {
                yield return FindAssembly(assemblyName, extensionsList);
            }
        }

        /// <summary>
        ///     Find the specified assembly from a manifest resource or from the file system.
        ///     It is possible to use wildcards but the first match will be loaded!
        /// </summary>
        /// <param name="assemblyName">string with the assembly name, e.g. from AssemblyName.Name, do not specify an extension</param>
        /// <param name="extensions">IEnumerable with extensions to look for, defaults will be set if null was passed</param>
        /// <returns>Assembly or null</returns>
        public static Assembly FindAssembly(string assemblyName, IEnumerable<string> extensions = null)
        {
            Assembly assembly;
            // Do not use the cache if a wildcard was used.
            if (!assemblyName.Contains("*"))
            {
                assembly = FindCachedAssemblyByAssemblyName(assemblyName);
                if (assembly != null)
                {
                    return assembly;
                }
            }

            var extensionsList = extensions?.ToList();
            // Loading order depends on ResolveEmbeddedBeforeFiles
            if (ResolveEmbeddedBeforeFiles)
            {
                assembly = LoadEmbeddedAssembly(assemblyName, extensionsList) ?? LoadAssemblyFromFileSystem(assemblyName, extensionsList);
            }
            else
            {
                assembly = LoadAssemblyFromFileSystem(assemblyName, extensionsList) ?? LoadEmbeddedAssembly(assemblyName, extensionsList);
            }

            return assembly;
        }

        /// <summary>
        ///     Load the specified assembly from a manifest resource, or return null
        /// </summary>
        /// <param name="assemblyName">string</param>
        /// <param name="extensions">IEnumerable with extensions to look for, defaults will be set if null was passed</param>
        /// <returns>Assembly</returns>
        public static Assembly LoadEmbeddedAssembly(string assemblyName, IEnumerable<string> extensions = null)
        {
            var assemblyRegex = FileTools.FilenameToRegex(assemblyName, extensions ?? Extensions);
            try
            {
                var resourceTuple = AssemblyCache.FindEmbeddedResources(assemblyRegex).FirstOrDefault();
                if (resourceTuple != null)
                {
                    return resourceTuple.Item1.LoadEmbeddedAssembly(resourceTuple.Item2);
                }
            }
            catch (Exception ex)
            {
                Log.Error().WriteLine("Error loading {0} from manifest resources: {1}", assemblyName, ex.Message);
            }
            return null;
        }

        /// <summary>
        ///     Load the specified assembly from an embedded (manifest) resource, or return null
        /// </summary>
        /// <param name="assembly">Assembly to load the resource from</param>
        /// <param name="resourceName">Name of the embedded resource for the assembly to load</param>
        /// <returns>Assembly</returns>
        public static Assembly LoadEmbeddedAssembly(this Assembly assembly, string resourceName)
        {
            if (CheckEmbeddedResourceNameAgainstCache)
            {
                var possibleAssemblyName = FileTools.RemoveExtensions(resourceName, Extensions);

                var cachedAssembly = FindCachedAssemblyByAssemblyName(possibleAssemblyName);
                if (cachedAssembly != null)
                {
                    Log.Warn().WriteLine("Cached assembly {0} found for resource {1}, if this is not correct disable this by setting CheckEmbeddedResourceNameAgainstCache to false", cachedAssembly.FullName, resourceName);
                    return cachedAssembly;
                }
            }
            using (var stream = assembly.GetEmbeddedResourceAsStream(resourceName))
            {
                Log.Verbose().WriteLine("Loading assembly from resource {0} in assembly {1}", resourceName, assembly.FullName);
                return LoadAssemblyFromStream(stream, CompressionTypes.None, !CheckEmbeddedResourceNameAgainstCache);
            }
        }

        /// <summary>
        ///     Load the specified assembly from the ResolveDirectories, or return null
        /// </summary>
        /// <param name="assemblyName">string with the name without path</param>
        /// <param name="extensions">IEnumerable with extensions to look for, defaults will be set if null was passed</param>
        /// <returns>Assembly</returns>
        public static Assembly LoadAssemblyFromFileSystem(string assemblyName, IEnumerable<string> extensions = null)
        {
            return LoadAssemblyFromFileSystem(ResolveDirectories, assemblyName, extensions);
        }

        /// <summary>
        ///     Load the specified assembly from the specified directories, or return null
        /// </summary>
        /// <param name="directories">IEnumerable with directories</param>
        /// <param name="assemblyName">string with the name without path</param>
        /// <param name="extensions">IEnumerable with extensions to look for, defaults will be set if null was passed</param>
        /// <returns>Assembly</returns>
        public static Assembly LoadAssemblyFromFileSystem(IEnumerable<string> directories, string assemblyName, IEnumerable<string> extensions = null)
        {
            var assemblyRegex = FileTools.FilenameToRegex(assemblyName, extensions ?? Extensions);
            var filepath = FileLocations.Scan(directories, assemblyRegex).Select(x => x.Item1).FirstOrDefault();
            if (!string.IsNullOrEmpty(filepath) && File.Exists(filepath))
            {
                try
                {
                    return LoadAssemblyFromFile(filepath);
                }
                catch (Exception ex)
                {
                    // don't log with other libraries as this might cause issues / recurse resolving
                    Log.Error().WriteLine("Error loading assembly from file {0}: {1}", filepath, ex.Message);
                }
            }
            return null;
        }
    }
}