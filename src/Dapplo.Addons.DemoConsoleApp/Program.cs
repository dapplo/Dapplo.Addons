using System.Linq;
using Dapplo.Addons.Bootstrapper.Resolving;
using Dapplo.Log;
using Dapplo.Log.Loggers;

namespace Dapplo.Addons.DemoConsoleApp
{
    internal static class Program
    {
        private static readonly LogSource Log = new LogSource();
        private static void Main(string[] args)
        {
            LogSettings.RegisterDefaultLogger<DebugLogger>(LogLevels.Verbose);
            using (var resolver = new AssemblyResolver())
            {
                // Find all, currently, available assemblies
                foreach (var resource in resolver.EmbeddedAssemblyNames())
                {
                    Log.Debug().WriteLine("Available assembly {0}", resource);
                }
                // Load one of those assemblies
                resolver.LoadEmbeddedAssembly(resolver.EmbeddedAssemblyNames().First(assemblyName => assemblyName.Contains("http")));
                resolver.LoadEmbeddedAssembly(resolver.EmbeddedAssemblyNames().First(assemblyName => assemblyName.Contains("autofac")));
            }
        }
    }
}
