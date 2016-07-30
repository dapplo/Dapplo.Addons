//  Dapplo - building blocks for desktop applications
//  Copyright (C) 2016 Dapplo
// 
//  For more information see: http://dapplo.net/
//  Dapplo repositories are hosted on GitHub: https://github.com/dapplo
// 
//  This file is part of Dapplo.Addons
// 
//  Dapplo.Addons is free software: you can redistribute it and/or modify
//  it under the terms of the GNU Lesser General Public License as published by
//  the Free Software Foundation, either version 3 of the License, or
//  (at your option) any later version.
// 
//  Dapplo.Addons is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//  GNU Lesser General Public License for more details.
// 
//  You should have a copy of the GNU Lesser General Public License
//  along with Dapplo.Addons. If not, see <http://www.gnu.org/licenses/lgpl.txt>.

#region using

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
using System.ComponentModel.Composition.Primitives;
using Dapplo.Log.Facade;
using Dapplo.Config;

#endregion

namespace Dapplo.Addons.Bootstrapper.ExportProviders
{
	/// <summary>
	///     The ConfigExportProvider takes care of resolving MEF imports for Dapplo.Config based types.
	///     It will register and create the type derrived classes, and return the export so it can be injected
	/// </summary>
	public class ConfigExportProvider<TExportType> : ExportProvider
	{
		// ReSharper disable once StaticMemberInGenericType
		private static readonly LogSource Log = new LogSource();
		private readonly IBootstrapper _bootstrapper;
		private readonly IConfigProvider _configProvider;
		private readonly IDictionary<string, Export> _loopup = new Dictionary<string, Export>(StringComparer.OrdinalIgnoreCase);

		/// <summary>
		///     Create a ConfigExportProvider which is for the specified application, IConfigProvider and works with the supplied
		///     assemblies
		/// </summary>
		/// <param name="configProvider">provider needed for the registering</param>
		/// <param name="bootstrapper">IBootstrapper</param>
		public ConfigExportProvider(IConfigProvider configProvider, IBootstrapper bootstrapper)
		{
			_configProvider = configProvider;
			_bootstrapper = bootstrapper;
		}

		/// <summary>
		///     Try to find the instance for the type that wants to be imported, and get/register it.
		/// </summary>
		/// <param name="definition">ImportDefinition</param>
		/// <param name="atomicComposition">AtomicComposition</param>
		/// <returns>Export</returns>
		protected override IEnumerable<Export> GetExportsCore(ImportDefinition definition, AtomicComposition atomicComposition)
		{
			Export export;
			// See if we already cached the value
			if (_loopup.TryGetValue(definition.ContractName, out export))
			{
				if (export != null)
				{
					yield return export;
				}
			}
			else
			{
				Log.Verbose().WriteLine("Searching for an export {0}", definition.ContractName);
				// Loop over all the supplied assemblies, these should come from the bootstrapper
				foreach (var assembly in _bootstrapper.KnownAssemblies)
				{
					// Make an AssemblyQualifiedName from the contract name
					var assemblyQualifiedName = $"{definition.ContractName}, {assembly.FullName}";

					Log.Verbose().WriteLine("Checking if {0} can be found in {1}", definition.ContractName, assembly.FullName);

					// Try to get it, don't throw an exception if not found
					Type contractType;
					try
					{
						contractType = Type.GetType(assemblyQualifiedName, false, true);
					}
					catch
					{
						// Ignore & break the loop at it is most likely a problem with the contract name
						break;
					}

					// Go to next assembly if it wasn't found
					if (contractType == null)
					{
						// Add null value, so we don't try it again
						continue;
					}

					if (contractType == typeof(TExportType))
					{
						// We can't export the base type itself
						break;
					}

					// Check if it is derrived from the exporting base type
					if (!typeof(TExportType).IsAssignableFrom(contractType))
					{
						continue;
					}

					// Generate the export & meta-data
					var metadata = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
					{
						{CompositionConstants.ExportTypeIdentityMetadataName, AttributedModelServices.GetTypeIdentity(contractType)}
					};

					// Create instance
					var instance = _configProvider.Get(contractType);

					// Make sure it's exported
					var contractName = AttributedModelServices.GetContractName(contractType);
					_bootstrapper?.Export(contractName, instance);

					// create the export so we can store and return it
					export = new Export(new ExportDefinition(contractName, metadata), () => instance);
					// store the export for fast retrieval
					_loopup.Add(definition.ContractName, export);
					yield return export;
					// Nothing more to do, break
					yield break;
				}
				Log.Verbose().WriteLine("Marking {0} as not a {1}", definition.ContractName, typeof(TExportType));

				// Add null value, so we don't try it again
				_loopup.Add(definition.ContractName, null);
			}
		}
	}
}