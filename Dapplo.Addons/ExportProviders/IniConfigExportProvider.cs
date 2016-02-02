/*
	Dapplo - building blocks for desktop applications
	Copyright (C) 2015-2016 Dapplo

	For more information see: http://dapplo.net/
	Dapplo repositories are hosted on GitHub: https://github.com/dapplo

	This file is part of Dapplo.Addons

	Dapplo.Addons is free software: you can redistribute it and/or modify
	it under the terms of the GNU General Public License as published by
	the Free Software Foundation, either version 3 of the License, or
	(at your option) any later version.

	Dapplo.Addons is distributed in the hope that it will be useful,
	but WITHOUT ANY WARRANTY; without even the implied warranty of
	MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
	GNU General Public License for more details.

	You should have received a copy of the GNU General Public License
	along with Dapplo.Addons. If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
using System.ComponentModel.Composition.Primitives;
using System.Reflection;
using Dapplo.Config.Ini;
using Dapplo.LogFacade;

namespace Dapplo.Addons.ExportProviders
{
	/// <summary>
	/// This ExportProvider takes care of resolving MEF imports for the IniConfig
	/// It will register & create the IniSection derrived class, and return the export so it can be injected
	/// </summary>
	public class IniConfigExportProvider : ExportProvider
	{
		private static readonly LogSource Log = new LogSource();
		private readonly IniConfig _iniConfig;
		private readonly IList<Assembly> _assemblies;
		private readonly IDictionary<string, Export> _loopup = new Dictionary<string, Export>();
		private readonly IServiceLocator _serviceLocator;
		private readonly Type _iniSectionType = typeof(IIniSection);

		/// <summary>
		/// Create a IniConfigExportProvider which is for the specified applicatio, iniconfig and works with the supplied assemblies
		/// </summary>
		/// <param name="iniConfig">IniConfig needed for the registering, can be null for the current</param>
		/// <param name="assemblies">List of assemblies used for finding the type</param>
		/// <param name="serviceLocator"></param>
		public IniConfigExportProvider(IniConfig iniConfig, IList<Assembly> assemblies, IServiceLocator serviceLocator)
		{
			_iniConfig = iniConfig ?? IniConfig.Current;
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
				foreach (var assembly in _assemblies ?? new List<Assembly>())
				{
					// Make an AssemblyQualifiedName from the contract name
					var assemblyQualifiedName = $"{definition.ContractName}, {assembly.FullName}";

					Log.Verbose().WriteLine("Checking {0}", assemblyQualifiedName);

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
						continue;
					}

					if (contractType == _iniSectionType)
					{
						// We can't export the IIniSection itself
						break;
					}

					// Check if it is derrived from IIniSection
					if (!_iniSectionType.IsAssignableFrom(contractType))
					{
						continue;
					}

					// Generate the export & meta-data
					var metadata = new Dictionary<string, object>
					{
						{CompositionConstants.ExportTypeIdentityMetadataName, AttributedModelServices.GetTypeIdentity(contractType)}
					};

					// Create instance
					var instance = _iniConfig.RegisterAndGet(contractType);

					// Make sure it's exported
					string contractName = AttributedModelServices.GetContractName(contractType);
					_serviceLocator?.Export(contractName, instance);

					// create the export so we can store and return it
					export = new Export(new ExportDefinition(contractName, metadata), () => instance);
					// store the export for fast retrieval
					_loopup.Add(definition.ContractName, export);
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
