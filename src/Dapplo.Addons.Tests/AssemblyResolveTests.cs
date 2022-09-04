// Dapplo - building blocks for .NET applications
// Copyright (C) 2016-2021 Dapplo
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

#if !NET6_0
using System;
using System.Reflection;
using System.Threading.Tasks;
using Autofac;
using Dapplo.Addons.Bootstrapper;
using Dapplo.Addons.Bootstrapper.Resolving;
using Dapplo.Addons.Tests.TestAssembly;
using Dapplo.Log;
using Dapplo.Log.XUnit;
using Xunit;
using Xunit.Abstractions;

namespace Dapplo.Addons.Tests
{
    /// <summary>
    ///     This tests the Assembly resolve functionality with Costura.
    ///     Disclaimer, the way I use costura doesn't work with .NET 5, and I don't feel like fixing it
    /// </summary>
    public class AssemblyResolveTests
    {
        private static readonly LogSource Log = new();

        private const string ScanDirectory =
#if NET471
#if DEBUG
            @"..\..\..\..\Dapplo.Addons.TestAddonWithCostura\bin\Debug\net471";
#else
            @"..\..\..\..\Dapplo.Addons.TestAddonWithCostura\bin\Release\net471";
#endif
#else
#if DEBUG
            @"..\..\..\..\Dapplo.Addons.TestAddonWithCostura\bin\Debug\net6.0-windows";
#else
            @"..\..\..\..\Dapplo.Addons.TestAddonWithCostura\bin\Release\net6.0-windows";
#endif
#endif


        /// <summary>
        /// </summary>
        /// <param name="testOutputHelper"></param>
        public AssemblyResolveTests(ITestOutputHelper testOutputHelper) => LogSettings.RegisterDefaultLogger<XUnitLogger>(LogLevels.Verbose, testOutputHelper);

        private void ThisForcesDelayedLoadingOfAssembly()
        {
            var helloWorld = ExternalClass.HelloWord();
            Assert.Equal(nameof(ExternalClass.HelloWord), helloWorld);
        }

        [Fact]
        public async Task TestCostura()
        {
            var applicationConfig = ApplicationConfigBuilder
                .Create()
                .WithApplicationName("TestCostura")
                .WithScanDirectories(
                    FileLocations.StartupDirectory, ScanDirectory
                )
                // Add Dapplo.Addons.TestAddonWithCostura
                .WithAssemblyNames("Dapplo.Addons.TestAddonWithCostura")
                .BuildApplicationConfig();

            Log.Debug().WriteLine("Current: {0}", System.IO.Directory.GetCurrentDirectory());
            Log.Debug().WriteLine("ScanDirectory passed: {0}", FileTools.NormalizeDirectory(ScanDirectory));
            Log.Debug().WriteLine("ScanDirectories: {0}", string.Join(",", applicationConfig.ScanDirectories));

            Log.Debug().WriteLine("Where: {0}", System.IO.Path.Combine(System.IO.Directory.GetCurrentDirectory(), ScanDirectory));
            using (var bootstrapper = new ApplicationBootstrapper(applicationConfig))
            {
                await bootstrapper.InitializeAsync();
                var jiraAssembly = Assembly.Load("Dapplo.Jira");
                Assert.NotNull(jiraAssembly);
            }
        }

        [Fact]
        public async Task TestCostura_Nested_AssemblyLoad()
        {
            var applicationConfig = ApplicationConfigBuilder.Create()
                .WithApplicationName("TestCostura_Nested")
                .WithScanDirectories(
                    FileLocations.StartupDirectory, ScanDirectory
                )
                // Add Dapplo.Addons.TestAddonWithCostura
                .WithAssemblyNames("Dapplo.Addons.TestAddonWithCostura")
                .BuildApplicationConfig();
            using (var bootstrapper = new ApplicationBootstrapper(applicationConfig))
            {
                await bootstrapper.InitializeAsync().ConfigureAwait(false);
                var svgAssembly = Assembly.Load("Svg");
                Assert.NotNull(svgAssembly);
            }
        }

        [Fact]
        public async Task TestCostura_Nested_ResolverLoad()
        {
            var applicationConfig = ApplicationConfigBuilder.Create()
                .WithApplicationName("TestCostura_Nested")
                .WithScanDirectories(
                    FileLocations.StartupDirectory, ScanDirectory
                )
                // Add Dapplo.Addons.TestAddonWithCostura
                .WithAssemblyNames("Dapplo.Addons.TestAddonWithCostura")
                .BuildApplicationConfig();
            using (var bootstrapper = new ApplicationBootstrapper(applicationConfig))
            {
                await bootstrapper.InitializeAsync().ConfigureAwait(false);
                var jiraAssembly = bootstrapper.Resolver.LoadAssembly("Svg");
                Assert.NotNull(jiraAssembly);
            }
        }

    }
}
#endif