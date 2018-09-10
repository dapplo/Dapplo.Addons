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

#region Usings

using System.Reflection;
using System.Threading.Tasks;
using Dapplo.Addons.Bootstrapper;
using Dapplo.Addons.Bootstrapper.Resolving;
using Dapplo.Addons.Tests.TestAssembly;
using Dapplo.Log;
using Dapplo.Log.XUnit;
using Xunit;
using Xunit.Abstractions;

#endregion

namespace Dapplo.Addons.Tests
{
    /// <summary>
    ///     This tests the Assembly resolve functionality.
    /// </summary>
    public class AssemblyResolveTests
    {
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
        public void TestCostura()
        {
            var applicationConfig = ApplicationConfigBuilder
                .Create()
                .WithApplicationName("TestCostura")
                .WithScanDirectories(
                    FileLocations.StartupDirectory,
#if DEBUG
                    @"..\..\..\..\Dapplo.Addons.TestAddonWithCostura\bin\Debug\net461"
#else
                    @"..\..\..\..\Dapplo.Addons.TestAddonWithCostura\bin\Release\net461"
#endif
                )
                // Add Dapplo.Addons.TestAddonWithCostura
                .WithAssemblyNames("Dapplo.Addons.TestAddonWithCostura")
                .BuildApplicationConfig();

            using (new ApplicationBootstrapper(applicationConfig))
            {
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
                    FileLocations.StartupDirectory,
#if DEBUG
                    @"..\..\..\..\Dapplo.Addons.TestAddonWithCostura\bin\Debug\net461"
#else
                    @"..\..\..\..\Dapplo.Addons.TestAddonWithCostura\bin\Release\net461"
#endif
                )
                // Add Dapplo.Addons.TestAddonWithCostura
                .WithAssemblyNames("Dapplo.Addons.TestAddonWithCostura")
                .BuildApplicationConfig();
            using (var bootstrapper = new ApplicationBootstrapper(applicationConfig))
            {
                await bootstrapper.InitializeAsync().ConfigureAwait(false);
                var jiraAssembly = Assembly.Load("Svg");
                Assert.NotNull(jiraAssembly);
            }
        }

        [Fact]
        public async Task TestCostura_Nested_ResolverLoad()
        {
            var applicationConfig = ApplicationConfigBuilder.Create()
                .WithApplicationName("TestCostura_Nested")
                .WithScanDirectories(
                    FileLocations.StartupDirectory,
#if DEBUG
                    @"..\..\..\..\Dapplo.Addons.TestAddonWithCostura\bin\Debug\net461"
#else
                    @"..\..\..\..\Dapplo.Addons.TestAddonWithCostura\bin\Release\net461"
#endif
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