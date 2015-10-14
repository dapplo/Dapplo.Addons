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
using System.Collections.Generic;
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
			base.ConfigureAggregateCatalog();

			// Add the base assemblies, but not the current (as this is Dapplo.Addons)
			var entryAssembly = Assembly.GetEntryAssembly();
			Add(entryAssembly);
			var callingAssembly = Assembly.GetCallingAssembly();
			if (callingAssembly != entryAssembly)
			{
				Add(callingAssembly);
			}
			if (callingAssembly == null && entryAssembly == null)
			{
				var executingAssembly = Assembly.GetExecutingAssembly();
				Add(executingAssembly);
            }
		}

		/// <summary>
		/// Simple "service-locater"
		/// </summary>
		/// <typeparam name="T">Type to locate</typeparam>
		/// <returns>Lazy T</returns>
		public Lazy<T> GetExport<T>()
		{
			return Container.GetExport<T>();
		}

		/// <summary>
		/// Simple "service-locater" with meta-data
		/// </summary>
		/// <typeparam name="T">Type to locate</typeparam>
		/// <typeparam name="TMetaData">Type for the meta-data</typeparam>
		/// <returns>Lazy T,TMetaData</returns>
		public Lazy<T, TMetaData> GetExport<T, TMetaData>()
		{
			return Container.GetExport<T, TMetaData>();
		}

		/// <summary>
		/// Simple "service-locater" to get multiple exports
		/// </summary>
		/// <typeparam name="T">Type to locate</typeparam>
		/// <returns>IEnumerable of Lazy T</returns>
		public IEnumerable<Lazy<T>> GetExports<T>()
		{
			return Container.GetExports<T>();
		}

		/// <summary>
		/// Simple "service-locater" to get multiple exports with meta-data
		/// </summary>
		/// <typeparam name="T">Type to locate</typeparam>
		/// <typeparam name="TMetaData">Type for the meta-data</typeparam>
		/// <returns>IEnumerable of Lazy T,TMetaData</returns>
		public IEnumerable<Lazy<T, TMetaData>> GetExports<T, TMetaData>()
		{
			return Container.GetExports<T, TMetaData>();
		}
	}
}
