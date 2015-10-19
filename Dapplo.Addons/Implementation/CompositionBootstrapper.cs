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
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
using System.IO;
using System.Linq;
using System.Reflection;

namespace Dapplo.Addons.Implementation
{
	/// <summary>
	/// A bootstrapper for making it possible to load Addons to Dapplo applications.
	/// This uses MEF for loading and managing the Addons.
	/// </summary>
	public abstract class CompositionBootstrapper
	{
		protected bool _aggregateCatalogConfigured;
		protected bool _initialized;

		/// <summary>
		/// The AggregateCatalog contains all the catalogs with the assemblies in it.
		/// </summary>
		protected AggregateCatalog AggregateCatalog
		{
			get;
			private set;
		} = new AggregateCatalog();

		/// <summary>
		/// The CompositionContainer
		/// </summary>
		protected CompositionContainer Container
		{
			get;
			private set;
		}

		/// <summary>
		/// List of ExportProviders
		/// </summary>
		protected IList<ExportProvider> ExportProviders
		{
			get;
		} = new List<ExportProvider>();

		/// <summary>
		/// List of all known assemblies
		/// </summary>
		public IList<Assembly> AddonAssemblies
		{
			get;
		} = new List<Assembly>();

		/// <summary>
		/// Get a list of all found files
		/// </summary>
		public IList<string> AddonFiles
		{
			get;
		} = new List<string>();

		/// <summary>
		/// Specify how the composition is made, is used in the Run()
		/// </summary>
		protected CompositionOptions CompositionOptionFlags
		{
			get;
			set;
		} = CompositionOptions.DisableSilentRejection;

		/// <summary>
		/// Override this method to extend what is loaded into the Catalog
		/// </summary>
		protected virtual void ConfigureAggregateCatalog()
		{
			_aggregateCatalogConfigured = true;
        }

		/// <summary>
		/// Export an object, without using Attribute
		/// </summary>
		public void Export<T>(T obj)
		{
			if (!_initialized)
			{
				throw new InvalidOperationException("Bootstrapper is not initialized");
            }
			Container.ComposeExportedValue(obj);
		}

		/// <summary>
		/// Export an object, without using Attribute
		/// </summary>
		public void Export<T>(string contractName, T obj)
		{
			if (!_initialized)
			{
				throw new InvalidOperationException("Bootstrapper is not initialized");
			}
			Container.ComposeExportedValue(contractName, obj);
		}

		/// <summary>
		/// Add an assembly to the AggregateCatalog.Catalogs
		/// In english: make the items in the assembly discoverable
		/// </summary>
		/// <param name="assembly">Assembly to add</param>
		public void Add(Assembly assembly)
		{
			if (assembly == null)
			{
				return;
			}
			var assemblyCatalog = new AssemblyCatalog(assembly);
			Add(assemblyCatalog);
		}

		/// <summary>
		/// Add an AssemblyCatalog AggregateCatalog.Catalogs
		/// But only if the AssemblyCatalog has parts
		/// </summary>
		/// <param name="assemblyCatalog">AssemblyCatalog to add</param>
		public void Add(AssemblyCatalog assemblyCatalog)
		{
			if (AddonAssemblies.Contains(assemblyCatalog.Assembly))
			{
				return;
			}
			if (assemblyCatalog.Parts.ToList().Count > 0)
			{
				AggregateCatalog.Catalogs.Add(assemblyCatalog);
				AddonAssemblies.Add(assemblyCatalog.Assembly);
				AddonFiles.Add(assemblyCatalog.Assembly.Location);
            }
		}

		/// <summary>
		/// Add the assemblies (with parts) found in the specified directory
		/// </summary>
		/// <param name="directory">Directory to scan</param>
		/// <param name="pattern">Pattern to use for the scan, default is "*.dll"</param>
		public void Add(string directory, string pattern = "*.dll")
		{
			if (!Directory.Exists(directory))
			{
				throw new ArgumentException("Directory doesn't exist: " + directory);
			}
			var files = Directory.EnumerateFiles(directory, pattern, SearchOption.AllDirectories);

			foreach (var file in files)
			{
				try
				{
					var assemblyCatalog = new AssemblyCatalog(file);
					Add(assemblyCatalog);
				}
				catch (Exception)
				{
					// Ignore
				}
			}
		}

		/// <summary>
		/// Add the assembly for the specified type
		/// </summary>
		/// <param name="type">The assembly for the type is retrieved add added via the Add(Assembly) method</param>
		public void Add(Type type)
		{
			var typeAssembly = Assembly.GetAssembly(type);
			Add(typeAssembly);
		}

		/// <summary>
		/// Add the ExportProvider to the export providers which are used in the CompositionContainer
		/// </summary>
		/// <param name="exportProvider">ExportProvider</param>
		public void Add(ExportProvider exportProvider)
		{
			ExportProviders.Add(exportProvider);
		}

		/// <summary>
		/// Fill all the imports in the object isntance
		/// </summary>
		/// <param name="importingObject">object to fill the imports for</param>
		public void FillImports(object importingObject)
		{
			if (!_initialized)
			{
				throw new InvalidOperationException("Bootstrapper is not initialized");
			}
			Container.SatisfyImportsOnce(importingObject);
        }

		/// <summary>
		/// Initialize the bootstrapper
		/// </summary>
		public virtual void Initialize()
		{
			_initialized = true;
			ConfigureAggregateCatalog();
			Container = new CompositionContainer(AggregateCatalog, CompositionOptionFlags, ExportProviders.ToArray());
		}

		/// <summary>
		/// Start the bootstrapper, initialize if needed
		/// </summary>
		public virtual void Run()
		{
			if (!_initialized)
			{
				Initialize();
            }
			Container.ComposeParts();
		}
	}
}
