#region Dapplo 2016-2019 - GNU Lesser General Public License

// Dapplo - building blocks for .NET applications
// Copyright (C) 2016-2019 Dapplo
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
using Dapplo.Addons.Bootstrapper.Internal;
using Dapplo.Addons.Bootstrapper.Resolving;
using Dapplo.Addons.Bootstrapper.Services;
using Dapplo.Log;

namespace Dapplo.Addons.Bootstrapper
{
    /// <summary>
    /// This is a bootstrapper for Autofac
    /// </summary>
    public class ApplicationBootstrapper : IApplicationBootstrapper
    {
        private static readonly LogSource Log = new LogSource();
        private readonly ResourceMutex _resourceMutex;
        private readonly IList<IDisposable> _disposables = new List<IDisposable>();
        private readonly ApplicationConfig _applicationConfig;
        private bool _loadedAssemblies;
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
        public string ApplicationName => _applicationConfig.ApplicationName;

        /// <summary>
        /// Log all Autofac activations, must be set before the container is build
        /// </summary>
        public bool EnableActivationLogging { get; set; }

        /// <summary>
        /// An IEnumerable with the loaded assemblies, but filtered to the ones not from the .NET Framework (where possible) 
        /// </summary>
        public IEnumerable<Assembly> LoadedAssemblies => Resolver.LoadedAssemblies
            .Where(pair => !Resolver.AssembliesToIgnore.IsMatch(pair.Key) && !pair.Value.IsDynamic)
            .Select(pair => pair.Value);

