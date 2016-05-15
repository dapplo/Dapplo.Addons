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
using System.IO;
using System.Linq;
using System.Reflection;
using Dapplo.LogFacade;

#endregion

namespace Dapplo.Addons.Bootstrapper
{
	/// <summary>
	///     A simple bootstrapper, takes the executing assembly and adds it
	///     It also takes care of resolving events, so DLL's in the same directory as the Addon will be found
	/// </summary>
	public class SimpleBootstrapper : CompositionBootstrapper
	{
		private static readonly LogSource Log = new LogSource();
		private static readonly IDictionary<string, Assembly> Assemblies = new Dictionary<string, Assembly>(StringComparer.OrdinalIgnoreCase);

		/// <summary>
		///     Constructor for the SimpleBootstrapper
		/// </summary>
		public SimpleBootstrapper()
		{
			AppDomain.CurrentDomain.AssemblyResolve += AddonResolveEventHandler;
		}

		/// <summary>
		///     A resolver which takes care of loading DLL's which are referenced from AddOns but not found
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="resolveEventArgs"></param>
		/// <returns>Assembly</returns>
		protected Assembly AddonResolveEventHandler(object sender, ResolveEventArgs resolveEventArgs)
		{
			Assembly assembly;
			var assemblyName = GetAssemblyName(resolveEventArgs);
			if (!Assemblies.TryGetValue(assemblyName, out assembly))
			{
				Log.Verbose().WriteLine("Resolving name: {0}", resolveEventArgs.Name);
				var addonDirectories = (from addonFile in KnownFiles
					select Path.GetDirectoryName(addonFile)).Distinct();

				foreach (var directory in addonDirectories)
				{
					var assemblyFile = Path.Combine(directory, GetAssemblyName(resolveEventArgs) + ".dll");
					if (!File.Exists(assemblyFile))
					{
						continue;
					}
					try
					{
						assembly = Assembly.LoadFile(assemblyFile);
						Assemblies[assemblyName] = assembly;
						Log.Verbose().WriteLine("Loaded {0} to satisfy resolving {1}", assemblyFile, assemblyName);
					}
					catch (Exception ex)
					{
						Log.Error().WriteLine(ex, "Couldn't read {0}, to load {1}", assemblyFile, assemblyName);
					}
					break;
				}
			}
			return assembly;
		}

		/// <summary>
		///     Configure the AggregateCatalog, by adding the default assemblies
		/// </summary>
		protected override void ConfigureAggregateCatalog()
		{
			if (IsAggregateCatalogConfigured)
			{
				return;
			}
			base.ConfigureAggregateCatalog();

			// Add the entry assembly, which should be the application, but not the calling or executing (as this is Dapplo.Addons)
			var entryAssembly = Assembly.GetEntryAssembly();
			if (entryAssembly != null && entryAssembly != GetType().Assembly)
			{
				Add(entryAssembly);
			}
		}

		/// <summary>
		///     Helper to get the assembly name
		/// </summary>
		/// <param name="args"></param>
		/// <returns></returns>
		private static string GetAssemblyName(ResolveEventArgs args)
		{
			var indexOf = args.Name.IndexOf(",", StringComparison.Ordinal);
			return indexOf > -1 ? args.Name.Substring(0, indexOf) : args.Name;
		}
	}
}