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
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
using System.ComponentModel.Composition.Primitives;
using Dapplo.Log;
using Dapplo.Utils.Resolving;

#endregion

namespace Dapplo.Addons.Bootstrapper.ExportProviders
{
	/// <summary>
	///     The ServiceProviderExportProvider is an ExportProvider which can solve special cases by using
	///     an IServiceLocator implementation to resolve type requests. Meaning it can do last minute dynamic lookups.
	///     The IServiceLocator will create the type derrived classes, and this ExportProvider will create the export so it can be injected
	/// </summary>
	public class ServiceProviderExportProvider : ExportProvider
	{
		private static readonly LogSource Log = new LogSource();

		/// <summary>
		/// Type-Cache for all the ServiceExportProvider 
		/// </summary>
		protected static readonly IDictionary<string, Type> TypeLookupDictionary = new ConcurrentDictionary<string, Type>(StringComparer.OrdinalIgnoreCase);

		// ReSharper disable once StaticMemberInGenericType
		private readonly IBootstrapper _bootstrapper;
		private readonly IServiceProvider _serviceProvider;
		private readonly IDictionary<string, Export> _lookup = new Dictionary<string, Export>(StringComparer.OrdinalIgnoreCase);

		/// <summary>
		///     Create a ServiceExportProvider which is for the specified application, IConfigProvider and works with the supplied
		///     assemblies
		/// </summary>
		/// <param name="serviceProvider">provider needed for the registering</param>
		/// <param name="bootstrapper">IBootstrapper</param>
		public ServiceProviderExportProvider(IServiceProvider serviceProvider, IBootstrapper bootstrapper)
		{
			_serviceProvider = serviceProvider;
			_bootstrapper = bootstrapper;
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

			// Create / get instance
			var instance = _serviceProvider.GetService(contractType);
			if (instance == null)
			{
				// So we couldn't get an instance, add null so we don't try again.
				_lookup.Add(specifiedContractName, null);
				return null;
			}

			// Make sure it's exported (is this needed?)
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
				Log.Verbose().WriteLine("Searching for an export {0}", definition.ContractName);
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

					// Store the Type to the contract name, so we can find the type quicker the next time a request was made to a different ServiceExportProvider
					TypeLookupDictionary[definition.ContractName] = contractType;
					break;
				}
			}

			// So we found a type, try to create a export for it. 
			export = CreateExport(contractType, definition.ContractName);
			if (export == null)
			{
				// No export
				Log.Verbose().WriteLine("Couldn't find type for {0}", definition.ContractName);
				yield break;
			}
			Log.Verbose().WriteLine("Export for {0} found as type {1}" , definition.ContractName, contractType);
			yield return export;
		}
	}
}