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
using System.IO;
using System.Linq;
using System.Reflection;
using Dapplo.LogFacade;
using System.Threading.Tasks;
using System.Threading;

namespace Dapplo.Addons.Implementation
{
	/// <summary>
	/// A bootstrapper for making it possible to load Addons to Dapplo applications.
	/// This uses MEF for loading and managing the Addons.
	/// </summary>
	public abstract class CompositionBootstrapper : IBootstrapper
	{
		private static readonly LogSource Log = new LogSource();
		protected bool IsAggregateCatalogConfigured;
		protected bool IsInitialized;
		private TaskScheduler _taskScheduler;

		/// <summary>
		/// The AggregateCatalog contains all the catalogs with the assemblies in it.
		/// </summary>
		protected AggregateCatalog AggregateCatalog
		{
			get;
			set;
		} = new AggregateCatalog();

		/// <summary>
		/// The CompositionContainer
		/// </summary>
		protected CompositionContainer Container
		{
			get;
			set;
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
		} = CompositionOptions.DisableSilentRejection | CompositionOptions.ExportCompositionService;

		/// <summary>
		/// Override this method to extend what is loaded into the Catalog
		/// </summary>
		protected virtual void ConfigureAggregateCatalog()
		{
			Log.Verbose().WriteLine("Configuring");
			IsAggregateCatalogConfigured = true;
        }

		/// <summary>
		/// Export an object
		/// </summary>
		/// <typeparam name="T">Type to export</typeparam>
		/// <param name="obj">object to add</param>
		/// <param name="metadata">Metadata for the export</param>
		/// <returns>ComposablePart, this can be used to remove the export later</returns>
		public ComposablePart Export<T>(T obj, IDictionary<string, object> metadata = null)
		{
			if (!IsInitialized)
			{
				throw new InvalidOperationException("Bootstrapper is not initialized");
            }
			var contractName = AttributedModelServices.GetContractName(typeof(T));
			return Export(contractName, obj);
		}

		/// <summary>
		/// Export an object
		/// </summary>
		/// <typeparam name="T">Type to export</typeparam>
		/// <param name="contractName">contractName under which the object of Type T is registered</param>
		/// <param name="obj">object to add</param>
		/// <param name="metadata">Metadata for the export</param>
		/// <returns>ComposablePart, this can be used to remove the export later</returns>
		public ComposablePart Export<T>(string contractName, T obj, IDictionary<string, object> metadata = null)
		{
			if (!IsInitialized)
			{
				throw new InvalidOperationException("Bootstrapper is not initialized");
			}

			if (obj == null)
			{
				throw new ArgumentNullException(nameof(obj));
			}

			if (Log.IsDebugEnabled())
			{
				Log.Debug().WriteLine("Exporting {0}", contractName);
			}

			string typeIdentity = AttributedModelServices.GetTypeIdentity(typeof(T));
			if (metadata == null)
			{
				metadata = new Dictionary<string, object>();
			}
			if (!metadata.ContainsKey(CompositionConstants.ExportTypeIdentityMetadataName))
			{
				metadata.Add(CompositionConstants.ExportTypeIdentityMetadataName, typeIdentity);
			}

			// TODO: Maybe this could be simplified, but this was currently the only way to get the meta-data from all attributes
			var partDefinition = AttributedModelServices.CreatePartDefinition(obj.GetType(), null);
			if (partDefinition != null && partDefinition.ExportDefinitions.Any())
			{
				var partMetadata = partDefinition.ExportDefinitions.First().Metadata;
                foreach (var key in partMetadata.Keys)
				{
					if (!metadata.ContainsKey(key))
					{
						metadata.Add(key, partMetadata[key]);
					}
				}
			}
			else
			{
				// If there wasn't an export, the ExportMetadataAttribute is not checked... so we do it ourselves
				var exportMetadataAttributes = obj.GetType().GetCustomAttributes<ExportMetadataAttribute>(true);
				if (exportMetadataAttributes != null)
				{
					foreach (var exportMetadataAttribute in exportMetadataAttributes)
					{
						if (!metadata.ContainsKey(exportMetadataAttribute.Name))
						{
							metadata.Add(exportMetadataAttribute.Name, exportMetadataAttribute.Value);
						}
					}
				}

			}

			// We probaby could use the export-definition from the partDefinition directly...
			var export = new Export(contractName, metadata, () => obj);
			var batch = new CompositionBatch();
			var part = batch.AddExport(export);
			Container.Compose(batch);
			return part;
		}

