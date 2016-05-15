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

using System.Linq;
using System.Threading.Tasks;
using Dapplo.Addons.Bootstrapper;
using Dapplo.LogFacade;
using Xunit;
using Xunit.Abstractions;

#endregion

namespace Dapplo.Addons.Tests
{
	public class AddonTest
	{
		private const string ApplicationName = "Dapplo";

		public AddonTest(ITestOutputHelper testOutputHelper)
		{
			XUnitLogger.RegisterLogger(testOutputHelper, LogLevel.Verbose);
		}

		[Fact]
		public async Task TestStartupShutdown()
		{
			using (var bootstrapper = new ApplicationBootstrapper(ApplicationName))
			{
				bootstrapper.Add(".", "Dapplo.*.dll");
				// Add test project, without having a direct reference
#if DEBUG
				bootstrapper.Add(@"..\..\..\Dapplo.Addons.TestAddon\bin\Debug", "Dapplo.*.dll");
#else
				bootstrapper.Add(@"..\..\..\Dapplo.Addons.TestAddon\bin\Release", "Dapplo.*.dll");
#endif
				// Test if our test addon was loaded
				Assert.True(bootstrapper.KnownFiles.Count(addon => addon.EndsWith("TestAddon.dll")) > 0);

				// Initialize, so we can export
				Assert.True(await bootstrapper.InitializeAsync(), "Not initialized");

				// test Export, this should work before Run as some of the addons might need some stuff.

				var part = bootstrapper.Export(this);

				// Start the composition, and IStartupActions
				Assert.True(await bootstrapper.RunAsync(), "Couldn't run");

				// test import
				Assert.NotNull(bootstrapper.GetExport<AddonTest>().Value);

				// test release
				bootstrapper.Release(part);
				Assert.False(bootstrapper.GetExports<AddonTest>().Any());

				// Test localization of a test addon, with the type specified. This is possible due to Export[typeof(SomeAddon)]
				Assert.NotNull(bootstrapper.GetExport<IStartupAction>().Value);

				// Test localization of a IStartupAction with meta-data, which is exported via [StartupAction(DoNotAwait = true)]
				var lazy = bootstrapper.GetExport<IStartupAction, IStartupActionMetadata>();
				Assert.False(lazy.Metadata.AwaitStart);
			}
			// Dispose automatically calls IShutdownActions
		}
	}
}