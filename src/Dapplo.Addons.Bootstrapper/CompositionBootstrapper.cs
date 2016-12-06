#region Dapplo 2016 - GNU Lesser General Public License

// Dapplo - building blocks for .NET applications
// Copyright (C) 2016 Dapplo
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
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
using System.ComponentModel.Composition.Primitives;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Dapplo.Log;
using Dapplo.Utils;
using Dapplo.Utils.Embedded;
using Dapplo.Utils.Resolving;

#endregion

namespace Dapplo.Addons.Bootstrapper
{
	/// <summary>
	///     A bootstrapper for making it possible to load Addons to Dapplo applications.
	///     This uses MEF for loading and managing the Addons.
	/// </summary>
	public class CompositionBootstrapper : IBootstrapper
	{
		private const string NotInitialized = "Bootstrapper is not initialized";
		private static readonly LogSource Log = new LogSource();

		/// <summary>
		///     The AggregateCatalog contains all the catalogs with the assemblies in it.
		/// </summary>
		protected AggregateCatalog AggregateCatalog { get; set; } = new AggregateCatalog();

		/// <summary>
		///     Specify how the composition is made, is used in the Run()
		/// </summary>
		protected CompositionOptions CompositionOptionFlags { get; set; } = CompositionOptions.DisableSilentRejection | CompositionOptions.ExportCompositionService | CompositionOptions.IsThreadSafe;

		/// <summary>
		///     The CompositionContainer
		/// </summary>
		protected CompositionContainer Container { get; set; }

		/// <summary>
		///     List of ExportProviders
		/// </summary>
		public IList<ExportProvider> ExportProviders { get; } = new List<ExportProvider>();

		/// <summary>
		///     Specify if the Aggregate Catalog is configured
		/// </summary>
		protected bool IsAggregateCatalogConfigured { get; set; }

		/// <summary>
		/// Make sure the assembly resolver is active as soon as the Bootstrapper is initialized.
		///Tthis makes sure assemblies which are embedded or in a subdirectory can be found.
		/// </summary>
		public CompositionBootstrapper()
		{
			AssemblyResolver.RegisterAssemblyResolve();
		}

		#region IServiceProvider

		/// <summary>
		///     Implement IServiceProdiver
		/// </summary>
		/// <param name="serviceType">Type</param>
		/// <returns>Instance of the serviceType</returns>
		public object GetService(Type serviceType)
		{
			if (!IsInitialized)
			{
				throw new InvalidOperationException(NotInitialized);
			}
			return GetExport(serviceType);
		}

		#endregion

		/// <summary>
		///     Configure the AggregateCatalog, by adding the default assemblies
		/// </summary>
		protected virtual void Configure()
		{
			if (IsAggregateCatalogConfigured)
			{
				return;
			}

			// Add the entry assembly, which should be the application, but not the calling or executing (as this is Dapplo.Addons)
			var applicationAssembly = Assembly.GetEntryAssembly();
			if (applicationAssembly != null && applicationAssembly != GetType().Assembly)
			{
				Add(applicationAssembly);
			}
		}

		/// <summary>
		///     Unconfigure the AggregateCatalog
		/// </summary>
		protected virtual void Unconfigure()
		{
			if (!IsAggregateCatalogConfigured)
			{
				return;
			}
			IsAggregateCatalogConfigured = false;

			// Remove all references to the assemblies
			KnownAssemblies.Clear();

			AssemblyResolver.UnregisterAssemblyResolve();
		}

		#region IServiceRepository

