//  Dapplo - building blocks for desktop applications
//  Copyright (C) 2015-2016 Dapplo
// 
//  For more information see: http://dapplo.net/
//  Dapplo repositories are hosted on GitHub: https://github.com/dapplo
// 
//  This file is part of Dapplo.Addons
// 
//  Dapplo.Addons is free software: you can redistribute it and/or modify
//  it under the terms of the GNU Lesser General Public License as published by
//  the Free Software Foundation, either version 3 of the License, or
//  (at your option) any later version.
// 
//  Dapplo.Addons is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//  GNU Lesser General Public License for more details.
// 
//  You should have a copy of the GNU Lesser General Public License
//  along with Dapplo.Addons. If not, see <http://www.gnu.org/licenses/lgpl.txt>.

#region using

using System;
using System.Threading;
using System.Threading.Tasks;
using Dapplo.Addons.Bootstrapper;
using Dapplo.LogFacade;
using Xunit;
using Xunit.Abstractions;

#endregion

namespace Dapplo.Addons.Tests
{
	public class ResourceMutexTests
	{
		private static readonly string MutexId = Guid.NewGuid().ToString();

		public ResourceMutexTests(ITestOutputHelper testOutputHelper)
		{
			XUnitLogger.RegisterLogger(testOutputHelper, LogLevel.Verbose);
		}


		[Fact]
		public void TestMutex_100()
		{
			// Test creating and cleanup 100x
			var i = 0;
			do
			{
				using (var resourceMutex = ResourceMutex.Create(MutexId, "Call" + i))
				{
					Assert.NotNull(resourceMutex);
					Assert.True(resourceMutex.IsLocked);
				}
			} while (i++ < 100);
		}

		[Fact]
		public void TestMutex_Create_Cleanup()
		{
			using (var resourceMutex = ResourceMutex.Create(MutexId, "TestMutex_Create_Cleanup"))
			{
				Assert.NotNull(resourceMutex);
				Assert.True(resourceMutex.IsLocked);
			}
		}

		[Fact]
		public void TestMutex_LockTwice()
		{
			using (var resourceMutex = ResourceMutex.Create(MutexId, "Call1"))
			{
				Assert.NotNull(resourceMutex);
				Assert.True(resourceMutex.IsLocked);
				Task.Factory.StartNew(() =>
				{
					using (var resourceMutex2 = ResourceMutex.Create(MutexId, "Call2"))
					{
						Assert.NotNull(resourceMutex2);
						Assert.False(resourceMutex2.IsLocked);
					}
				}, default(CancellationToken)).Wait();
			}
		}
	}
}