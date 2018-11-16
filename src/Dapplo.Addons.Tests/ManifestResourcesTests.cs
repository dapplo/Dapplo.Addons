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

using System;
using System.Drawing;
using System.IO.Packaging;
using System.Linq;
using System.Reflection;
using Dapplo.Addons.Bootstrapper;
using Dapplo.Addons.Bootstrapper.Resolving;
using Dapplo.Addons.Tests.TestFiles;
using Dapplo.Log;
using Dapplo.Log.XUnit;
using Xunit;
using Xunit.Abstractions;

#endregion

namespace Dapplo.Addons.Tests
{
    public class ManifestResourcesTests
    {
        private readonly AssemblyResolver _resolver = new AssemblyResolver(ApplicationConfigBuilder.Create().BuildApplicationConfig());

        public ManifestResourcesTests(ITestOutputHelper testOutputHelper)
        {
            LogSettings.RegisterDefaultLogger<XUnitLogger>(LogLevels.Verbose, testOutputHelper);

            // Add the pack scheme
            // ReSharper disable once UnusedVariable
            var packScheme = PackUriHelper.UriSchemePack;
        }

        /// <summary>
        ///     Test if resources can be found
        /// </summary>
        [Fact]
        public void Test_FindEmbeddedResources()
        {
            var resource = _resolver.Resources.Find(typeof(Dummy), @"embedded-dapplo.png");
            Assert.NotNull(resource);
            resource = _resolver.Resources.Find(GetType(), "TestFiles", @"embedded-dapplo.png");
            Assert.NotNull(resource);

            var resources = _resolver.Resources.FindEmbeddedResources(GetType().Assembly, @"dapplo.png");
            Assert.True(resources.Any());
        }

        /// <summary>
        ///     Test if finding and loading from the manifest works
        /// </summary>
        [Fact]
        public void Test_LocateResourceAsStream()
        {
            using (var stream = _resolver.Resources.LocateResourceAsStream(GetType().Assembly, @"TestFiles\embedded-dapplo.png"))
            {
                var bitmap = Image.FromStream(stream);
                Assert.NotNull(bitmap);
                Assert.True(bitmap.Width > 0);
            }
        }

        /// <summary>
        ///     Test if gunzip works
        /// </summary>
        [Fact]
        public void Test_ResourceAsStream_GZ()
        {
            using (var stream = _resolver.Resources.ResourceAsStream(GetType().Assembly, @"TestFiles\embedded-dapplo.png.gz"))
            {
                using (var bitmap = Image.FromStream(stream))
                {
                    Assert.NotNull(bitmap);
                    Assert.True(bitmap.Width > 0);
                }
            }
        }

        /// <summary>
        ///     Test if gunzip works
        /// </summary>
        [Fact]
        public void Test_LocateResourceAsStream_GZ()
        {
            using (var stream = _resolver.Resources.LocateResourceAsStream(GetType().Assembly, @"TestFiles\embedded-dapplo.png.gz"))
            {
                using (var bitmap = Image.FromStream(stream))
                {
                    Assert.NotNull(bitmap);
                    Assert.True(bitmap.Width > 0);
                }
            }
        }

        /// <summary>
        ///     Test if finding and loading from the manifest via pack uris work
        /// </summary>
        [Fact]
        public void Test_PackUri()
        {
            var packUri = new Uri("pack://application:,,,/Dapplo.Addons.Tests;component/TestFiles/embedded-dapplo.png", UriKind.RelativeOrAbsolute);

            Assert.True(_resolver.Resources.EmbeddedResourceExists(packUri));

            using (var stream = _resolver.Resources.ResourceAsStream(packUri))
            {
                using (var bitmap = Image.FromStream(stream))
                {
                    Assert.NotNull(bitmap);
                    Assert.True(bitmap.Width > 0);
                }
            }
        }


        /// <summary>
        ///     Test if finding and loading from the manifest via pack uris work
        /// </summary>
        [Fact]
        public void Test_Resources()
        {
            var assembly = Assembly.Load("MahApps.Metro");
            var packUri = new Uri($@"{PackUriHelper.UriSchemePack}://application:,,,/MahApps.Metro;component/Styles/Accents/Yellow.xaml", UriKind.RelativeOrAbsolute);

            Assert.True(_resolver.Resources.EmbeddedResourceExists(packUri));
        }
    }
}