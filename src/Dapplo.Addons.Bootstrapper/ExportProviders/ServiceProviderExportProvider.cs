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

#region Usings

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
using System.ComponentModel.Composition.Primitives;
using System.Linq;
using System.Text.RegularExpressions;
using Dapplo.Log;
using Dapplo.Utils.Resolving;

#endregion

namespace Dapplo.Addons.Bootstrapper.ExportProviders
{
	/// <summary>
	///     The ServiceProviderExportProvider is an ExportProvider which can solve special cases by using IServiceLocator implementations to resolve type requests.
	///     Meaning it can do last minute dynamic lookups. The IServiceLocator will create the type derrived classes, and this ExportProvider will create the export so it can be injected.
	/// </summary>
	internal sealed class ServiceProviderExportProvider : ExportProvider
	{
		private static readonly LogSource Log = new LogSource();
		private static readonly IDictionary<string, Regex> IgnoreContractRegexes = new Dictionary<string, Regex>
		{
			{"System", new Regex(@"^System\..+", RegexOptions.Compiled)},
			{"Dapplo.Addons interface", new Regex(@"^Dapplo\.Addons\.I[^\.]+$", RegexOptions.Compiled)},
			{"No FQ-type", new Regex(@"^[a-z]+$", RegexOptions.Compiled)}
		};
		private static readonly IList<Regex> IgnoreAssemblyRegexes = new List<Regex> {
			new Regex(@"^System\..+", RegexOptions.Compiled),
			new Regex(@"^Dapplo\.(Addons|InterfaceImpl|Utils)$", RegexOptions.Compiled),
			new Regex(@"^Dapplo\.Addons.\Bootstrapper$", RegexOptions.Compiled),
			new Regex(@"^Dapplo\.Log\.\w+$", RegexOptions.Compiled)
		};

		/// <summary>
		/// Type-Cache for all the ServiceExportProvider 
		/// </summary>
		private readonly IDictionary<string, Type> _typeLookupDictionary = new ConcurrentDictionary<string, Type>(StringComparer.OrdinalIgnoreCase);

		private readonly IBootstrapper _bootstrapper;
		private readonly IDictionary<string, Export> _lookup = new Dictionary<string, Export>(StringComparer.OrdinalIgnoreCase);

		/// <summary>
		///     Create a ServiceExportProvider which is for the specified application, IConfigProvider and works with the supplied
		///     assemblies
		/// </summary>
		/// <param name="bootstrapper">IBootstrapper</param>
		public ServiceProviderExportProvider(IBootstrapper bootstrapper)
		{
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
				_lookup[specifiedContractName] = export;
				return export;
			}

			// Create / get instance from the first provider which responds, ignore the bootstrapper itself
			var instance = _bootstrapper.GetExports<IServiceProvider>().Select(lazy => lazy.Value).Where(provider => !ReferenceEquals(_bootstrapper, provider)).Select(provider => provider.GetService(contractType)).FirstOrDefault(o => o != null);
			if (instance == null)
			{
				// So we couldn't get an instance, add null so we don't try again.
				_lookup[specifiedContractName] = null;
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
			_lookup[contractName] = export;
			if (specifiedContractName != null && !contractName.Equals(specifiedContractName))
			{
				_lookup[specifiedContractName] = export;
			}
			_typeLookupDictionary[contractName] = contractType;
			return export;
		}

		/// <summary>
		/// Do the actual resolving, try to find out what type is wanted
		/// </summary>
		/// <param name="definition">ImportDefinition</param>
		/// <param name="contractType">Type or null</param>
		/// <returns>true if found</returns>
		private bool TryToResolveType(ImportDefinition definition, out Type contractType)
		{
			if (!_typeLookupDictionary.TryGetValue(definition.ContractName, out contractType))
			{
				Log.Verbose().WriteLine("Searching for an export {0}", definition.ContractName);
				// Loop over all the supplied assemblies, these should come from the bootstrapper
				foreach (var assembly in AssemblyResolver.AssemblyCache)
				{
					var currentAssemblyName = assembly.GetName().Name;
					// Skip assemblies which do not concern us
					if (IgnoreAssemblyRegexes.Any(regex => regex.IsMatch(currentAssemblyName)))
					{
						Log.Verbose().WriteLine("Skipping assembly {0}", currentAssemblyName);
						continue;
					}
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
						Log.Verbose().WriteLine("Type {0} couldn't be found in {1}", definition.ContractName, assembly.FullName);
						continue;
					}
					Log.Verbose().WriteLine("Found Type {0} in {1}", definition.ContractName, assembly.FullName);

					// Store the Type to the contract name, so we can find the type quicker the next time a request was made to a different ServiceExportProvider
					_typeLookupDictionary[definition.ContractName] = contractType;
					break;
				}
				// Check if type is not found, so we store this in the dictionary
				if (contractType == null)
				{
					_typeLookupDictionary[definition.ContractName] = null;
				}
			}

			// Log if the type was not found
			if (contractType == null)
			{
				Log.Verbose().WriteLine("Couldn't find type for {0}", definition.ContractName);
			}
			return contractType != null;
		}

		/// <summary>
		///     Try to find the instance for the type that wants to be imported, and get/register it.
		/// </summary>
		/// <param name="definition">ImportDefinition</param>
		/// <param name="atomicComposition">AtomicComposition</param>
		/// <returns>Export</returns>
		protected override IEnumerable<Export> GetExportsCore(ImportDefinition definition, AtomicComposition atomicComposition)
		{
			foreach (var ignoreRegex in IgnoreContractRegexes)
			{
				if (!ignoreRegex.Value.IsMatch(definition.ContractName))
				{
					continue;
				}
				Log.Verbose().WriteLine("Not resolving contract {0}, it was excluded due to rule: {1}.", definition.ContractName, ignoreRegex.Key);
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
			if (TryToResolveType(definition, out contractType))
			{
				// So we found a type, try to create a export for it. 
				try
				{
					export = CreateExport(contractType, definition.ContractName);
				}
				catch (Exception ex)
				{
					var message = $"Exception while creating an export for {definition.ContractName}";
					Log.Error().WriteLine(ex, message);
					throw new StartupException(message, ex);
				}
				if (export == null)
				{
					// No export
					Log.Verbose().WriteLine("Couldn't create export for {0}", definition.ContractName);
					yield break;
				}
				Log.Verbose().WriteLine("Export for {0} found as type {1}", definition.ContractName, contractType);
				yield return export;
			}
			else if (contractType == null)
			{
				// The type is not found
				Log.Verbose().WriteLine("Couldn't find type for {0}", definition.ContractName);
				_lookup.Add(definition.ContractName, null);
			}
		}
	}
}