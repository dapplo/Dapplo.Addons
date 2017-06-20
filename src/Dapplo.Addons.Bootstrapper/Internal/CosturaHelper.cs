#region Dapplo 2016-2017 - GNU Lesser General Public License

// Dapplo - building blocks for .NET applications
// Copyright (C) 2016-2017 Dapplo
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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using Dapplo.Addons.Bootstrapper.Extensions;
using Dapplo.Log;

namespace Dapplo.Addons.Bootstrapper.Internal
{
    /// <summary>
    /// A helper class for using Costura embedded assemblies
    /// </summary>
    internal class CosturaHelper
    {
        private static readonly LogSource Log = new LogSource();
        private const string CosturaPrefix = "costura.";
        private const string CosturaPostfix = ".compressed";
        private const string AssemblyLoaderTypeName = "Costura.AssemblyLoader";
        private readonly MethodInfo _readFromEmbeddedResourcesMethodInfo;

        /// <summary>
        /// all the assemblies which Costura has available
        /// </summary>
        public IDictionary<string, string> AssembliesAsResources { get; }

        /// <summary>
        /// All the symbols which Costura has available 
        /// </summary>
        public IDictionary<string, string> SymbolsAsResources { get; }

        /// <summary>
        /// Construct a CosturaHelper
        /// </summary>
        public CosturaHelper()
        {
            var assemblyLoaderType = Assembly.GetEntryAssembly()?.GetType(AssemblyLoaderTypeName);
            var assembliesAsResourcesFieldInfo = assemblyLoaderType?.GetField("assemblyNames", BindingFlags.Static | BindingFlags.NonPublic);
            if (assembliesAsResourcesFieldInfo == null)
            {
                return;
            }

            AssembliesAsResources = (IDictionary<string, string>)assembliesAsResourcesFieldInfo.GetValue(null);

            var symbolsAsResourcesFieldInfo = assemblyLoaderType.GetField("symbolNames", BindingFlags.Static | BindingFlags.NonPublic);
            SymbolsAsResources = (IDictionary<string, string>)symbolsAsResourcesFieldInfo?.GetValue(null);

            _readFromEmbeddedResourcesMethodInfo = assemblyLoaderType.GetMethod("ReadFromEmbeddedResources", BindingFlags.Static | BindingFlags.NonPublic);
            if (_readFromEmbeddedResourcesMethodInfo != null)
            {
                IsActive = true;
            }
        }

        /// <summary>
        /// Checks if the specified resource is embedded by Costura
        /// </summary>
        /// <param name="resourceName">For instance an assembly name like: Dapplo.Addons.dll</param>
        /// <returns>true if it was found</returns>
        public bool HasResource(string resourceName)
        {
            return AssembliesAsResources.Any(pair => string.Equals(pair.Value, $"{CosturaPrefix}{resourceName}{CosturaPostfix}", StringComparison.InvariantCultureIgnoreCase) ||
                                                     string.Equals(pair.Value, $"{CosturaPrefix}{resourceName}", StringComparison.InvariantCultureIgnoreCase));
        }

        /// <summary>
        /// Tells if Costura is active
        /// </summary>
        public bool IsActive { get;  }

        /// <summary>
        /// Load the, by costura, embedded assemblies which match the pattern
        /// </summary>
        /// <param name="pattern">Regex to match the embedded assemblies against</param>
        /// <returns>IEnumerable with assemblies</returns>
        public IEnumerable<Assembly> LoadEmbeddedAssemblies(Regex pattern)
        {
            // Skip the prefix in the pattern matching
            return AssembliesAsResources.Where(pair => pattern.IsMatch(pair.Value.Substring(CosturaPrefix.Length))).Select(assemblyKeyValuePair =>
            {
                var loadedAssembly = AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(assembly => string.Equals(assembly.GetName().Name, assemblyKeyValuePair.Key, StringComparison.InvariantCultureIgnoreCase));
                if (loadedAssembly != null)
                {
                    Log.Verbose().WriteLine("Returning assembly '{0}' which was already loaded from: {1}", loadedAssembly.FullName, loadedAssembly.GetLocation() ?? "N.A.");
                    return loadedAssembly;
                }
                Log.Verbose().WriteLine("Forcing load from Costura packed assembly '{0}'", assemblyKeyValuePair.Key);

                return _readFromEmbeddedResourcesMethodInfo.Invoke(null, new object[] { AssembliesAsResources, SymbolsAsResources, new AssemblyName(assemblyKeyValuePair.Key) }) as Assembly;
            }).Where(assembly => assembly != null);
        }
    }
}
