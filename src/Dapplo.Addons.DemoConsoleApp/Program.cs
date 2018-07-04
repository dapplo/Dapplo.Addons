using System.Reflection;
using System.Threading.Tasks;
using Autofac;
using Dapplo.Addons.Bootstrapper;
using Dapplo.Addons.Bootstrapper.Resolving;
using Dapplo.Addons.Bootstrapper.Services;
using Dapplo.Log;
using Dapplo.Log.Loggers;

namespace Dapplo.Addons.DemoConsoleApp
{
    internal static class Program
    {
        private static readonly LogSource Log = new LogSource();
        public static async Task<int> Main(string[] args)
        {
            LogSettings.RegisterDefaultLogger<DebugLogger>(LogLevels.Verbose);

            var applicationConfig = ApplicationConfigBuilder.Create()
                .WithApplicationName("DemoConsoleApp")
                .WithScanDirectories(
                    FileLocations.StartupDirectory,
                    @"MyOtherLibs",
#if DEBUG
                    @"..\..\..\Dapplo.Addons.TestAddonWithCostura\bin\Debug"
#else
                    @"..\..\..\Dapplo.Addons.TestAddonWithCostura\bin\Release"
#endif
                )
                .WithAssemblyNames("Dapplo.HttpExtensions", "Dapplo.Addons.TestAddonWithCostura").BuildApplicationConfig();

            using (var bootstrapper = new ApplicationBootstrapper(applicationConfig))
            {
#if DEBUG
                bootstrapper.EnableActivationLogging = true;
#endif
                bootstrapper.Configure();

                await bootstrapper.InitializeAsync().ConfigureAwait(false);

                bootstrapper.Container.Resolve<ServiceStartupShutdown>();
                // Find all, currently, available assemblies
                if (Log.IsDebugEnabled())
                {
                    foreach (var resource in bootstrapper.Resolver.EmbeddedAssemblyNames())
                    {
                        Log.Debug().WriteLine("Available embedded assembly {0}", resource);
                    }
                }
                Assembly.Load("Svg");
            }

            return 0;
        }
    }
}
