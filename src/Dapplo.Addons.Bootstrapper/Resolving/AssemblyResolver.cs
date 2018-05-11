using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Dapplo.Log;

namespace Dapplo.Addons.Bootstrapper.Resolving
{
    /// <summary>
    /// This class supports the resolving of assemblies
    /// </summary>
    public class AssemblyResolver
    {
        private static readonly LogSource Log = new LogSource();

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
        /// <param name="args">ResolveEventArgs</param>
        /// <returns>Assembly</returns>
        private Assembly AssemblyResolve(object sender, ResolveEventArgs args)
        {
            if (!LoadedAssemblies.TryGetValue(args.Name, out var assembly))
            {
                return null;
            }

            Log.Info().WriteLine("Returned {0} from cache.", args.Name);
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

            LoadedAssemblies[assemblyName] = assembly;
        }
    }
}
