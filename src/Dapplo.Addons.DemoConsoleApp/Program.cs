using System.Collections.Generic;
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
                };
                bootstrapper
                    .AddScanDirectories(scanDirectories)
                    .FindAndLoadAssemblies("Dapplo.HttpExtensions");

                await bootstrapper.InitializeAsync().ConfigureAwait(false);

                // Find all, currently, available assemblies
                foreach (var resource in bootstrapper.Resolver.EmbeddedAssemblyNames())
                {
                    Log.Debug().WriteLine("Available assembly {0}", resource);
                }
            }

            return 0;
        }
    }
}
