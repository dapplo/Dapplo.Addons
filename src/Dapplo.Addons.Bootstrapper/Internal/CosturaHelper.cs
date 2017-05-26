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
        private const string AssemblyLoaderTypeName = "Costura.AssemblyLoader";
        private readonly MethodInfo _readFromEmbeddedResourcesMethodInfo;
        private readonly IDictionary<string, string> _assembliesAsResources;
        private readonly IDictionary<string, string> _symbolsAsResources;

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

            _assembliesAsResources = (IDictionary<string, string>)assembliesAsResourcesFieldInfo.GetValue(null);

            var symbolsAsResourcesFieldInfo = assemblyLoaderType.GetField("symbolNames", BindingFlags.Static | BindingFlags.NonPublic);
            _symbolsAsResources = (IDictionary<string, string>)symbolsAsResourcesFieldInfo?.GetValue(null);

            _readFromEmbeddedResourcesMethodInfo = assemblyLoaderType.GetMethod("ReadFromEmbeddedResources", BindingFlags.Static | BindingFlags.NonPublic);
            if (_readFromEmbeddedResourcesMethodInfo != null && _symbolsAsResources != null)
            {
                IsActive = true;
            }
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
            return _assembliesAsResources.Where(pair => pattern.IsMatch(pair.Value.Substring(CosturaPrefix.Length))).Select(assemblyKeyValuePair =>
            {
                var loadedAssembly = AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(assembly => assembly.FullName.ToLowerInvariant().Contains($"{assemblyKeyValuePair.Key},"));
                if (loadedAssembly != null)
                {
                    Log.Verbose().WriteLine("Returning already loaded assembly '{0}'", assemblyKeyValuePair.Key);
                    return loadedAssembly;
                }
                Log.Verbose().WriteLine("Forcing load from Costura packed assembly '{0}'", assemblyKeyValuePair.Key);

                return _readFromEmbeddedResourcesMethodInfo.Invoke(null, new object[] { _assembliesAsResources, _symbolsAsResources, new AssemblyName(assemblyKeyValuePair.Key) }) as Assembly;
            }).Where(assembly => assembly != null);
        }
    }
}
