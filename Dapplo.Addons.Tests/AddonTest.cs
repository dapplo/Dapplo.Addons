/*
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

using Dapplo.Addons.Implementation;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;
using System.Threading.Tasks;
using Dapplo.Config.Ini;
using Dapplo.LogFacade;
using Dapplo.LogFacade.Loggers;

namespace Dapplo.Addons.Tests
{
	[TestClass]
	public class AddonTest
	{
		private const string ApplicationName = "Dapplo";

		[TestInitialize]
		public void Initialize()
		{
			LogSettings.Logger = new TraceLogger {Level = LogLevel.Verbose};
		}

		[TestMethod]
		public async Task TestStartupShutdown()
		{
			var bootstrapper = new ApplicationBootstrapper(ApplicationName);

			var iniConfig = new IniConfig(ApplicationName, "test");
			bootstrapper.IniConfigForExport = iniConfig;

			bootstrapper.Add(".", "Dapplo.*.dll");
			// Add test project, without having a direct reference
#if DEBUG
			bootstrapper.Add(@"..\..\..\Dapplo.Addons.TestAddon\bin\Debug", "Dapplo.*.dll");
#else
			bootstrapper.Add(@"..\..\..\Dapplo.Addons.TestAddon\bin\Release", "Dapplo.*.dll");
#endif
			// Test if our test addon was loaded
			Assert.IsTrue(bootstrapper.AddonFiles.Count(addon => addon.EndsWith("TestAddon.dll")) > 0);

			// Initialize, so we can export
			bootstrapper.Initialize();

			// Start the composition
			bootstrapper.Run();

			// test Export
			var part = bootstrapper.Export(this);

			// test import
			Assert.IsNotNull(bootstrapper.GetExport<AddonTest>().Value);

			// test release
			bootstrapper.Release(part);
            Assert.IsFalse(bootstrapper.GetExports<AddonTest>().Any());

			// Test localization of a test addon, with the type specified. This is possible due to Export[typeof(SomeAddon)]
			Assert.IsNotNull(bootstrapper.GetExport<IStartupAction>().Value);

			// Test localization of a IStartupAction with meta-data, which is exported via [StartupAction(DoNotAwait = true)]
			var lazy = bootstrapper.GetExport<IStartupAction, IStartupActionMetadata>();
            Assert.IsFalse(lazy.Metadata.DoNotAwait);

			// Test startup
			await bootstrapper.StartupAsync();

			// Test shutdown
			await bootstrapper.ShutdownAsync();
		}
	}
}