		/// <summary>
		/// Release an export which was previously added with the Export method
		/// </summary>
		/// <param name="part">ComposablePart from Export call</param>
		public void Release(ComposablePart part)
		{
			if (!IsInitialized)
			{
				throw new InvalidOperationException("Bootstrapper is not initialized");
			}

			if (Log.IsDebugEnabled())
			{
				var contracts = part.ExportDefinitions.Select(x => x.ContractName);
				Log.Debug().WriteLine("Releasing {0}", string.Join(",", contracts));
			}

			var batch = new CompositionBatch();
			batch.RemovePart(part);
			Container.Compose(batch);
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
				Log.Debug().WriteLine("Adding file {0}", assemblyCatalog.Assembly.Location);
				AddonFiles.Add(assemblyCatalog.Assembly.Location);
			}
			Log.Debug().WriteLine("Adding assembly {0}", assemblyCatalog.Assembly.FullName);
			// Always add the assembly, even if there are no parts, so we can resolve certain "non" parts in ExportProviders.
			AddonAssemblies.Add(assemblyCatalog.Assembly);
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
			Log.Debug().WriteLine("Scanning directory {0} with pattern {1}", directory, pattern);
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
			Log.Verbose().WriteLine("Adding ExportProvider");
			ExportProviders.Add(exportProvider);
		}

		/// <summary>
		/// Fill all the imports in the object isntance
		/// </summary>
		/// <param name="importingObject">object to fill the imports for</param>
		public void FillImports(object importingObject)
		{
			if (!IsInitialized)
			{
				throw new InvalidOperationException("Bootstrapper is not initialized");
			}
			if (Log.IsDebugEnabled())
			{
				Log.Debug().WriteLine("Filling imports of {0}", importingObject.GetType());
			}
			Container.SatisfyImportsOnce(importingObject);
        }

		/// <summary>
		/// Simple "service-locater"
		/// </summary>
		/// <typeparam name="T">Type to locate</typeparam>
		/// <returns>Lazy T</returns>
		public Lazy<T> GetExport<T>()
		{
			if (Log.IsVerboseEnabled())
			{
				Log.Verbose().WriteLine("Getting export for {0}", typeof(T));
			}
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
			if (Log.IsVerboseEnabled())
			{
				Log.Verbose().WriteLine("Getting export for {0}", typeof(T));
			}
			return Container.GetExport<T, TMetaData>();
		}

		/// <summary>
		/// Simple "service-locater" to get multiple exports
		/// </summary>
		/// <typeparam name="T">Type to locate</typeparam>
		/// <returns>IEnumerable of Lazy T</returns>
		public IEnumerable<Lazy<T>> GetExports<T>()
		{
			if (Log.IsVerboseEnabled())
			{
				Log.Verbose().WriteLine("Getting exports for {0}", typeof(T));
			}
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
			if (Log.IsVerboseEnabled())
			{
				Log.Verbose().WriteLine("Getting export for {0}", typeof(T));
			}
			return Container.GetExports<T, TMetaData>();
		}

		/// <summary>
		/// Initialize the bootstrapper
		/// </summary>
		public virtual Task<bool> InitializeAsync(CancellationToken cancellationToken = default(CancellationToken))
		{
			Log.Debug().WriteLine("Initializing");
			IsInitialized = true;
			ConfigureAggregateCatalog();
			Container = new CompositionContainer(AggregateCatalog, CompositionOptionFlags, ExportProviders.ToArray());
			// Make sure we export ourselves as the IServiceLocator
			Export<IServiceLocator>(this);
			return Task.FromResult(IsInitialized);
		}

		/// <summary>
		/// Start the bootstrapper, initialize if needed
		/// </summary>
		public virtual async Task<bool> RunAsync(CancellationToken cancellationToken = default(CancellationToken))
		{
			Log.Debug().WriteLine("Starting");
			if (!IsInitialized)
			{
				await InitializeAsync(cancellationToken);
            }
			if (!IsInitialized)
			{
				throw new NotSupportedException("Can't run if IsInitialized is false!");
			}
			Container.ComposeParts();
			return IsInitialized;
		}

		/// <summary>
		/// Stop the bootstrapper
		/// </summary>
		public virtual Task<bool> StopAsync(CancellationToken cancellationToken = default(CancellationToken))
		{
			if (IsInitialized)
			{
				Log.Debug().WriteLine("Stopped");
				IsInitialized = false;
			}
			return Task.FromResult(!IsInitialized);
		}

		#region IDisposable Support
		// To detect redundant calls
		private bool _disposedValue = false;

		protected virtual void Dispose(bool disposing)
		{
			if (!_disposedValue)
			{
				if (disposing)
				{
					Log.Debug().WriteLine("Disposing...");
					// dispose managed state (managed objects) here.
					StopAsync().Wait();
				}
				// Dispose unmanaged objects here
				// DO NOT CALL any managed objects here, outside of the disposing = true, as this is also used when a distructor is called

				_disposedValue = true;
			}
		}

		/// <summary>
		/// Implement IDisposable
		/// </summary>
		public void Dispose()
		{
			// Do not change this code. Put cleanup code in Dispose(bool disposing) above.
			Dispose(true);
		}
		#endregion
	}
}
