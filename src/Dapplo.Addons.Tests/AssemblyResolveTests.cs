#region Dapplo 2016 - GNU Lesser General Public License

// Dapplo - building blocks for .NET applications
// Copyright (C) 2016 Dapplo
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

using Dapplo.Addons.Tests.TestAssembly;
using Dapplo.Log;
using Dapplo.Log.XUnit;
using Dapplo.Utils;
using Dapplo.Utils.Resolving;
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
        /// 
        /// </summary>
        /// <param name="testOutputHelper"></param>
        public AssemblyResolveTests(ITestOutputHelper testOutputHelper)
        {
            LogSettings.RegisterDefaultLogger<XUnitLogger>(LogLevels.Verbose, testOutputHelper);
        }

        private static readonly LogSource Log = new LogSource();

        private void ThisForcesDelayedLoadingOfAssembly()
        {
            var helloWorld = ExternalClass.HelloWord();
            Assert.Equal(nameof(ExternalClass.HelloWord), helloWorld);
        }

        /// <summary>
        /// 
        /// </summary>
        [Fact]
        public void Test_AssemblyNameToRegex()
        {
            var fileNoMatch = @"C:\Project\Dapplo.Addons\Dapplo.Addons.Tests\bin\Debug\xunit.execution.desktop.dll";
            var fileMatch = @"C:\Project\blub\bin\Debug\Dapplo.something.dll";
            var regex = FileTools.FilenameToRegex("Dapplo.Something*", AssemblyResolver.Extensions);
            Assert.False(regex.IsMatch(fileNoMatch));
            Assert.True(regex.IsMatch(fileMatch));

            var regex2 = FileTools.FilenameToRegex("Something*", AssemblyResolver.Extensions);
            Assert.False(regex2.IsMatch(fileNoMatch));
            Assert.False(regex2.IsMatch(fileMatch));
        }
    }
}