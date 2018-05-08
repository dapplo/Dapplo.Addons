using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
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
        private static readonly Regex AssembliesToIgnore = new Regex(@"(Microsoft\..*|mscorlib|UIAutomationProvider|PresentationFramework|PresentationCore|WindowsBase|autofac.*|Dapplo\.Log|system.*|.*resources|Dapplo\.InterfaceImpl.*)", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private readonly ResourceMutex _resourceMutex;
        private readonly AssemblyResolver _resolver = new AssemblyResolver();
        private bool _isStartedUp;
        private bool _isShutDown;

        /// <summary>
        /// The current instance
        /// </summary>
        public static ApplicationBootstrapper Instance { get; private set; }

        private ContainerBuilder _builder;

        /// <summary>
        /// Provides access to the builder, as long as it can be modified.
        /// </summary>
        public ContainerBuilder Builder => Container == null ?_builder : null;

        /// <summary>
        /// Provides the Autofac container
        /// </summary>
        public IContainer Container { get; private set; }

        /// <summary>
        /// Provides the Autofac primary lifetime scope
        /// </summary>
        public ILifetimeScope Scope { get; private set; }

        /// <summary>
        /// The name of the application
        /// </summary>
        public string ApplicationName { get; }

        public IEnumerable<Assembly> LoadedAssemblies => _resolver.LoadedAssemblies
            .Where(pair => !AssembliesToIgnore.IsMatch(pair.Key) && !pair.Value.IsDynamic)
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
            Thread.CurrentThread.Name = applicationName;

            if (mutexId != null)
            {
                _resourceMutex = ResourceMutex.Create(mutexId, applicationName, global);
            }
        }

        /// <summary>
        ///     Returns if the Mutex is locked, in other words if the Bootstrapper can continue
        ///     This also returns true if no mutex is used
        /// </summary>
        public bool IsAlreadyRunning => _resourceMutex != null && !_resourceMutex.IsLocked;

        /// <summary>
        /// Load the specified assembly files
        /// </summary>
        /// <param name="assemblyFiles">string array with assembly files</param>
        public void LoadAssemblies(params string[] assemblyFiles)
        {
            LoadAssemblies((IEnumerable<string>)assemblyFiles);
        }

        /// <summary>
        /// Load the specified assembly files
        /// </summary>
        /// <param name="assemblyFiles">IEnumerable of string</param>
        public void LoadAssemblies(IEnumerable<string> assemblyFiles)
        {
            foreach (var assemblyFile in assemblyFiles)
            {
                Log.Debug().WriteLine("Loading {0}", assemblyFile);
                if (_resolver.LoadAssembly(assemblyFile))
                {
                    Log.Debug().WriteLine("Loaded {0}", assemblyFile);
                }
                else
                {
                    Log.Debug().WriteLine("Ignored / failed: {0}", assemblyFile);
                }
            }
        }

        /// <summary>
        /// Configure the Bootstrapper
        /// </summary>
        public virtual void Configure()
        {
            // It's no problem when the builder is already created, skip recreating!
            if (_builder != null)
            {
                return;
            }
            Log.Verbose().WriteLine("Configuring");

            _builder = new ContainerBuilder();
            _builder.Properties["applicationName"] = ApplicationName;

            // Enable logging
            //_builder.RegisterModule<LogRequestModule>();
            // Make sure Attributes are allowed
            _builder.RegisterModule<AttributedMetadataModule>();
            // Provide the startup & shutdown functionality
            _builder.RegisterType<StartupHandler>().AsSelf().SingleInstance();
            _builder.RegisterType<ShutdownHandler>().AsSelf().SingleInstance();
        }

        /// <summary>
        /// Initialize the bootstrapper
        /// </summary>
        public virtual Task InitializeAsync(CancellationToken cancellationToken = default)
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
                countBefore = _resolver.LoadedAssemblies.Count;

                var assembliesToProcess = _resolver.LoadedAssemblies.Keys.ToList()
                    .Where(key => processedAssemblies.Add(key))
                    .Where(key => !AssembliesToIgnore.IsMatch(key))
                    .Where(key => !_resolver.LoadedAssemblies[key].IsDynamic).ToList();
                if (Log.IsDebugEnabled())
                {
                    Log.Debug().WriteLine("Processing assemblies {0}", string.Join(",", assembliesToProcess));
                }

                _builder.RegisterAssemblyModules(assembliesToProcess.Select(key => _resolver.LoadedAssemblies[key]).ToArray());
            } while (_resolver.LoadedAssemblies.Count > countBefore);          

            // Now build the container
            Container = _builder.Build();
            // And the scope
            Scope = Container.BeginLifetimeScope();

            return Task.FromResult(true);
        }

        /// <summary>
        /// Start the IStartupModules
        /// </summary>
        /// <param name="cancellationToken">CancellationToken</param>
        public Task StartupAsync(CancellationToken cancellationToken = default)
        {
            _isStartedUp = true;
            return Scope.Resolve<StartupHandler>().StartupAsync(cancellationToken);
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
            return Scope.Resolve<ShutdownHandler>().ShutdownAsync(cancellationToken);
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
            Scope?.Dispose();
            Container?.Dispose();
        }
    }
}
