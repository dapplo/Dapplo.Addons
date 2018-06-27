#region Dapplo 2016-2018 - GNU Lesser General Public License

// Dapplo - building blocks for .NET applications
// Copyright (C) 2016-2018 Dapplo
// 
// For more information see: http://dapplo.net/
// Dapplo repositories are hosted on GitHub: https://github.com/dapplo
// 
// This file is part of Dapplo.Utils
// 
// Dapplo.Utils is free software: you can redistribute it and/or modify
// it under the terms of the GNU Lesser General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// Dapplo.Utils is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU Lesser General Public License for more details.
// 
// You should have a copy of the GNU Lesser General Public License
// along with Dapplo.Utils. If not, see <http://www.gnu.org/licenses/lgpl.txt>.

#endregion

#region Usings

using System;
using Dapplo.Addons.Bootstrapper;
using Dapplo.Addons.Bootstrapper.Resolving;
using Dapplo.Log;
using Dapplo.Log.XUnit;
using Xunit;
using Xunit.Abstractions;

#endregion

namespace Dapplo.Addons.Tests
{
    /// <summary>
    /// Tests for the ApplicationConfig
    /// </summary>
    public class ApplicationConfigTests
    {
        public ApplicationConfigTests(ITestOutputHelper testOutputHelper)
        {
            LogSettings.RegisterDefaultLogger<XUnitLogger>(LogLevels.Verbose, testOutputHelper);
        }

        [Fact]
        public void Test_ApplicationConfig_Defaults()
        {
            var config = ApplicationConfigBuilder.Create().BuildApplicationConfig();
            Assert.NotEmpty(config.ApplicationName);
            Assert.Equal(FileLocations.AssemblyResolveDirectories, config.ScanDirectories);
            Assert.Contains(config.Extensions, s => s.Equals(".dll"));
            Assert.False(config.UseGlobalMutex);
            Assert.True(config.ScanForEmbeddedAssemblies);
            Assert.True(config.CopyEmbeddedAssembliesToFileSystem);
        }

        [Fact]
        public void Test_ApplicationConfig_ApplicationName()
        {
            var config = ApplicationConfigBuilder.Create().WithApplicationName("Dapplo").BuildApplicationConfig();
            Assert.Equal("Dapplo", config.ApplicationName);
        }

        [Fact]
        public void Test_ApplicationConfig_ScanDirectories_With()
        {
            var config = ApplicationConfigBuilder.Create().WithScanDirectories("TestFiles").BuildApplicationConfig();
            Assert.Contains(config.ScanDirectories, s => s.EndsWith(@"TestFiles", StringComparison.OrdinalIgnoreCase));
        }

        [Fact]
        public void Test_ApplicationConfig_AssemblyNames()
        {
            var config = ApplicationConfigBuilder.Create().WithAssemblyNames("Dapplo.Addons.Config").BuildApplicationConfig();
            Assert.Contains(config.AssemblyNames, s => s.Equals("Dapplo.Addons.Config"));
        }

        [Fact]
        public void Test_ApplicationConfig_Mutex()
        {
            var config = ApplicationConfigBuilder.Create().WithMutex("Dapplo.Addons.Config").BuildApplicationConfig();
            Assert.Equal("Dapplo.Addons.Config", config.Mutex);
        }

        [Fact]
        public void Test_ApplicationConfig_Extensions_Add()
        {
            var config = ApplicationConfigBuilder.Create().WithExtensions(".gsp").BuildApplicationConfig();
            Assert.Equal(config.Extensions, new[]{".dll", ".dll.compressed", ".dll.gz", ".gsp" });
        }

        [Fact]
        public void Test_ApplicationConfig_Extensions_Remove()
        {
            var config = ApplicationConfigBuilder.Create().WithoutExtensions(".dll", ".dll.compressed", ".dll.gz").BuildApplicationConfig();
            Assert.Empty(config.Extensions);
            config = ApplicationConfigBuilder.Create().WithoutExtensions().BuildApplicationConfig();
            Assert.Empty(config.Extensions);
        }
    }
}