#region Dapplo 2016-2017 - GNU Lesser General Public License

// Dapplo - building blocks for .NET applications
// Copyright (C) 2016-2017 Dapplo
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
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Dapplo.Addons.Bootstrapper.ExportProviders;
using Dapplo.Addons.Bootstrapper.Extensions;
using Dapplo.Addons.Bootstrapper.Internal;
using Dapplo.Addons.Bootstrapper.Resolving;
using Dapplo.Log;
using Microsoft.Practices.ServiceLocation;

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
        private readonly IList<IDisposable> _disposables = new List<IDisposable>();
        private static readonly CosturaHelper Costura = new CosturaHelper();

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
            // Register this bootstrapper with the BootstrapperLocator
            BootstrapperLocator.Register(this);
            AssemblyResolver.RegisterAssemblyResolve();
        }

        
        #region IServiceProvider

        /// <inheritdoc />
        public object GetService(Type serviceType)
        {
            if (!IsInitialized)
            {
                throw new InvalidOperationException(NotInitialized);
            }
            return GetExport(serviceType);
        }

        #endregion

        /// <inheritdoc />
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

        /// <inheritdoc />
        public ISet<string> KnownAssemblies { get; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        /// <inheritdoc />

        public IList<string> KnownFiles { get; } = new List<string>();

        /// <inheritdoc />

        public void AddScanDirectory(string directory)
        {
            if (AllowAssemblyCleanup)
            {
                RemoveEmbeddedAssembliesFromDirectory(directory);
            }
            AssemblyResolver.AddDirectory(directory);
        }

        /// <inheritdoc />
        public void AddScanDirectories(IEnumerable<string> directories)
        {
            foreach (var directory in directories)
            {
                AssemblyResolver.AddDirectory(directory);
            }
        }

        /// <summary>
        /// Helper method to test if we already know an assembly
        /// </summary>
        /// <param name="assembly">Assembly to test for</param>
        /// <returns>true if it's know, false if not</returns>
        private bool HasAssembly(Assembly assembly)
        {
            var name = assembly.GetName().Name;
            var found = KnownAssemblies.Contains(name);
            if (found)
            {
                Log.Verbose().WriteLine("Skipping assembly {0}, as we already know of it.", name);
            }
            return found;
        }

        /// <summary>
        /// Helper method to add a known assembly to the KnownAssemblies
        /// </summary>
        /// <param name="assembly">Assembly</param>
        private void AddKnownAssembly(Assembly assembly)
        {
            KnownAssemblies.Add(assembly.GetName().Name);
        }

        /// <inheritdoc />
        public void Add(Assembly assembly)
        {
            if (assembly == null)
            {
                throw new ArgumentNullException(nameof(assembly));
            }

            if (HasAssembly(assembly))
            {
                return;
            }
            var assemblyCatalog = new AssemblyCatalog(assembly);
            Add(assemblyCatalog);
        }

        /// <inheritdoc />
        public void Add(AssemblyCatalog assemblyCatalog)
        {
            if (assemblyCatalog == null)
            {
                throw new ArgumentNullException(nameof(assemblyCatalog));
            }

            if (HasAssembly(assemblyCatalog.Assembly))
            {
                return;
            }
            try
            {
                var location = assemblyCatalog.Assembly.GetLocation(false);
                Log.Verbose().WriteLine("Adding assembly {0} from {1}", assemblyCatalog.Assembly.FullName, location);
                if (assemblyCatalog.Parts.ToList().Count > 0)
                {
                    AggregateCatalog.Catalogs.Add(assemblyCatalog);
                    // Use the location, but not the CodeBase to see which file was loaded
                    if (!string.IsNullOrEmpty(location))
                    {
                        Log.Verbose().WriteLine("Adding file {0}", location);
                        KnownFiles.Add(location);
                    }
                }
                else
                {
                    Log.Verbose().WriteLine("Assembly {0} from {1} doesn't have any parts exported.", assemblyCatalog.Assembly.FullName, location);
                }
                // Always add the assembly, even if there are no parts, so we can resolve certain "non" parts in ExportProviders.
                AddKnownAssembly(assemblyCatalog.Assembly);
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

        /// <inheritdoc />
        public void FindAndLoadAssemblies(string pattern = "*", bool loadEmbedded = true, IEnumerable<string> extensions = null)
        {
            if (string.IsNullOrEmpty(pattern))
            {
                throw new ArgumentNullException(nameof(pattern));
            }
            var regex = FileTools.FilenameToRegex(pattern, extensions ?? AssemblyResolver.Extensions);
            FindAndLoadAssemblies(AssemblyResolver.Directories, regex, loadEmbedded);
        }

        /// <inheritdoc />
        public void FindAndLoadAssembliesFromDirectory(string directory, string pattern = "*", bool loadEmbedded = true, IEnumerable<string> extensions = null)
        {
            if (string.IsNullOrEmpty(pattern))
            {
                throw new ArgumentNullException(nameof(pattern));
            }
            var regex = FileTools.FilenameToRegex(pattern, extensions ?? AssemblyResolver.Extensions);
            FindAndLoadAssembliesFromDirectory(directory, regex, loadEmbedded);
        }

        /// <inheritdoc />
        public void FindAndLoadAssembliesFromDirectory(string directory, Regex pattern = null, bool loadEmbedded = true)
        {
            if (directory == null)
            {
                throw new ArgumentNullException(nameof(directory));
            }
            Log.Debug().WriteLine("Scanning directory {0}", directory);

            var directoriesToScan = FileLocations.DirectoriesFor(directory);
            FindAndLoadAssemblies(directoriesToScan, pattern, loadEmbedded);
        }

        /// <inheritdoc />
        public void FindAndLoadAssemblies(IEnumerable<string> directories, Regex pattern, bool loadEmbedded = true)
        {
            if (directories == null)
            {
                throw new ArgumentNullException(nameof(directories));
            }
            directories = directories.ToList();

            // Pre-cleanup
            if (AllowAssemblyCleanup && Costura.IsActive)
            {
                foreach (var directory in directories)
                {
                    RemoveEmbeddedAssembliesFromDirectory(directory);
                }
            }

            // check if there is a pattern, use the all assemblies in the directory if none is given
            pattern = pattern ?? FileTools.FilenameToRegex("*", AssemblyResolver.Extensions);

            // Decide on the loading order
            if (AssemblyResolver.ResolveEmbeddedBeforeFiles)
            {
                FindEmbeddedAssemblies(pattern, loadEmbedded);
                FindAssembliesFromFilesystem(directories, pattern);
            }
            else
            {
                FindAssembliesFromFilesystem(directories, pattern);
                FindEmbeddedAssemblies(pattern, loadEmbedded);
            }
        }

        /// <summary>
        /// Helper method triggers the loading of the assemblies on the file system
        /// </summary>
        /// <param name="directories">IEnumerable of string with directories to scan</param>
        /// <param name="pattern">Regex</param>
        private void FindAssembliesFromFilesystem(IEnumerable<string> directories, Regex pattern)
        {
            foreach (var file in FileLocations.Scan(directories, pattern).Select(x => x.Item1))
            {
                try
                {
                    Log.Verbose().WriteLine("Trying to load {0}", file);
                    var assembly = AssemblyResolver.LoadAssemblyFromFile(file);
                    Add(assembly);
                }
                catch
                {
                    Log.Error().WriteLine("Problem loading assembly from {0}", file);
                }
            }
        }

        /// <summary>
        /// A helper method which will delete the assemblies, which are already embedded by costura, from the directory.
        /// This prevents double loading and should make the application stable.
        /// </summary>
        /// <param name="directory">string with the </param>
        private void RemoveEmbeddedAssembliesFromDirectory(string directory)
        {
            if (!Costura.IsActive)
            {
                return;
            }

            foreach (var filePath in FileLocations.Scan(directory, "*.dll"))
            {
                if (!Costura.HasResource(Path.GetFileName(filePath)))
                {
                    continue;
                }
                Log.Debug().WriteLine("Deleting {0} to prevent .NET from automatically loading it and having doubles.", filePath);
                File.Delete(filePath);
            }
        }

        /// <summary>
        /// Helper method which triggers the loading of embedded assemblies
        /// </summary>
        /// <param name="pattern"></param>
        /// <param name="loadEmbedded"></param>
        private void FindEmbeddedAssemblies(Regex pattern, bool loadEmbedded = true)
        {
            if (!loadEmbedded)
            {
                return;
            }
            if (Costura.IsActive)
            {
                foreach (var assembly in Costura.LoadEmbeddedAssemblies(pattern))
                {
                    assembly?.Register();
                    Add(assembly);
                }
            }
            foreach (var resourceTuple in AssemblyResolver.AssemblyCache.FindEmbeddedResources(pattern))
            {
                try
                {
                    var assembly = resourceTuple.Item1.LoadEmbeddedAssembly(resourceTuple.Item2);
                    Add(assembly);
                }
                catch
                {
                    Log.Error().WriteLine("Problem loading assembly from embedded resource {0} in assembly {1}", resourceTuple.Item2, resourceTuple.Item1.GetName().Name);
                }
            }
        }

        /// <inheritdoc />
        public void Add(Type type)
        {
            if (type == null)
            {
                throw new ArgumentNullException(nameof(type));
            }
            var typeAssembly = Assembly.GetAssembly(type);
            Add(typeAssembly);
        }

        /// <inheritdoc />
        public void Add(ExportProvider exportProvider)
        {
            if (IsInitialized)
            {
                throw new InvalidOperationException("Bootstrapper is already initialized, can't add ExportProviders afterwards.");
            }
            if (exportProvider == null)
            {
                throw new ArgumentNullException(nameof(exportProvider));
            }

            Log.Verbose().WriteLine("Adding ExportProvider: {0}", exportProvider.GetType().FullName);
            ExportProviders.Add(exportProvider);
        }

        #endregion

        #region IDependencyProvider
        /// <inheritdoc />
        public void ProvideDependencies(object objectWithDependencies)
        {
            if (!IsInitialized)
            {
                throw new InvalidOperationException(NotInitialized);
            }
            if (objectWithDependencies == null)
            {
                throw new ArgumentNullException(nameof(objectWithDependencies));
            }
            if (Log.IsDebugEnabled())
            {
                Log.Debug().WriteLine("Filling dependencies of {0}", objectWithDependencies.GetType());
            }
            Container.SatisfyImportsOnce(objectWithDependencies);
        }
        #endregion

        #region IMefServiceLocator

        /// <inheritdoc />
        public Lazy<T> GetExport<T>(string contractname = "")
        {
            if (!IsInitialized)
            {
                throw new InvalidOperationException(NotInitialized);
            }
            if (Log.IsVerboseEnabled())
            {
                Log.Verbose().WriteLine("Getting export for {0}", typeof(T));
            }
            return Container.GetExport<T>(contractname);
        }

        /// <inheritdoc />
        public Lazy<T, TMetaData> GetExport<T, TMetaData>(string contractname = "")
        {
            if (!IsInitialized)
            {
                throw new InvalidOperationException(NotInitialized);
            }
            if (Log.IsVerboseEnabled())
            {
                Log.Verbose().WriteLine("Getting export for {0}", typeof(T));
            }
            return Container.GetExport<T, TMetaData>(contractname);
        }


        /// <inheritdoc />
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

        /// <inheritdoc />
        public IEnumerable<Lazy<T>> GetExports<T>(string contractname = "")
        {
            if (!IsInitialized)
            {
                throw new InvalidOperationException(NotInitialized);
            }
            if (Log.IsVerboseEnabled())
            {
                Log.Verbose().WriteLine("Getting exports for {0}", typeof(T));
            }
            return Container.GetExports<T>(contractname);
        }

        /// <inheritdoc />
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

        /// <inheritdoc />
        public IEnumerable<Lazy<T, TMetaData>> GetExports<T, TMetaData>(string contractname = "")
        {
            if (!IsInitialized)
            {
                throw new InvalidOperationException(NotInitialized);
            }
            if (Log.IsVerboseEnabled())
            {
                Log.Verbose().WriteLine("Getting export for {0}", typeof(T));
            }
            return Container.GetExports<T, TMetaData>(contractname);
        }

        #endregion

        #region IServiceLocator

        /// <inheritdoc />
        public object GetInstance(Type serviceType)
        {
            return GetExport(serviceType);
        }

        /// <inheritdoc />
        public object GetInstance(Type serviceType, string key)
        {
            return GetExport(serviceType, key);
        }

        /// <inheritdoc />
        public IEnumerable<object> GetAllInstances(Type serviceType)
        {
            return GetExports(serviceType).Select(lazy => lazy.Value);
        }

        /// <inheritdoc />
        public TService GetInstance<TService>()
        {
            return GetExport<TService>().Value;
        }

        /// <inheritdoc />
        public TService GetInstance<TService>(string key)
        {
            return GetExport<TService>(key).Value;
        }

        /// <inheritdoc />
        public IEnumerable<TService> GetAllInstances<TService>()
        {
            return GetExports<TService>().Select(lazy => lazy.Value);
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
            return Export(contractName, obj, metadata);
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
            return Export(type, contractName, obj, metadata);
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

        /// <inheritdoc />
        public bool AllowAssemblyCleanup { get; set; }


        /// <inheritdoc />
        public bool IsInitialized { get; set; }

        /// <inheritdoc />
        public virtual Task<bool> InitializeAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            Log.Debug().WriteLine("Initializing");
            IsInitialized = true;

            // Configure first, this can be overloaded
            Configure();

            // Get all the export providers
            var exportProviders = ExportProviders.Concat(new List<ExportProvider> {new ServiceProviderExportProvider(this)}).ToArray();
            // Now create the container
            Container = new CompositionContainer(AggregateCatalog, CompositionOptionFlags, exportProviders);
            // Make this bootstrapper as Dapplo.Addons.IServiceLocator
            Export<IServiceLocator>(this);
            // Export this bootstrapper as Dapplo.Addons.IServiceExporter
            Export<IServiceExporter>(this);
            // Export this bootstrapper as Dapplo.Addons.IServiceRepository
            Export<IServiceRepository>(this);
            // Export this bootstrapper as System.IServiceProvider
            Export<IServiceProvider>(this);
            // Export this bootstrapper as IMefServiceLocator
            Export<IMefServiceLocator>(this);
            // Export this bootstrapper as IDependencyProvider
            Export<IDependencyProvider>(this);
            // Export this bootstrapper as IBootstrapper
            Export<IBootstrapper>(this);

            return Task.FromResult(IsInitialized);
        }

        /// <inheritdoc />
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

        /// <inheritdoc />
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
            // Return fast
            if (_disposedValue)
            {
                return;
            }
            // Remove this bootstrapper from the BootstrapperLocator
            BootstrapperLocator.Unregister(this);

            if (disposing && IsInitialized)
            {
                Log.Debug().WriteLine("Disposing...");
                // dispose managed state (managed objects) here.
                using (new NoSynchronizationContextScope())
                {
                    StopAsync().Wait();
                }
                // dispose all registered disposables, in reversed order
                foreach (var disposable in _disposables.Reverse())
                {
                    disposable?.Dispose();
                }
                _disposables.Clear();
            }
            // Dispose unmanaged objects here
            // DO NOT CALL any managed objects here, outside of the disposing = true, as this is also used when a distructor is called

            _disposedValue = true;
        }

        /// <summary>
        ///     Implement IDisposable
        /// </summary>
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
        }

        /// <inheritdoc />
        public void RegisterForDisposal(IDisposable disposable)
        {
            _disposables.Add(disposable);
        }

        #endregion
    }
}