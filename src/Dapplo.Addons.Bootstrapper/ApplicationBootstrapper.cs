#region Dapplo 2016-2018 - GNU Lesser General Public License

// Dapplo - building blocks for .NET applications
// Copyright (C) 2016-2018 Dapplo
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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Autofac;
using Autofac.Extras.AttributeMetadata;
using Dapplo.Addons.Bootstrapper.Handler;
using Dapplo.Addons.Bootstrapper.Internal;
using Dapplo.Addons.Bootstrapper.Resolving;
using Dapplo.Log;

namespace Dapplo.Addons.Bootstrapper
{
    /// <summary>
    /// This is a bootstrapper for Autofac
    /// </summary>
    public class ApplicationBootstrapper : IDisposable
    {
        private static readonly LogSource Log = new LogSource();
        private readonly ResourceMutex _resourceMutex;
        private readonly IList<IDisposable> _disposables = new List<IDisposable>();
        private bool _isStartedUp;
        private bool _isShutDown;
        private ContainerBuilder _builder;

        /// <summary>
        /// The current instance
        /// </summary>
        public static ApplicationBootstrapper Instance { get; private set; }

        /// <summary>
        /// The used assembly resolver
        /// </summary>
        public AssemblyResolver Resolver { get; }

        /// <summary>
        /// Provides access to the builder, as long as it can be modified.
        /// </summary>
        public ContainerBuilder Builder => Container == null ?_builder : null;

        /// <summary>
        /// Provides the Autofac container
        /// </summary>
        public IContainer Container { get; private set; }

        /// <summary>
        /// Signals when the container is created
        /// </summary>
        public Action<IContainer> OnContainerCreated { get; set; }

        /// <summary>
        /// Provides the Autofac primary lifetime scope
        /// </summary>
        public ILifetimeScope Scope { get; private set; }

        /// <summary>
        /// The name of the application
        /// </summary>
        public string ApplicationName { get; }

        /// <summary>
        /// An IEnumerable with the loaded assemblies, but filtered to the ones not from the .NET Framework (where possible) 
        /// </summary>
        public IEnumerable<Assembly> LoadedAssemblies => Resolver.LoadedAssemblies
            .Where(pair => !Resolver.AssembliesToIgnore.IsMatch(pair.Key) && !pair.Value.IsDynamic)
            .Select(pair => pair.Value);

        /// <summary>
        /// Create the application bootstrapper
        /// </summary>
        /// <param name="applicationName">string with the name of the application</param>
        /// <param name="mutexId">optional mutex id</param>
        /// <param name="global">is the mutex globally?</param>
        public ApplicationBootstrapper(string applicationName, string mutexId = null, bool global = false)
        {
            Instance = this;
            ApplicationName = applicationName ?? throw new ArgumentNullException(nameof(applicationName));
            if (Thread.CurrentThread.Name == null)
            {
                Thread.CurrentThread.Name = applicationName;
            }
            if (mutexId != null)
            {
                _resourceMutex = ResourceMutex.Create(mutexId, applicationName, global);
            }

            Resolver = new AssemblyResolver(applicationName);
        }

        /// <summary>
        ///     Returns if the Mutex is locked, in other words if the Bootstrapper can continue
        ///     This also returns true if no mutex is used
        /// </summary>
        public bool IsAlreadyRunning => _resourceMutex != null && !_resourceMutex.IsLocked;

        /// <summary>
        /// Add the disposable to a list, everything in there is disposed when the bootstrapper is disposed.
        /// </summary>
        /// <param name="disposable">IDisposable</param>
        public ApplicationBootstrapper RegisterForDisposal(IDisposable disposable)
        {
            if (disposable == null)
            {
                throw new ArgumentNullException(nameof(disposable));
            }
            _disposables.Add(disposable);
            return this;
        }

        /// <summary>
        /// Add an additional scan directory
        /// </summary>
        /// <param name="scanDirectory">string</param>
        public ApplicationBootstrapper AddScanDirectory(string scanDirectory)
        {
            Resolver.AddScanDirectory(scanDirectory);
            return this;
        }

        /// <summary>
        /// Add additional scan directories
        /// </summary>
        /// <param name="scanDirectories">IEnumerable</param>
        public ApplicationBootstrapper AddScanDirectories(IEnumerable<string> scanDirectories)
        {
            foreach (var scanDirectory in scanDirectories)
            {
                if (string.IsNullOrEmpty(scanDirectory))
                {
                    continue;
                }
                Resolver.AddScanDirectory(scanDirectory);
            }
            return this;
        }

        /// <summary>
        /// Find a certain assembly in the available scan directories and load this
        /// </summary>
        /// <param name="pattern">string with the pattern for the files</param>
        /// <param name="extensions">IEnumerable with the extensions to process, default is .dll</param>
        /// <param name="allowEmbedded">bool which specifies if the </param>
        public ApplicationBootstrapper FindAndLoadAssemblies(string pattern, IEnumerable<string> extensions = null, bool allowEmbedded = true)
        {
            if (string.IsNullOrEmpty(pattern))
            {
                throw new ArgumentNullException(nameof(pattern));
            }

            var fileRegex = FileTools.FilenameToRegex(pattern, extensions ?? new[] { ".dll" });
            LoadAssemblies(FileLocations.Scan(Resolver.ScanDirectories, fileRegex).Select(tuple => tuple.Item1).ToList());

            if (!allowEmbedded)
            {
                return this;
            }

            foreach (var assemblyName in Resolver.EmbeddedAssemblyNames().Where(assemblyName => fileRegex.IsMatch(assemblyName + ".dll")).ToList())
            {
                Resolver.LoadEmbeddedAssembly(assemblyName);
            }

            return this;
        }

