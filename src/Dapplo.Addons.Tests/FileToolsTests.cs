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

using System.Text.RegularExpressions;
using Dapplo.Addons.Bootstrapper.Resolving;
using Dapplo.Log;
using Dapplo.Log.XUnit;
using Xunit;
using Xunit.Abstractions;

#endregion

namespace Dapplo.Addons.Tests
{
    public class FileToolsTests
    {
        public FileToolsTests(ITestOutputHelper testOutputHelper)
        {
            LogSettings.RegisterDefaultLogger<XUnitLogger>(LogLevels.Verbose, testOutputHelper);
        }
        
        [Fact]
        public void TestFilenameToRegex_OneExtension()
        {
            var regex = FileTools.FilenameToRegex("*", new[] {".xml"});
            Assert.Equal(@"^(.*\\)*[^\\]*\.xml$", regex.ToString());
        }

        [Fact]
        public void TestFilenameToRegex_MultipleExtensions()
        {
            var regex = FileTools.FilenameToRegex("*", new[] { ".xml" , ".txt"});
            Assert.Equal(@"^(.*\\)*[^\\]*(\.xml|\.txt)$", regex.ToString());
        }

        [Fact]
        public void TestFilenameToRegex_ExtensionInPattern()
        {
            var regex = FileTools.FilenameToRegex("*.xml");
            Assert.Equal(@"^(.*\\)*[^\\]*\.xml$", regex.ToString());
        }
    }
}