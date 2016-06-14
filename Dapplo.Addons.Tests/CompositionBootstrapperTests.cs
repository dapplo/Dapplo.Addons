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

using Dapplo.Addons.Bootstrapper;
using Dapplo.Log.XUnit;
using Dapplo.LogFacade;
using System;
using System.ComponentModel.Composition.Hosting;
using System.Reflection;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

#endregion

namespace Dapplo.Addons.Tests
{
	public class CompositionBootstrapperTests
	{
		private const string ApplicationName = "Dapplo";

		public CompositionBootstrapperTests(ITestOutputHelper testOutputHelper)
		{
			XUnitLogger.RegisterLogger(testOutputHelper, LogLevels.Verbose);
		}

		[Fact]
		public void TestNotInitialized()
		{
			// SimpleBootstrapper extends CompositionBootstrapper
			var cb = new SimpleBootstrapper();
			Assert.Throws<InvalidOperationException>(() => cb.Export("Hello"));
			Assert.Throws<InvalidOperationException>(() => cb.GetExport<string>());
			Assert.Throws<InvalidOperationException>(() => cb.GetExport<string, IStartupActionMetadata>());
			Assert.Throws<InvalidOperationException>(() => cb.GetExport(typeof(string)));
			Assert.Throws<InvalidOperationException>(() => cb.GetService(typeof(string)));
			Assert.Throws<InvalidOperationException>(() => cb.GetExports(typeof(string)));
			Assert.Throws<InvalidOperationException>(() => cb.GetExports<string>());
			Assert.Throws<InvalidOperationException>(() => cb.GetExports<string, IStartupActionMetadata>());
			Assert.Throws<InvalidOperationException>(() => cb.Release(null));
			Assert.Throws<InvalidOperationException>(() => cb.FillImports(null));
			cb.Dispose();
		}

		[Fact]
		public async Task TestArgumentNull()
		{
			// SimpleBootstrapper extends CompositionBootstrapper
			var cb = new SimpleBootstrapper();
			Assert.Throws<ArgumentNullException>(() => cb.Add((Assembly)null));
			Assert.Throws<ArgumentNullException>(() => cb.Add((AssemblyCatalog)null));
			Assert.Throws<ArgumentNullException>(() => cb.Add((string)null));
			Assert.Throws<ArgumentNullException>(() => cb.Add((Type)null));
			Assert.Throws<ArgumentNullException>(() => cb.Add((ExportProvider)null));
			await cb.InitializeAsync().ConfigureAwait(false);
			Assert.Throws<ArgumentNullException>(() => cb.Export<string>(null));
			Assert.Throws<ArgumentNullException>(() => cb.Release(null));
			Assert.Throws<ArgumentNullException>(() => cb.FillImports(null));
			cb.Dispose();
		}
	}
}