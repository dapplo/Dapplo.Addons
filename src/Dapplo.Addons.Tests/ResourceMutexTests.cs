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
using System.Threading;
using System.Threading.Tasks;
using Dapplo.Addons.Bootstrapper;
using Dapplo.Log;
using Dapplo.Log.XUnit;
using Xunit;
using Xunit.Abstractions;

#endregion

namespace Dapplo.Addons.Tests
{
    public class ResourceMutexTests
    {
        private static readonly LogSource Log = new LogSource();

        public ResourceMutexTests(ITestOutputHelper testOutputHelper)
        {
            LogSettings.RegisterDefaultLogger<XUnitLogger>(LogLevels.Verbose, testOutputHelper);
        }

        [Fact]
        public void TestMutex_100()
        {
            var mutexId = Guid.NewGuid().ToString();
            // Test creating and cleanup 100x
            var i = 0;
            do
            {
                using (var resourceMutex = ResourceMutex.Create(mutexId, "Call" + i))
                {
                    Assert.NotNull(resourceMutex);
                    Assert.True(resourceMutex.IsLocked);
                }
            } while (i++ < 100);
        }

        [Fact]
        public void TestMutex_Create_Cleanup()
        {
            var mutexId = Guid.NewGuid().ToString();
            using (var resourceMutex = ResourceMutex.Create(mutexId, "TestMutex_Create_Cleanup"))
            {
                Assert.NotNull(resourceMutex);
                Assert.True(resourceMutex.IsLocked);
            }
        }

        [Fact]
        public void TestMutex_Finalizer()
        {
            var mutexId = Guid.NewGuid().ToString();
            var resourceMutex = ResourceMutex.Create(mutexId, "TestMutex_Finalizer");
            Assert.NotNull(resourceMutex);
            Assert.True(resourceMutex.IsLocked);
        }

        [Fact]
        public void TestMutex_LockTwice()
        {
            var mutexId = Guid.NewGuid().ToString();
            using (var resourceMutex = ResourceMutex.Create(mutexId, "FirstCall"))
            {
                Assert.NotNull(resourceMutex);
                Assert.True(resourceMutex.IsLocked);
                Task.Factory.StartNew(() =>
                {
                    using (var resourceMutex2 = ResourceMutex.Create(mutexId, "SecondCall"))
                    {
                        Assert.NotNull(resourceMutex2);
                        Assert.False(resourceMutex2.IsLocked);
                    }
                }, default(CancellationToken)).Wait();
                Log.Info().WriteLine("Finished task");
            }
        }
    }
}