		/// <summary>
		///     List of all known assemblies.
		///     This might be needed in e.g. a Caliburn Micro bootstrapper, so it can locate a view for a view model.
		/// </summary>
		public ISet<string> KnownAssemblies { get; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

		/// <summary>
		///     Get a list of all known file locations.
		///     This is internally needed to resolved dependencies.
		/// </summary>
		public IList<string> KnownFiles { get; } = new List<string>();

		/// <summary>
		///     Add the specified directory to have the bootstrapper look for assemblies
		/// </summary>
		/// <param name="directory">string with the directory</param>
		public void AddScanDirectory(string directory)
		{
			AssemblyResolver.AddDirectory(directory);
		}

		/// <summary>
		///     Add an assembly to the AggregateCatalog.Catalogs
		///     In english: make the items in the assembly discoverable
		/// </summary>
		/// <param name="assembly">Assembly to add</param>
		public void Add(Assembly assembly)
		{
			if (assembly == null)
			{
				throw new ArgumentNullException(nameof(assembly));
			}

			if (KnownAssemblies.Contains(assembly.FullName))
			{
				Log.Verbose().WriteLine("Skipping assembly {0}, we already added it", assembly.FullName);
				return;
			}
			var assemblyCatalog = new AssemblyCatalog(assembly);
			Add(assemblyCatalog);
		}

		/// <summary>
		///     Add an AssemblyCatalog AggregateCatalog.Catalogs
		///     But only if the AssemblyCatalog has parts
		/// </summary>
		/// <param name="assemblyCatalog">AssemblyCatalog to add</param>
		public void Add(AssemblyCatalog assemblyCatalog)
		{
			if (assemblyCatalog == null)
			{
				throw new ArgumentNullException(nameof(assemblyCatalog));
			}
			if (KnownAssemblies.Contains(assemblyCatalog.Assembly.FullName))
			{
				Log.Verbose().WriteLine("Skipping assembly {0}, we already added it", assemblyCatalog.Assembly.FullName);
				return;
			}
			try
			{
				Log.Verbose().WriteLine("Adding assembly {0}", assemblyCatalog.Assembly.FullName);
				if (assemblyCatalog.Parts.ToList().Count > 0)
				{
					AggregateCatalog.Catalogs.Add(assemblyCatalog);
					if (!string.IsNullOrEmpty(assemblyCatalog.Assembly.Location))
					{
						Log.Verbose().WriteLine("Adding file {0}", assemblyCatalog.Assembly.Location);
						KnownFiles.Add(assemblyCatalog.Assembly.Location);
					}
				}
				// Always add the assembly, even if there are no parts, so we can resolve certain "non" parts in ExportProviders.
				KnownAssemblies.Add(assemblyCatalog.Assembly.FullName);
			}
			catch (ReflectionTypeLoadException rtlEx)
			{
				Log.Error().WriteLine(rtlEx, "Couldn't add the supplied assembly. Details follow:");
				foreach (var loaderException in rtlEx.LoaderExceptions)
				{
					Log.Error().WriteLine(loaderException, loaderException.Message);
				}
				throw;
			}
			catch (Exception ex)
			{
				Log.Error().WriteLine(ex, "Couldn't add the supplied assembly catalog.");
				throw;
			}
		}

		/// <summary>
		///     Find the the assembly with the specified name, and load it.
		///     The assembly will be searched in the directories added by AddScanDirectory, and also in the embedded resources.
		/// </summary>
		/// <param name="assemblyName">string with the assembly name</param>
		/// <param name="extensions">IEnumerable with extensions to look for, defaults will be set if null was passed</param>
		public void FindAndLoadAssembly(string assemblyName, IEnumerable<string> extensions = null)
		{
			if (string.IsNullOrEmpty(assemblyName))
			{
				throw new ArgumentNullException(nameof(assemblyName));
			}
			try
			{
				Log.Verbose().WriteLine("Trying to load {0}", assemblyName);
				var assembly = AssemblyResolver.FindAssembly(assemblyName, extensions);
				if (assembly != null)
				{
					Add(assembly);
				}
				else
				{
					Log.Warn().WriteLine("Couldn't load {0}", assemblyName);
				}
			}
			catch
			{
				// Ignore the exception, so we can continue, and don't log as this is handled in Add(assembly);
				Log.Error().WriteLine("Problem loading assembly {0}", assemblyName);
			}
		}

		/// <summary>
		///     Find the assemblies (with parts) found in default directories, or manifest resources, matching the specified
		///     filepattern.
		/// </summary>
		/// <param name="pattern">File-Pattern to use for the scan, default all dlls will be found</param>
		/// <param name="loadEmbedded">bool specifying if embedded resources should be used. default is true</param>
		/// <param name="extensions">IEnumerable with extensions to look for, defaults will be set if null was passed</param>
		public void FindAndLoadAssemblies(string pattern = "*", bool loadEmbedded = true, IEnumerable<string> extensions = null)
		{
			if (string.IsNullOrEmpty(pattern))
			{
				throw new ArgumentNullException(nameof(pattern));
			}
			var regex = FileTools.FilenameToRegex(pattern, extensions ?? AssemblyResolver.Extensions);
			FindAndLoadAssemblies(AssemblyResolver.Directories, regex, loadEmbedded);
		}

		/// <summary>
		///     Find the assemblies (with parts) found in the specified directory, or manifest resources, matching the specified
		///     filepattern.
		/// </summary>
		/// <param name="directory">Directory to scan</param>
		/// <param name="pattern">File-Pattern to use for the scan, default all dlls will be found</param>
		/// <param name="loadEmbedded">bool specifying if embedded resources should be used. default is true</param>
		/// <param name="extensions">IEnumerable with extensions to look for, defaults will be set if null was passed</param>
		public void FindAndLoadAssembliesFromDirectory(string directory, string pattern = "*", bool loadEmbedded = true, IEnumerable<string> extensions = null)
		{
			if (string.IsNullOrEmpty(pattern))
			{
				throw new ArgumentNullException(nameof(pattern));
			}
			var regex = FileTools.FilenameToRegex(pattern, extensions ?? AssemblyResolver.Extensions);
			FindAndLoadAssembliesFromDirectory(directory, regex, loadEmbedded);
		}

		/// <summary>
		///     Find the assemblies (with parts) found in the specified directory, or manifest resources, matching the specified
		///     regex.
		/// </summary>
		/// <param name="directory">Directory to scan</param>
		/// <param name="pattern">Regex to use for the scan, when null all dlls will be found</param>
		/// <param name="loadEmbedded">bool specifying if embedded resources should be used. default is true</param>
		public void FindAndLoadAssembliesFromDirectory(string directory, Regex pattern, bool loadEmbedded = true)
		{
			if (directory == null)
			{
				throw new ArgumentNullException(nameof(directory));
			}
			Log.Debug().WriteLine("Scanning directory {0}", directory);

			var directoriesToScan = FileLocations.DirectoriesFor(directory);
			FindAndLoadAssemblies(directoriesToScan, pattern, loadEmbedded);
		}

		/// <summary>
		///     Find the assemblies (with parts) found in the specified directories, or manifest resources, matching the specified
		///     regex.
		/// </summary>
		/// <param name="directories">Directory to scan</param>
		/// <param name="pattern">Regex to use for the scan, when null all files ending on .dll (and .dll.gz) will be loaded</param>
		/// <param name="loadEmbedded">bool specifying if embedded resources should be used. default is true</param>
		public void FindAndLoadAssemblies(IEnumerable<string> directories, Regex pattern, bool loadEmbedded = true)
		{
			if (directories == null)
			{
				throw new ArgumentNullException(nameof(directories));
			}
			// check if there is a pattern, use the all assemblies in the directory if none is given
			pattern = pattern ?? FileTools.FilenameToRegex("*", AssemblyResolver.Extensions);

			foreach (var file in FileLocations.Scan(directories.ToList(), pattern).Select(x => x.Item1))
			{
				try
				{
					Log.Verbose().WriteLine("Trying to load {0}", file);
					var assembly = AssemblyResolver.LoadAssemblyFromFile(file);
					Add(assembly);
				}
				catch
				{
					// Ignore the exception, so we can continue, and don't log as this is handled in Add(assemblyCatalog);
					Log.Error().WriteLine("Problem loading assembly from {0}", file);
				}
			}
			if (loadEmbedded)
			{
				foreach (var resourceTuple in AssemblyResolver.AssemblyCache.FindEmbeddedResources(pattern))
				{
					try
					{
						var assembly = resourceTuple.Item1.LoadEmbeddedAssembly(resourceTuple.Item2);
						Add(assembly);
					}
					catch
					{
						// Ignore the exception, so we can continue, and don't log as this is handled in Add(assemblyCatalog);
						Log.Error().WriteLine("Problem loading assembly from embedded resource {0} in assembly {1}", resourceTuple.Item2, resourceTuple.Item1.GetName().Name);
					}
				}
			}
		}

		/// <summary>
		///     Add the assembly for the specified type
		/// </summary>
		/// <param name="type">The assembly for the type is retrieved add added via the Add(Assembly) method</param>
		public void Add(Type type)
		{
			if (type == null)
			{
				throw new ArgumentNullException(nameof(type));
			}
			var typeAssembly = Assembly.GetAssembly(type);
			Add(typeAssembly);
		}

		/// <summary>
		///     Add the ExportProvider to the export providers which are used in the CompositionContainer
		/// </summary>
		/// <param name="exportProvider">ExportProvider</param>
		public void Add(ExportProvider exportProvider)
		{
			if (exportProvider == null)
			{
				throw new ArgumentNullException(nameof(exportProvider));
			}
			Log.Verbose().WriteLine("Adding ExportProvider: {0}", exportProvider.GetType().FullName);
			ExportProviders.Add(exportProvider);
		}

		#endregion

		#region IServiceLocator

		/// <summary>
		///     Fill all the imports in the object isntance
		/// </summary>
		/// <param name="importingObject">object to fill the imports for</param>
		public void FillImports(object importingObject)
		{
			if (!IsInitialized)
			{
				throw new InvalidOperationException(NotInitialized);
			}
			if (importingObject == null)
			{
				throw new ArgumentNullException(nameof(importingObject));
			}
			if (Log.IsDebugEnabled())
			{
				Log.Debug().WriteLine("Filling imports of {0}", importingObject.GetType());
			}
			Container.SatisfyImportsOnce(importingObject);
		}

		/// <summary>
		///     Simple "service-locater"
		/// </summary>
		/// <typeparam name="T">Type to locate</typeparam>
		/// <returns>Lazy T</returns>
		public Lazy<T> GetExport<T>()
		{
			if (!IsInitialized)
			{
				throw new InvalidOperationException(NotInitialized);
			}
			if (Log.IsVerboseEnabled())
			{
				Log.Verbose().WriteLine("Getting export for {0}", typeof(T));
			}
			return Container.GetExport<T>();
		}

		/// <summary>
		///     Simple "service-locater" with meta-data
		/// </summary>
		/// <typeparam name="T">Type to locate</typeparam>
		/// <typeparam name="TMetaData">Type for the meta-data</typeparam>
		/// <returns>Lazy T,TMetaData</returns>
		public Lazy<T, TMetaData> GetExport<T, TMetaData>()
		{
			if (!IsInitialized)
			{
				throw new InvalidOperationException(NotInitialized);
			}
			if (Log.IsVerboseEnabled())
			{
				Log.Verbose().WriteLine("Getting export for {0}", typeof(T));
			}
			return Container.GetExport<T, TMetaData>();
		}

		/// <summary>
		///     Simple "service-locater"
		/// </summary>
		/// <param name="type">Type to locate</param>
		/// <param name="contractname">Name of the contract, null or an empty string</param>
		/// <returns>object for type</returns>
		public object GetExport(Type type, string contractname = "")
		{
			if (!IsInitialized)
			{
				throw new InvalidOperationException(NotInitialized);
			}
			if (Log.IsVerboseEnabled())
			{
				Log.Verbose().WriteLine("Getting export for {0}", type);
			}
			var lazyResult = Container.GetExports(type, null, contractname).FirstOrDefault();
			return lazyResult?.Value;
		}

		/// <summary>
		///     Simple "service-locater" to get multiple exports
		/// </summary>
		/// <typeparam name="T">Type to locate</typeparam>
		/// <returns>IEnumerable of Lazy T</returns>
		public IEnumerable<Lazy<T>> GetExports<T>()
		{
			if (!IsInitialized)
			{
				throw new InvalidOperationException(NotInitialized);
			}
			if (Log.IsVerboseEnabled())
			{
				Log.Verbose().WriteLine("Getting exports for {0}", typeof(T));
			}
			return Container.GetExports<T>();
		}

		/// <summary>
		///     Simple "service-locater" to get multiple exports
		/// </summary>
		/// <param name="type">Type to locate</param>
		/// <param name="contractname">Name of the contract, null or an empty string</param>
		/// <returns>IEnumerable of Lazy object</returns>
		public IEnumerable<Lazy<object>> GetExports(Type type, string contractname = "")
		{
			if (!IsInitialized)
			{
				throw new InvalidOperationException(NotInitialized);
			}
			if (Log.IsVerboseEnabled())
			{
				Log.Verbose().WriteLine("Getting exports for {0}", type);
			}
			return Container.GetExports(type, null, contractname);
		}

		/// <summary>
		///     Simple "service-locater" to get multiple exports with meta-data
		/// </summary>
		/// <typeparam name="T">Type to locate</typeparam>
		/// <typeparam name="TMetaData">Type for the meta-data</typeparam>
		/// <returns>IEnumerable of Lazy T,TMetaData</returns>
		public IEnumerable<Lazy<T, TMetaData>> GetExports<T, TMetaData>()
		{
			if (!IsInitialized)
			{
				throw new InvalidOperationException(NotInitialized);
			}
			if (Log.IsVerboseEnabled())
			{
				Log.Verbose().WriteLine("Getting export for {0}", typeof(T));
			}
			return Container.GetExports<T, TMetaData>();
		}

		#endregion

		#region IServiceExporter

		/// <summary>
		///     Export an object
		/// </summary>
		/// <typeparam name="T">Type to export</typeparam>
		/// <param name="obj">object to add</param>
		/// <param name="metadata">Metadata for the export</param>
		/// <returns>ComposablePart, this can be used to remove the export later</returns>
		public ComposablePart Export<T>(T obj, IDictionary<string, object> metadata = null)
		{
			var contractName = AttributedModelServices.GetContractName(typeof(T));
			return Export(contractName, obj);
		}

		/// <summary>
		///     Export an object
		/// </summary>
		/// <param name="type">Type to export</param>
		/// <param name="obj">object to add</param>
		/// <param name="metadata">Metadata for the export</param>
		/// <returns>ComposablePart, this can be used to remove the export later</returns>
		public ComposablePart Export(Type type, object obj, IDictionary<string, object> metadata = null)
		{
			var contractName = AttributedModelServices.GetContractName(type);
			return Export(contractName, obj);
		}

		/// <summary>
		///     Export an object
		/// </summary>
		/// <typeparam name="T">Type to export</typeparam>
		/// <param name="contractName">contractName under which the object of Type T is registered</param>
		/// <param name="obj">object to add</param>
		/// <param name="metadata">Metadata for the export</param>
		/// <returns>ComposablePart, this can be used to remove the export later</returns>
		public ComposablePart Export<T>(string contractName, T obj, IDictionary<string, object> metadata = null)
		{
			return Export(typeof(T), contractName, obj, metadata);
		}

		/// <summary>
		///     Export an object
		/// </summary>
		/// <param name="type">Type to export</param>
		/// <param name="contractName">contractName under which the object of Type T is registered</param>
		/// <param name="obj">object to add</param>
		/// <param name="metadata">Metadata for the export</param>
		/// <returns>ComposablePart, this can be used to remove the export later</returns>
		public ComposablePart Export(Type type, string contractName, object obj, IDictionary<string, object> metadata = null)
		{
			if (!IsInitialized)
			{
				throw new InvalidOperationException(NotInitialized);
			}

			if (obj == null)
			{
				throw new ArgumentNullException(nameof(obj));
			}

			if (Log.IsDebugEnabled())
			{
				Log.Debug().WriteLine("Exporting {0}", contractName);
			}

			var typeIdentity = AttributedModelServices.GetTypeIdentity(type);
			if (metadata == null)
			{
				metadata = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
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
		///     Release an export which was previously added with the Export method
		/// </summary>
		/// <param name="part">ComposablePart from Export call</param>
		public void Release(ComposablePart part)
		{
			if (!IsInitialized)
			{
				throw new InvalidOperationException(NotInitialized);
			}
			if (part == null)
			{
				throw new ArgumentNullException(nameof(part));
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

		#endregion

		#region IBootstrapper

		/// <summary>
		///     Is this initialized?
		/// </summary>
		public bool IsInitialized { get; set; }

		/// <summary>
		///     Initialize the bootstrapper
		/// </summary>
		public virtual Task<bool> InitializeAsync(CancellationToken cancellationToken = default(CancellationToken))
		{
			Log.Debug().WriteLine("Initializing");
			IsInitialized = true;

			// Configure first, this can be overloaded
			Configure();

			// Now create the container
			Container = new CompositionContainer(AggregateCatalog, CompositionOptionFlags, ExportProviders.ToArray());
			// Make this bootstrapper as Dapplo.Addons.IServiceLocator
			Export<IServiceLocator>(this);
			// Export this bootstrapper as Dapplo.Addons.IServiceExporter
			Export<IServiceExporter>(this);
			// Export this bootstrapper as Dapplo.Addons.IServiceRepository
			Export<IServiceRepository>(this);
			// Export this bootstrapper as System.IServiceProvider
			Export<IServiceProvider>(this);

			return Task.FromResult(IsInitialized);
		}

		/// <summary>
		///     Start the bootstrapper, initialize if needed
		/// </summary>
		public virtual async Task<bool> RunAsync(CancellationToken cancellationToken = default(CancellationToken))
		{
			Log.Debug().WriteLine("Starting");
			if (!IsInitialized)
			{
				await InitializeAsync(cancellationToken).ConfigureAwait(false);
			}
			if (!IsInitialized)
			{
				throw new NotSupportedException("Can't run if IsInitialized is false!");
			}
			Container.ComposeParts();
			return IsInitialized;
		}

		/// <summary>
		///     Stop the bootstrapper
		/// </summary>
		public virtual Task<bool> StopAsync(CancellationToken cancellationToken = default(CancellationToken))
		{
			if (IsInitialized)
			{
				// Unconfigure can be overloaded
				Unconfigure();
				// Now dispose the container
				Container?.Dispose();
				Container = null;
				Log.Debug().WriteLine("Stopped");
				IsInitialized = false;
			}
			return Task.FromResult(!IsInitialized);
		}

		#endregion

		#region IDisposable Support

		// To detect redundant calls
		private bool _disposedValue;

		/// <summary>
		///     Implementation of the dispose pattern
		/// </summary>
		/// <param name="disposing">bool</param>
		protected virtual void Dispose(bool disposing)
		{
			if (!_disposedValue)
			{
				if (disposing && IsInitialized)
				{
					Log.Debug().WriteLine("Disposing...");
					// dispose managed state (managed objects) here.
					using (new NoSynchronizationContextScope())
					{
						StopAsync().Wait();
					}
				}
				// Dispose unmanaged objects here
				// DO NOT CALL any managed objects here, outside of the disposing = true, as this is also used when a distructor is called

				_disposedValue = true;
			}
		}

		/// <summary>
		///     Implement IDisposable
		/// </summary>
		public void Dispose()
		{
			// Do not change this code. Put cleanup code in Dispose(bool disposing) above.
			Dispose(true);
		}

		#endregion
	}
}