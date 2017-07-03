#region Dapplo 2016-2017 - GNU Lesser General Public License

// Dapplo - building blocks for .NET applications
// Copyright (C) 2016-2017 Dapplo
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

using System;
using System.ComponentModel.Composition.Hosting;
using System.Reflection;
using System.Threading.Tasks;
using Dapplo.Addons.Bootstrapper;
using Dapplo.Addons.Bootstrapper.Internal;
using Dapplo.Addons.Tests.Entities;
using Dapplo.Log;
using Dapplo.Log.XUnit;
using Xunit;
using Xunit.Abstractions;

#endregion

namespace Dapplo.Addons.Tests
{
    public class CompositionBootstrapperTests
    {
        public CompositionBootstrapperTests(ITestOutputHelper testOutputHelper)
        {
            LogSettings.RegisterDefaultLogger<XUnitLogger>(LogLevels.Verbose, testOutputHelper);
        }

        [Fact]
        public async Task TestDisposing()
        {
            bool isDisposed = false;
            using (var compositionBootstrapper = new CompositionBootstrapper())
            {
                compositionBootstrapper.RegisterForDisposal(SimpleDisposable.Create(() => isDisposed = true));
                await compositionBootstrapper.InitializeAsync();
                await compositionBootstrapper.StopAsync();
                Assert.False(isDisposed);
            }
            Assert.True(isDisposed);
        }

        [Fact]
        public async Task TestArgumentNull()
        {
            var compositionBootstrapper = new CompositionBootstrapper();
            Assert.Throws<ArgumentNullException>(() => compositionBootstrapper.Add((Assembly) null));
            Assert.Throws<ArgumentNullException>(() => compositionBootstrapper.Add((AssemblyCatalog) null));
            Assert.Throws<ArgumentNullException>(() => compositionBootstrapper.FindAndLoadAssemblies(null));
            Assert.Throws<ArgumentNullException>(() => compositionBootstrapper.Add((Type) null));
            Assert.Throws<ArgumentNullException>(() => compositionBootstrapper.Add((ExportProvider) null));
            await compositionBootstrapper.InitializeAsync().ConfigureAwait(false);
            Assert.Throws<ArgumentNullException>(() => compositionBootstrapper.Export<string>(null));
            Assert.Throws<ArgumentNullException>(() => compositionBootstrapper.Release(null));
            Assert.Throws<ArgumentNullException>(() => compositionBootstrapper.ProvideDependencies(null));
            compositionBootstrapper.Dispose();
        }

        [Fact]
        public void TestNotInitialized()
        {
            var compositionBootstrapper = new CompositionBootstrapper();
            Assert.Throws<InvalidOperationException>(() => compositionBootstrapper.GetInstance<string>());
            Assert.Throws<InvalidOperationException>(() => compositionBootstrapper.GetInstance<string>(null));
            Assert.Throws<InvalidOperationException>(() => compositionBootstrapper.GetInstance(typeof(string), null));
            Assert.Throws<InvalidOperationException>(() => compositionBootstrapper.GetInstance(typeof(string)));
            Assert.Throws<InvalidOperationException>(() => compositionBootstrapper.GetAllInstances(typeof(string)));
            Assert.Throws<InvalidOperationException>(() => compositionBootstrapper.GetAllInstances<string>());

            Assert.Throws<InvalidOperationException>(() => compositionBootstrapper.Export("Hello"));
            Assert.Throws<InvalidOperationException>(() => compositionBootstrapper.GetExport<string>());
            Assert.Throws<InvalidOperationException>(() => compositionBootstrapper.GetExport<string, IStartupMetadata>());
            Assert.Throws<InvalidOperationException>(() => compositionBootstrapper.GetExport(typeof(string)));
            Assert.Throws<InvalidOperationException>(() => compositionBootstrapper.GetService(typeof(string)));
            Assert.Throws<InvalidOperationException>(() => compositionBootstrapper.GetExports(typeof(string)));
            Assert.Throws<InvalidOperationException>(() => compositionBootstrapper.GetExports<string>());
            Assert.Throws<InvalidOperationException>(() => compositionBootstrapper.GetExports<string, IStartupMetadata>());
            Assert.Throws<InvalidOperationException>(() => compositionBootstrapper.Release(null));
            Assert.Throws<InvalidOperationException>(() => compositionBootstrapper.ProvideDependencies(null));
            compositionBootstrapper.Dispose();
        }

        [Fact]
        public async Task TestExportRelease()
        {
            using (var compositionBootstrapper = new CompositionBootstrapper())
            {
                await compositionBootstrapper.InitializeAsync().ConfigureAwait(false);

                // Create a string export with "Hello"
                var export = compositionBootstrapper.Export("Hello");
                // Make sure it's there
                Assert.Equal("Hello", compositionBootstrapper.GetExport(typeof(string)));
                // Remove it
                compositionBootstrapper.Release(export);

                // Create a string export with "World"
                compositionBootstrapper.Export("World");
                // Make sure it's there
                Assert.Equal("World", compositionBootstrapper.GetExport(typeof(string)));
            }
        }

        [Fact]
        public async Task TestDependencyProvider()
        {
            using (var compositionBootstrapper = new CompositionBootstrapper())
            {
                await compositionBootstrapper.InitializeAsync().ConfigureAwait(false);

                // Create a string export with "Hello"
                compositionBootstrapper.Export("Hello");

                var dependencyProvider = compositionBootstrapper.GetExport<IDependencyProvider>().Value;


                var dependencyTaker = new DependencyTaker();
                dependencyProvider.ProvideDependencies(dependencyTaker);
                // Make sure it's there
                Assert.Equal("Hello", dependencyTaker.Take);
            }
        }
    }
}