        /// <summary>
        /// Create the application bootstrapper
        /// </summary>
        /// <param name="applicationConfig">ApplicationConfig with the complete configuration</param>
        public ApplicationBootstrapper(ApplicationConfig applicationConfig)
        {
            _applicationConfig = applicationConfig ?? throw new ArgumentNullException(nameof(applicationConfig));
            Instance = this;
            if (Thread.CurrentThread.Name == null)
            {
                Thread.CurrentThread.Name = applicationConfig.ApplicationName;
            }
            if (applicationConfig.HasMutex)
            {
                _resourceMutex = ResourceMutex.Create(applicationConfig.Mutex, applicationConfig.ApplicationName, applicationConfig.UseGlobalMutex);
            }

            Resolver = new AssemblyResolver(_applicationConfig);
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
        public IApplicationBootstrapper RegisterForDisposal(IDisposable disposable)
        {
            if (disposable == null)
            {
                throw new ArgumentNullException(nameof(disposable));
            }
            _disposables.Add(disposable);
            return this;
        }

        /// <summary>
        /// Configure the Bootstrapper
        /// </summary>
        public virtual IApplicationBootstrapper Configure()
        {
            // It's no problem when the builder is already created, skip recreating!
            if (_builder != null)
            {
                return this;
            }

            if (Log.IsVerboseEnabled())
            {
                Log.Verbose().WriteLine("Configuring");
            }

            _builder = new ContainerBuilder();
            _builder.Properties[nameof(ApplicationName)] = ApplicationName;
            foreach (var propertiesKey in _applicationConfig.Properties.Keys)
            {
                _builder.Properties[propertiesKey] = _applicationConfig.Properties[propertiesKey];
            }
            // Provide the IAssemblyResolver
            _builder.RegisterInstance<IAssemblyResolver>(Resolver).ExternallyOwned();
            // Provide the IResourceProvider
            _builder.RegisterInstance(Resolver.Resources).ExternallyOwned();
            // Provide the IApplicationBootstrapper
            _builder.RegisterInstance<IApplicationBootstrapper>(this).ExternallyOwned();
            return this;
        }

        /// <summary>
        /// Load all specified assemblies
        /// </summary>
        /// <param name="cancellationToken">CancellationToken</param>
        /// <exception cref="DllNotFoundException"></exception>
        public virtual async Task LoadAssemblies(CancellationToken cancellationToken = default)
        {
            if (_loadedAssemblies)
            {
                return;
            }

            _loadedAssemblies = true;

            var loaderTasks = new List<Task>();
            foreach (var wantedAssemblyName in _applicationConfig.AssemblyNames)
            {
                if (Resolver.AvailableAssemblies.TryGetValue(wantedAssemblyName, out var assemblyLocationInformation))
                {
                    if (_applicationConfig.UseAsyncAssemblyLoading)
                    {
                        loaderTasks.Add(Task.Run(() => Resolver.LoadAssembly(assemblyLocationInformation), cancellationToken));
                    }
                    else
                    {
                        Resolver.LoadAssembly(assemblyLocationInformation);
                    }
                }
                else
                {
                    throw new DllNotFoundException($"Assembly {wantedAssemblyName} not found!");
                }
            }

            // Start loading!
            foreach (var availableAssembly in Resolver.AvailableAssemblies.Values.Where(information => _applicationConfig.AssemblyNamePatterns.Any(regex => regex.IsMatch(information.Name))))
            {
                if (_applicationConfig.UseAsyncAssemblyLoading)
                {
                    loaderTasks.Add(Task.Run(() => Resolver.LoadAssembly(availableAssembly), cancellationToken));
                }
                else
                {
                    Resolver.LoadAssembly(availableAssembly);
                }
            }

            if (loaderTasks.Count > 0)
            {
                await Task.WhenAll(loaderTasks).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Initialize the bootstrapper
        /// </summary>
        public virtual async Task<bool> InitializeAsync(CancellationToken cancellationToken = default)
        {
            if (Container != null)
            {
                throw new NotSupportedException("Initialize can only be called once.");
            }

            if (Log.IsVerboseEnabled())
            {
                Log.Verbose().WriteLine("Initializing");
            }
            await LoadAssemblies(cancellationToken).ConfigureAwait(false);

            Configure();
            _builder.Properties[nameof(EnableActivationLogging)] = EnableActivationLogging.ToString();
            // Process all assemblies, while doing this more might be loaded, so process those again
            var processedAssemblies = new HashSet<string>();
            int countBefore;
            do
            {
                countBefore = Resolver.LoadedAssemblies.Count;

                var assembliesToProcess = Resolver.LoadedAssemblies.ToList()
                    // Skip dynamic assemblies
                    .Where(pair => !pair.Value.IsDynamic)
                    // Ignore well know assemblies we don't care about
                    .Where(pair => !Resolver.AssembliesToIgnore.IsMatch(pair.Key))
                    // Only scan the assemblies which reference Dapplo.Addons
                    .Where(pair => pair.Value.GetReferencedAssemblies()
                        .Any(assemblyName => string.Equals("Dapplo.Addons", assemblyName.Name, StringComparison.OrdinalIgnoreCase)))
                    .Where(pair => processedAssemblies.Add(pair.Key));

                foreach (var assemblyToProcess in assembliesToProcess)
                {
                    try
                    {
                        if (Log.IsDebugEnabled())
                        {
                            Log.Debug().WriteLine("Processing assembly {0}", assemblyToProcess.Key);
                        }

                        _builder.RegisterAssemblyModules(assemblyToProcess.Value);
                    }
                    catch (Exception ex)
                    {
                        Log.Warn().WriteLine(ex, "Couldn't read modules in {0}", assemblyToProcess.Key);
                    }
                }
                
            } while (Resolver.LoadedAssemblies.Count > countBefore);          

            // Now build the container
            try
            {
                Container = _builder.Build();
            }
            catch (Exception ex)
            {
                Log.Error().WriteLine(ex, "Couldn't create the container.");
                throw;
            }

            // Inform that the container was created
            OnContainerCreated?.Invoke(Container);

            // And the scope
            Scope = Container.BeginLifetimeScope();

            return true;
        }

        /// <summary>
        /// Start the IStartupModules
        /// </summary>
        /// <param name="cancellationToken">CancellationToken</param>
        public async Task StartupAsync(CancellationToken cancellationToken = default)
        {
            _isStartedUp = true;
            if (Container == null)
            {
                await InitializeAsync(cancellationToken).ConfigureAwait(false);
            }
            await Scope.Resolve<ServiceStartupShutdown>().StartupAsync(cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Shutdown the IShutdownModules
        /// </summary>
        /// <param name="cancellationToken">CancellationToken</param>
        public Task ShutdownAsync(CancellationToken cancellationToken = default)
        {
            if (!_isStartedUp)
            {
                throw new NotSupportedException("Please call StartupAsync before shutdown!");
            }
            _isShutDown = true;
            return Scope.Resolve<ServiceStartupShutdown>().ShutdownAsync(cancellationToken);
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
                    try
                    {
                        ShutdownAsync().Wait();
                    }
                    catch (AggregateException ex)
                    {
                        throw ex.GetBaseException();
                    }
                }
            }

            if (_disposables.Count > 0)
            {
                var reversedDisposables = _disposables.Reverse().Where(disposable => disposable != null).ToList();
                _disposables.Clear();
                foreach (var disposable in reversedDisposables)
                {
                    disposable?.Dispose();
                }
            }

            Scope?.Dispose();
            Container?.Dispose();
            Resolver.Dispose();
        }
    }
}
