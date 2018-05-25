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
using System.IO.Packaging;
using System.Linq;
using Dapplo.Addons.Bootstrapper.Extensions;
using Dapplo.Addons.Bootstrapper.Resolving;
using Dapplo.Log;
using Dapplo.Log.XUnit;
using Xunit;
using Xunit.Abstractions;

#endregion

namespace Dapplo.Addons.Tests
{
    public class PackUriTests
    {
        public PackUriTests(ITestOutputHelper testOutputHelper)
        {
            LogSettings.RegisterDefaultLogger<XUnitLogger>(LogLevels.Verbose, testOutputHelper);
        }
        
        [Fact]
        public void Test_PackUri_Validation()
        {
            var packUri = new Uri($@"{PackUriHelper.UriSchemePack}://application:,,,/Dapplo.Addons.Tests;component/TestFiles/embedded-dapplo.png");
            Assert.True(packUri.IsWellformedApplicationPackUri());
        }

        [Fact]
        public void Test_PackUri_GetResource()
        {
            var packUri = new Uri($@"{PackUriHelper.UriSchemePack}://application:,,,/Dapplo.Addons.Tests;component/TestFiles/embedded-dapplo.png");
            var resources = new ManifestResources(s => AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(assembly => assembly.GetName().Name == s));
            using (var stream = resources.ResourceAsStream(packUri))
            {
                Assert.NotNull(stream);
                Assert.True(stream.Length > 0);
            }
        }

        [Fact]
        public void Test_PackUri_EmbeddedResourceExists()
        {
            var packUri = new Uri($@"{PackUriHelper.UriSchemePack}://application:,,,/Dapplo.Addons.Tests;component/TestFiles/embedded-dapplo.png");
            var resources = new ManifestResources(s => AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(assembly => assembly.GetName().Name == s));
            Assert.True(resources.EmbeddedResourceExists(packUri));
        }
    }
}