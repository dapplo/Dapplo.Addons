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
        /// The constructor of the Assembly Resolver
        /// </summary>
        public AssemblyResolver()
        {
            Resources = new ManifestResources(assemblyName => LoadedAssemblies.ContainsKey(assemblyName) ? LoadedAssemblies[assemblyName] : null);

            foreach (var loadedAssembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                LoadedAssemblies[loadedAssembly.GetName().Name] = loadedAssembly;
            }

            // Register assembly loading
            AppDomain.CurrentDomain.AssemblyLoad += AssemblyLoad;
            // Register assembly resolving
            AppDomain.CurrentDomain.AssemblyResolve += AssemblyResolve;
        }

        /// <summary>
        /// Load an assembly from the specified filename, if the assembly was already loaded skip it.
        /// </summary>
        /// <param name="filename">string</param>
        /// <returns>bool</returns>
        public bool LoadAssembly(string filename)
        {
            var assemblyName = Path.GetFileNameWithoutExtension(filename);
            if (string.IsNullOrEmpty(assemblyName))
            {
                return false;
            }
            if (LoadedAssemblies.ContainsKey(assemblyName))
            {
                Log.Debug().WriteLine("Skipping {0} as the assembly was already loaded.", filename);
                return false;
            }

            try
            {
                Assembly.LoadFrom(filename);
                return true;
            }
            catch (Exception ex)
            {
                Log.Error().WriteLine(ex, "Couldn't load assembly from file {0}", filename);
            }

            return false;
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
            if (assemblyName.Name.EndsWith(".resources"))
            {
                // Ignore to prevent resolving logic which doesn't find anything anyway
                if (Log.IsVerboseEnabled())
                {
                    Log.Verbose().WriteLine("Ignoring resolve event for {0}", assemblyName.Name);
                }
                return null;
            }

            if (LoadedAssemblies.TryGetValue(assemblyName.Name, out var assembly))
            {
                Log.Info().WriteLine("Returned {0} from cache.", assemblyName.Name);
                return assembly;
            }

            return LoadEmbeddedAssembly(assemblyName.Name);
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
            if (LoadedAssemblies.TryGetValue(assemblyName, out var assembly))
            {
                Log.Info().WriteLine("Returned {0} from cache.", assemblyName);
                return assembly;
            }
            foreach (var loadedAssembly in LoadedAssemblies.Where(pair => !AssembliesToIgnore.IsMatch(pair.Key)).Select(pair => pair.Value))
            {
                var resources = Resources.GetCachedManifestResourceNames(loadedAssembly);
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
                    using (var stream = Resources.GetEmbeddedResourceAsStream(loadedAssembly, resource, false))
                    {
                        Log.Verbose().WriteLine("Resolved {0} from {1}", assemblyName, loadedAssembly.GetName().Name);
                        return Assembly.Load(stream.ToByteArray());
                    }
                }
            }
            Log.Warn().WriteLine("Couldn't locate {0}", assemblyName);
            return null;
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