        /// <summary>
        /// Load the specified assembly files
        /// </summary>
        /// <param name="assemblyFiles">string array with assembly files</param>
        public ApplicationBootstrapper LoadAssemblies(params string[] assemblyFiles)
        {
            LoadAssemblies((IEnumerable<string>)assemblyFiles);
            return this;
        }

        /// <summary>
        /// Load the specified assembly files
        /// </summary>
        /// <param name="assemblyFiles">IEnumerable of string</param>
        public ApplicationBootstrapper LoadAssemblies(IEnumerable<string> assemblyFiles)
        {
            foreach (var assemblyFile in assemblyFiles)
            {
                Log.Debug().WriteLine("Loading {0}", assemblyFile);
                if (Resolver.LoadAssembly(assemblyFile) != null)
                {
                    Log.Debug().WriteLine("Loaded {0}", assemblyFile);
                }
            }
            return this;
        }

        /// <summary>
        /// Configure the Bootstrapper
        /// </summary>
        public virtual ApplicationBootstrapper Configure()
        {
            // It's no problem when the builder is already created, skip recreating!
            if (_builder != null)
            {
                return this;
            }
            Log.Verbose().WriteLine("Configuring");

            _builder = new ContainerBuilder();
            _builder.Properties["applicationName"] = ApplicationName;

            // Enable logging
            //_builder.RegisterModule<LogRequestModule>();
            // Make sure Attributes are allowed
            _builder.RegisterModule<AttributedMetadataModule>();
            // Provide the startup & shutdown functionality
            _builder.RegisterType<ServiceHandler>().AsSelf().SingleInstance();
            // Provide the IResourceProvider
            _builder.RegisterInstance(Resolver.Resources).As<IResourceProvider>().ExternallyOwned();
            return this;
        }

        /// <summary>
        /// Initialize the bootstrapper
        /// </summary>
        public virtual Task<bool> InitializeAsync(CancellationToken cancellationToken = default)
        {
            if (Container != null)
            {
                throw new NotSupportedException("Initialize can only be called once.");
            }
            Log.Verbose().WriteLine("Initializing");

            Configure();

            // Process all assemblies, while doing this more might be loaded, so process those again
            var processedAssemblies = new HashSet<string>();
            int countBefore;
            do
            {
                countBefore = Resolver.LoadedAssemblies.Count;

                var assembliesToProcess = Resolver.LoadedAssemblies.Keys.ToList()
                    .Where(key => processedAssemblies.Add(key))
                    .Where(key => !Resolver.AssembliesToIgnore.IsMatch(key))
                    .Where(key => !Resolver.LoadedAssemblies[key].IsDynamic).ToList();
                if (!assembliesToProcess.Any())
                {
                    break;
                }
                if (Log.IsDebugEnabled())
                {
                    Log.Debug().WriteLine("Processing assemblies {0}", string.Join(",", assembliesToProcess));
                }

                foreach (var key in assembliesToProcess)
                {
                    try
                    {
                        _builder.RegisterAssemblyModules(Resolver.LoadedAssemblies[key]);
                    }
                    catch (Exception ex)
                    {
                        Log.Warn().WriteLine(ex, "Couldn't read modules in {0}", key);
                    }
                }
                
            } while (Resolver.LoadedAssemblies.Count > countBefore);          

            // Now build the container
            Container = _builder.Build();

            // Inform that the container was created
            OnContainerCreated?.Invoke(Container);

            // And the scope
            Scope = Container.BeginLifetimeScope();

            return Task.FromResult(true);
        }

        /// <summary>
        /// Start the IStartupModules
        /// </summary>
        /// <param name="cancellationToken">CancellationToken</param>
        public async Task StartupAsync(CancellationToken cancellationToken = default)
        {
            if (Container == null)
            {
                await InitializeAsync(cancellationToken);
            }
            _isStartedUp = true;
            await Scope.Resolve<ServiceHandler>().StartupAsync(cancellationToken);
        }

        /// <summary>
        /// Shutdown the IShutdownModules
        /// </summary>
        /// <param name="cancellationToken">CancellationToken</param>
        public Task ShutdownAsync(CancellationToken cancellationToken = default)
        {
            if (!_isStartedUp)
            {
                throw new NotSupportedException("Start before shutdown!");
            }
            _isShutDown = true;
            return Scope.Resolve<ServiceHandler>().ShutdownAsync(cancellationToken);
        }

        /// <summary>
        /// Dispose the scope and container
        /// </summary>
        public void Dispose()
        {
            // When startup was called, but shutdown not, do this now
            if (_isStartedUp && !_isShutDown)
            {
                // Auto shutdown
                using (new NoSynchronizationContextScope())
                {
                    ShutdownAsync().Wait();
                }
            }

            var reversedDisposables = _disposables.Reverse().ToList();
            _disposables.Clear();
            foreach (var disposable in reversedDisposables)
            {
                disposable?.Dispose();
            }

            Scope?.Dispose();
            Container?.Dispose();
            Resolver.Dispose();
        }
    }
}
