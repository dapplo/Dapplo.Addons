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

using System;
using System.Threading.Tasks;
using Autofac;
using Dapplo.Addons.Bootstrapper;
using Dapplo.Addons.Bootstrapper.Resolving;
using Dapplo.Addons.Tests.Utils;
using Dapplo.Log;
using Dapplo.Log.XUnit;
using Xunit;
using Xunit.Abstractions;

namespace Dapplo.Addons.Tests
{
    [Collection("IniConfig")]
    public sealed class ApplicationBootstrapperTests
    {
        private const string ApplicationName = "Dapplo";
        private readonly string[] _testAssemblyDirectories = {
            FileLocations.StartupDirectory,
#if DEBUG
                    @"..\..\..\..\Dapplo.Addons.TestAddon\bin\Debug\net5.0-windows",
                    @"..\..\..\..\Dapplo.Addons.TestAddonWithCostura\bin\Debug\net5.0-windows"
#else
                    @"..\..\..\..\Dapplo.Addons.TestAddon\bin\Release\net5.0-windows",
                    @"..\..\..\..\Dapplo.Addons.TestAddonWithCostura\bin\Release\net5.0-windows"
#endif
        };

        public ApplicationBootstrapperTests(ITestOutputHelper testOutputHelper)
        {
            LogSettings.RegisterDefaultLogger<XUnitLogger>(LogLevels.Verbose, testOutputHelper);
        }

        [Fact]
        public async Task Test_StartupException()
        {
            bool isDisposed = false;
            AssemblyUtils.SetEntryAssembly(GetType().Assembly);

            var applicationConfig = ApplicationConfigBuilder
                .Create()
                .WithApplicationName(ApplicationName)
                .WithScanDirectories(
                    _testAssemblyDirectories
                )
                // Add all file starting with Dapplo and ending on .dll
                .WithAssemblyPatterns("Dapplo*")
                .BuildApplicationConfig();

            using (var bootstrapper = new ApplicationBootstrapper(applicationConfig))
            {
                bootstrapper.Configure();

                // Makes the startup break
                bootstrapper.Builder.Register(context => true);

                bootstrapper.RegisterForDisposal(SimpleDisposable.Create(() => isDisposed = true));

                // Initialize, so we can export
                Assert.True(await bootstrapper.InitializeAsync().ConfigureAwait(false), "Not initialized");
                
                // Start the composition, and IStartupActions
                await Assert.ThrowsAsync<NotSupportedException>(async () => await bootstrapper.StartupAsync().ConfigureAwait(false));
            }
            // Dispose automatically calls IShutdownActions
            Assert.True(isDisposed);
        }

        [Fact]
        public void TestConstructorAndCleanup()
        {
            var applicationConfig = ApplicationConfigBuilder
                .Create()
                .WithApplicationName(ApplicationName)
                .BuildApplicationConfig();

            var bootstrapper = new ApplicationBootstrapper(applicationConfig);
            bootstrapper.Dispose();
        }

        [Fact]
        public void TestConstructorWithMutexAndCleanup()
        {
            var applicationConfig = ApplicationConfigBuilder
                .Create()
                .WithApplicationName("Test")
                .WithMutex(Guid.NewGuid().ToString())
                .BuildApplicationConfig();
            using (var bootstrapper = new ApplicationBootstrapper(applicationConfig))
            {
                Assert.False(bootstrapper.IsAlreadyRunning);
            }
        }

        [Fact]
        public void TestNewNullApplicationName()
        {
            Assert.Throws<ArgumentNullException>(() => new ApplicationBootstrapper(null));
        }

        [Fact]
        public async Task TestStartupShutdown()
        {
            AssemblyUtils.SetEntryAssembly(GetType().Assembly);

            var applicationConfig = ApplicationConfigBuilder
                .Create()
                .WithApplicationName(ApplicationName)
                .WithScanDirectories(_testAssemblyDirectories)
                // Add all assemblies starting with Dapplo
                .WithAssemblyPatterns("Dapplo*")
                .BuildApplicationConfig();
            using (var bootstrapper = new ApplicationBootstrapper(applicationConfig))
            {
                bootstrapper.Configure();
#if DEBUG
                bootstrapper.EnableActivationLogging = true;
#endif
                // Start the composition, and IStartupActions
                Assert.True(await bootstrapper.InitializeAsync().ConfigureAwait(false), "Couldn't run");

                Assert.Contains(bootstrapper.LoadedAssemblies, addon => addon.GetName().Name.EndsWith("TestAddon"));
            }
            // Dispose automatically calls IShutdownActions
        }
    }
}