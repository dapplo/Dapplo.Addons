/*
 * dapplo - building blocks for desktop applications
 * Copyright (C) 2015 Robin Krom
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
using System.IO;
using System.Linq;
using System.Reflection;

namespace Dapplo.Addons.Implementation
{
	/// <summary>
	/// A simple bootstrapper, takes the executing assembly and adds it
	/// </summary>
	public class SimpleBootstrapper : CompositionBootstrapper
	{
		/// <summary>
		/// Constructor for the SimpleBootstrapper
		/// </summary>
		public SimpleBootstrapper()
		{
			AppDomain.CurrentDomain.AssemblyResolve += AddonResolveEventHandler;
		}

		/// <summary>
		/// A resolver which takes care of loading DLL's which are referenced from AddOns but not found
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="resolveEventArgs"></param>
		/// <returns>Assembly</returns>
		protected Assembly AddonResolveEventHandler(object sender, ResolveEventArgs resolveEventArgs)
		{
			var addonDirectories = (from addonFile in AddonFiles
								   select Path.GetDirectoryName(addonFile)).Distinct();

            foreach (var directory in addonDirectories)
			{

				var assemblyFile = Path.Combine(directory, GetAssemblyName(resolveEventArgs) + ".dll");
				if (!File.Exists(assemblyFile))
				{
					continue;
				}
				return Assembly.LoadFile(assemblyFile);
			}
			return null;
		}

		/// <summary>
		/// Helper to get the assembly name
		/// </summary>
		/// <param name="args"></param>
		/// <returns></returns>
		private string GetAssemblyName(ResolveEventArgs args)
		{
			String name;
			if (args.Name.IndexOf(",") > -1)
			{
				name = args.Name.Substring(0, args.Name.IndexOf(","));
			}
			else
			{
				name = args.Name;
			}
			return name;
		}

		/// <summary>
		/// Configure the AggregateCatalog, by adding the default assemblies
		/// </summary>
		protected override void ConfigureAggregateCatalog()
		{
			if (_aggregateCatalogConfigured)
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
	}
}
