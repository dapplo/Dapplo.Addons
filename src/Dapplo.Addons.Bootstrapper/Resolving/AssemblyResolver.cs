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

            var assemblyRegex = new Regex(@"^(costura\.)*" + assemblyName.Name.Replace(".", @"\.") + @"\.dll(\.compressed|\*.gz)*$", RegexOptions.IgnoreCase);
            foreach (var loadedAssembly in LoadedAssemblies.Where(pair => !AssembliesToIgnore.IsMatch(pair.Key)).Select(pair => pair.Value))
            {
                var resources = Resources.GetCachedManifestResourceNames(loadedAssembly);
                foreach (var resource in resources)
                {
                    if (!assemblyRegex.IsMatch(resource))
                    {
                        continue;
                    }
                    // Match
                    using (var stream = Resources.GetEmbeddedResourceAsStream(loadedAssembly, resource, false))
                    {
                        Log.Verbose().WriteLine("Resolved {0} from {1}", assemblyName.Name, loadedAssembly.GetName().Name);
                        return Assembly.Load(stream.ToByteArray());
                    }
                }
            }
            Log.Warn().WriteLine("Couldn't resolve {0}", assemblyName.Name);
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
