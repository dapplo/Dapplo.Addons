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

using System.Text.RegularExpressions;
using Dapplo.Addons.Bootstrapper.Resolving;
using Dapplo.Log;
using Dapplo.Log.XUnit;
using Xunit;
using Xunit.Abstractions;

#endregion

namespace Dapplo.Addons.Tests
{
    public class FileLocationsTests
    {
        public FileLocationsTests(ITestOutputHelper testOutputHelper)
        {
            LogSettings.RegisterDefaultLogger<XUnitLogger>(LogLevels.Verbose, testOutputHelper);
        }

        [Fact]
        public void TestRoamingAppData()
        {
            var roamingAppDataDirectory = FileLocations.RoamingAppDataDirectory("Dapplo");
            Assert.EndsWith(@"AppData\Roaming\Dapplo", roamingAppDataDirectory);
        }

        [Fact]
        public void TestScan()
        {
            var startupDirectory = FileLocations.StartupDirectory;
            var files = FileLocations.Scan(new[] {startupDirectory}, "*.xml");
            Assert.Contains(files, file => file.EndsWith("Dapplo.Utils.xml"));
        }

        [Fact]
        public void TestScanFilePatternToRegex()
        {
            var startupDirectory = FileLocations.StartupDirectory;
            var regex = FileTools.FilenameToRegex("*", new[] {".xml"});
            var files = FileLocations.Scan(new[] { startupDirectory }, regex);
            Assert.Contains(files, file => file.Item1.EndsWith("Dapplo.Utils.xml"));
        }

        [Fact]
        public void TestScanRegex()
        {
            var startupDirectory = FileLocations.StartupDirectory;
            var files = FileLocations.Scan(new[] {startupDirectory}, new Regex(@".*\.xml"));
            Assert.Contains(files, file => file.Item1.EndsWith("Dapplo.Utils.xml"));
        }
    }
}