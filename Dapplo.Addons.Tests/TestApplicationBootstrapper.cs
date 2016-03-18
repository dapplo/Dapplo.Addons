﻿/*
	Dapplo - building blocks for desktop applications
	Copyright (C) 2015-2016 Dapplo

	For more information see: http://dapplo.net/
	Dapplo repositories are hosted on GitHub: https://github.com/dapplo

	This file is part of Dapplo.Addons

	Dapplo.Addons is free software: you can redistribute it and/or modify
	it under the terms of the GNU General Public License as published by
	the Free Software Foundation, either version 3 of the License, or
	(at your option) any later version.

	Dapplo.Addons is distributed in the hope that it will be useful,
	but WITHOUT ANY WARRANTY; without even the implied warranty of
	MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
	GNU General Public License for more details.

	You should have received a copy of the GNU General Public License
	along with Dapplo.Addons. If not, see <http://www.gnu.org/licenses/>.
 */

using Dapplo.Addons.Bootstrapper;
using System;
using Xunit;
using Xunit.Abstractions;

namespace Dapplo.Addons.Tests
{

	public class TestApplicationBootstrapper
	{

		public TestApplicationBootstrapper(ITestOutputHelper testOutputHelper)
		{
			XUnitLogger.RegisterLogger(testOutputHelper, LogFacade.LogLevel.Verbose);
		}


		[Fact]
		public void TestConstructorAndCleanup()
		{
			var bootstrapper = new ApplicationBootstrapper("Test");
			bootstrapper.Dispose();
		}

		[Fact]
		public void TestConstructorWithMutexAndCleanup()
		{
			var bootstrapper = new ApplicationBootstrapper("Test", Guid.NewGuid().ToString());
			bootstrapper.Dispose();
		}
	}
}
