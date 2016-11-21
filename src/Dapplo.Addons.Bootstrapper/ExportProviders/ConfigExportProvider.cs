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
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
using System.ComponentModel.Composition.Primitives;
using System.Linq;
using Dapplo.Config;
using Dapplo.Log;
using Dapplo.Utils.Resolving;

#endregion

namespace Dapplo.Addons.Bootstrapper.ExportProviders
{
	/// <summary>
	///     The ConfigExportProvider takes care of resolving MEF imports for Dapplo.Config based types.
	///     It will register and create the type derrived classes, and return the export so it can be injected
	/// </summary>
	/// <typeparam name="TExportType">The actual exported type</typeparam>
	/// <typeparam name="TExportSubType">Sub-types are types which can be used to separate the TExportType into modular parts</typeparam>
	public class ConfigExportProvider<TExportType, TExportSubType> : BaseConfigExportProvider
	{
		// ReSharper disable once StaticMemberInGenericType
		private readonly IBootstrapper _bootstrapper;
		private readonly IConfigProvider _configProvider;
		private readonly IDictionary<string, Export> _lookup = new Dictionary<string, Export>(StringComparer.OrdinalIgnoreCase);
		private readonly Type _exportType = typeof(TExportType);
		private readonly Type _exportSubType = typeof(TExportSubType);

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

		/// <inheritdoc />
		public override void Initialize()
		{
			foreach (var assembly in AssemblyResolver.AssemblyCache.Where(a => !a.GetName().Name.StartsWith("System")))
			{
				var configTypes = assembly.DefinedTypes.Where(t => t.GUID != _exportType.GUID && !t.ContainsGenericParameters && _exportType.IsAssignableFrom(t));
				foreach (var configType in configTypes)
				{
					CreateExport(configType);
				}
			}
		}

		/// <summary>
		/// Create the export and store it for caching
		/// </summary>
		/// <param name="contractType"></param>
		/// <param name="specifiedContractName"></param>
		/// <returns>Export</returns>
		private Export CreateExport(Type contractType, string specifiedContractName = null)
		{
			Export export;

			if (specifiedContractName != null && _lookup.TryGetValue(specifiedContractName, out export))
			{
				return export;
			}

			var contractName = AttributedModelServices.GetContractName(contractType);

			if (_lookup.TryGetValue(contractName, out export))
			{
				// Don't forget to register
				_lookup.Add(specifiedContractName, export);
				return export;
			}
			// Create instance
			var instance = _configProvider.Get(contractType);

			// Make sure it's exported
			_bootstrapper?.Export(contractName, instance);

			// Generate the export & meta-data
			var metadata = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
			{
				{CompositionConstants.ExportTypeIdentityMetadataName, AttributedModelServices.GetTypeIdentity(contractType)}
			};
			// create the export so we can store and return it
			export = new Export(new ExportDefinition(contractName, metadata), () => instance);

			// store the export for fast retrieval
			_lookup.Add(contractName, export);
			if (specifiedContractName != null && !contractName.Equals(specifiedContractName))
			{
				_lookup.Add(specifiedContractName, export);
			}
			TypeLookupDictionary[contractName] = contractType;
			return export;
		}

		/// <summary>
		///     Try to find the instance for the type that wants to be imported, and get/register it.
		/// </summary>
		/// <param name="definition">ImportDefinition</param>
		/// <param name="atomicComposition">AtomicComposition</param>
		/// <returns>Export</returns>
		protected override IEnumerable<Export> GetExportsCore(ImportDefinition definition, AtomicComposition atomicComposition)
		{
			if (definition.ContractName.StartsWith("System."))
			{
				Log.Verbose().WriteLine("skipping contract {0} as it is from the system namespace.", definition.ContractName);
				yield break;
			}
			Export export;
			// See if we already cached the value
			if (_lookup.TryGetValue(definition.ContractName, out export))
			{
				if (export != null)
				{
					yield return export;
				}
				yield break;
			}
			Type contractType;

			if (!TypeLookupDictionary.TryGetValue(definition.ContractName, out contractType))
			{
				Log.Verbose().WriteLine("Searching for an export {0} and testing if it's a {1}", definition.ContractName, _exportType.Name);
				// Loop over all the supplied assemblies, these should come from the bootstrapper
				foreach (var assembly in AssemblyResolver.AssemblyCache)
				{
					// Try to get it, don't throw an exception if not found
					try
					{
						contractType = assembly.GetType(definition.ContractName, false, true);
					}
					catch (Exception ex)
					{
						Log.Verbose().WriteLine("Couldn't get type {0} due to {1}", definition.ContractName, ex.Message);
						// Ignore & break the loop at it is most likely a problem with the contract name
						break;
					}

					// Go to next assembly if it wasn't found
					if (contractType == null)
					{
						// Add null value, so we don't try it again
						Log.Verbose().WriteLine("Type {0} couldn't be found in {1}", definition.ContractName, assembly.FullName);
						continue;
					}
					Log.Verbose().WriteLine("Found Type {0} in {1}", definition.ContractName, assembly.FullName);

					// Store the Type to the contract name, so we can find the type quicker the next time a request was made to a different ConfigExportProvider
					TypeLookupDictionary[definition.ContractName] = contractType;
					break;
				}
			}

			if (contractType == null)
			{
				Log.Verbose().WriteLine("Couldn't find {0}", definition.ContractName);

				// Add null value, so we don't try it again
				_lookup.Add(definition.ContractName, null);
				yield break;
			}
			
			// Special test needed due to assembly loading?
			if (contractType.GUID == _exportType.GUID)
			{
				// We can't export the base type itself
				Log.Verbose().WriteLine("Skipping, can't export base type itself");
				_lookup.Add(definition.ContractName, null);
				yield break;
			}

			// Check if it is derrived from the exporting base type
			if (!_exportType.IsAssignableFrom(contractType))
			{
				Log.Verbose().WriteLine("Type {0} is not assignable to basetype {1}", contractType, _exportType.FullName);
				if (_exportSubType.IsAssignableFrom(contractType))
				{
					var subType = contractType;
					contractType = TypeLookupDictionary.Values.FirstOrDefault(s => _exportType.IsAssignableFrom(s) && subType.IsAssignableFrom(s));
					if (contractType != null)
					{
						Log.Verbose().WriteLine("Type {0} is a sub-type of 'parent' {1}", subType, contractType);

					}
				}
				else
				{
					contractType = null;
				}
				if (contractType == null)
				{
					_lookup.Add(definition.ContractName, null);
					yield break;
				}
			}

			export = CreateExport(contractType, definition.ContractName);

			yield return export;
		}
	}
}