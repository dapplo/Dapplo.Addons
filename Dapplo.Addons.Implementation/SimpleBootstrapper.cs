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

using System.IO;
using System.Reflection;
using Dapplo.Addons.Implementation.Internals;

namespace Dapplo.Addons.Implementation
{
	/// <summary>
	/// A simple bootstrapper, takes the executing assembly and adds it
	/// </summary>
	public class SimpleBootstrapper : CompositionBootstrapper
	{
		protected override void ConfigureAggregateCatalog()
		{
			base.ConfigureAggregateCatalog();
			// Add executing assembly
			Add(Assembly.GetExecutingAssembly());
		}

		/// <summary>
		/// Scan the supplied directory for assemblies, add it to the catalog
		/// </summary>
		/// <param name="directory"></param>
		/// <param name="pattern">default is *.dll</param>
		public void ScanDirectory(string directory, string pattern = "*.dll")
		{
			if (!Directory.Exists(directory))
			{
				return;
			}
			//_pluginPaths.Add(addonPath);
			AggregateCatalog.Catalogs.Add(new SafeDirectoryCatalog(directory, pattern));
		}
	}
}
