/*
 * dapplo - building blocks for desktop applications
 * Copyright (C) Dapplo 2015-2016
 * 
 * For more information see: http://dapplo.net/
 * dapplo repositories are hosted on GitHub: https://github.com/dapplo
 * 
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 1 of the License, or
 * (at your option) any later version.
 * 
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 * 
 * You should have received a copy of the GNU General Public License
 * along with this program. If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
using System.ComponentModel.Composition.Primitives;
using System.Reflection;
using Dapplo.Config.Language;

namespace Dapplo.Addons.ExportProviders
{
	/// <summary>
	/// This ExportProvider takes care of resolving MEF imports for the IniConfig
	/// It will register & create the ILanguage derrived class, and return the export so it can be injected
	/// </summary>
	public class LanguageExportProvider : ExportProvider
	{
		private readonly LanguageLoader _languageLoader;
		private readonly IList<Assembly> _assemblies;
		private readonly IDictionary<string, Export> _loopup = new Dictionary<string, Export>();
		private readonly IServiceLocator _serviceLocator;
		private readonly Type _languageType = typeof(ILanguage);

		/// <summary>
		/// Create a LanguageExportProvider which is for the specified languageloader and works with the supplied assemblies
		/// </summary>
		/// <param name="languageLoader">LanguageLoader needed for the registering, can be null for the current</param>
		/// <param name="assemblies">List of assemblies used for finding the type</param>
		/// <param name="serviceLocator"></param>
		public LanguageExportProvider(LanguageLoader languageLoader, IList<Assembly> assemblies, IServiceLocator serviceLocator)
		{
			_languageLoader = languageLoader ?? LanguageLoader.Current;
			_assemblies = assemblies;
			_serviceLocator = serviceLocator;
		}

		/// <summary>
		/// Try to find the IniSection type that wants to be imported, and get/register it.
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
				// Loop over all the supplied assemblies, these should come from the bootstrapper
				foreach (var assembly in _assemblies)
				{
					// Make an AssemblyQualifiedName from the contract name
					var assemblyQualifiedName = $"{definition.ContractName}, {assembly.FullName}";
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

					if (contractType == _languageType)
					{
						// We can't export the ILanguage itself
						break;
					}

					// Check if it is derrived from ILanguage
					if (!_languageType.IsAssignableFrom(contractType))
					{
						continue;
					}

					// Generate the export & meta-data
					var metadata = new Dictionary<string, object>
					{
						{CompositionConstants.ExportTypeIdentityMetadataName, AttributedModelServices.GetTypeIdentity(contractType)}
					};

					//  Create instance
					var instance = _languageLoader.RegisterAndGet(contractType);
					// Make sure it's exported
					string contractName = AttributedModelServices.GetContractName(contractType);
					_serviceLocator?.Export(contractName, instance);

					// create the export so we can store and return it
					export = new Export(new ExportDefinition(contractName, metadata), () => instance);
					// store the export for fast retrieval
					_loopup.Add(definition.ContractName, export);
					// return it so the import can be made
					yield return export;
					// Nothing more to do, break
					yield break;
				}
				// Add null value, so we don't try it again
				_loopup.Add(definition.ContractName, null);
			}
		}
	}
}
