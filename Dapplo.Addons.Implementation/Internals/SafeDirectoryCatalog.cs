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
using System.ComponentModel.Composition.Hosting;
using System.ComponentModel.Composition.Primitives;
using System.IO;
using System.Linq;
using NLog;

namespace Dapplo.Addons.Implementation.Internals
{
	/// <summary>
	/// This makes sure we don't get problems loading plugins, see this stackoverflow article
	/// http://stackoverflow.com/questions/4144683/handle-reflectiontypeloadexception-during-mef-composition
	/// </summary>
	public class SafeDirectoryCatalog : ComposablePartCatalog
	{
		private static readonly Logger LOG = LogManager.GetCurrentClassLogger();
		private readonly AggregateCatalog _catalog;

		/// <summary>
		/// Constructor for DLL's in a directory
		/// </summary>
		/// <param name="directory"></param>
		public SafeDirectoryCatalog(string directory)
			: this(directory, "*.dll")
		{
		}

		/// <summary>
		/// Constructor for files which don't end on .dll
		/// </summary>
		/// <param name="directory"></param>
		/// <param name="pattern"></param>
		public SafeDirectoryCatalog(string directory, string pattern)
		{
			if (!Directory.Exists(directory))
			{
				throw new ArgumentException("Directory doesn't exist: " + directory);
			}
			var files = Directory.EnumerateFiles(directory, pattern, SearchOption.AllDirectories);

			_catalog = new AggregateCatalog();

			foreach (var file in files)
			{
				try
				{
					var asmCat = new AssemblyCatalog(file);

					//Force MEF to load the plugin and figure out if there are any exports
					// good assemblies will not throw an exception and can be added to the catalog
					if (asmCat.Parts.ToList().Count > 0)
					{
						_catalog.Catalogs.Add(asmCat);
					}
				}
				catch (Exception ex)
				{
					LOG.Error(ex, "Error loading {0}", file);
				}
			}
		}

		/// <summary>
		/// Retrieve the parts of catalog
		/// </summary>
		public override IQueryable<ComposablePartDefinition> Parts
		{
			get
			{
				return _catalog.Parts;
			}
		}
	}
}
