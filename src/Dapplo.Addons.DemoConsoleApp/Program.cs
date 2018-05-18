using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using Dapplo.Addons.Bootstrapper;
using Dapplo.Addons.Bootstrapper.Resolving;
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

            using (var bootstrapper = new ApplicationBootstrapper("DemoConsoleApp"))
            {
                bootstrapper.Configure();

                var scanDirectories = new List<string>
                {
                    FileLocations.StartupDirectory,
                    @"MyOtherLibs",
#if DEBUG
                    @"..\..\..\Dapplo.Addons.TestAddonWithCostura\bin\Debug",
#else
                    @"..\..\..\Dapplo.Addons.TestAddonWithCostura\bin\Release",
#endif
                };
                bootstrapper
                    .AddScanDirectories(scanDirectories)
                    .FindAndLoadAssemblies("Dapplo.HttpExtensions")
                    .FindAndLoadAssemblies("Dapplo.Addons.TestAddonWithCostura");

                await bootstrapper.InitializeAsync().ConfigureAwait(false);

                // Find all, currently, available assemblies
                foreach (var resource in bootstrapper.Resolver.EmbeddedAssemblyNames())
                {
                    Log.Debug().WriteLine("Available embedded assembly {0}", resource);
                }
                Assembly.Load("Svg");
            }

            return 0;
        }
    }
}
