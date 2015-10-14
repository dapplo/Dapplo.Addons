﻿/*
 * dapplo - building blocks for desktop applications
 * Copyright (C) 2015 Robin Krom
 * 
 * For more information see: http://dapplo.net/
 * dapplo repositories are hosted on GitHub: https://github.com/dapplo
 * 
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 1 of the License, or
 * (at your option) any later version.
 * 
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 * 
 * You should have received a copy of the GNU General Public License
 * along with this program. If not, see <http://www.gnu.org/licenses/>.
 */

using Dapplo.Addons.Implementation;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;
using System.Threading.Tasks;

namespace Dapplo.Addons.Tests
{
	[TestClass]
	public class AddonTest
	{
		[TestMethod]
		public async Task TestStartup()
		{
			var bootstrapper = new SimpleBootstrapper();
			bootstrapper.Add(".", "Dapplo.*.dll");
			// Add test project, without having a direct reference
			bootstrapper.Add(@"..\..\..\Dapplo.Addons.TestAddon\bin\Debug", "Dapplo.*.dll");
			Assert.IsTrue(bootstrapper.AddonFiles.Count(addon => addon.EndsWith("TestAddon.dll")) > 0);
			bootstrapper.Run();

			// Test localization of a test addon, with the type specified. This is possible due to Export[typeof(SomeAddon)]
			Assert.IsNotNull(bootstrapper.GetExport<IStartupAction>().Value);

			// Test localization of a IStartupAction with meta-data, which is exported via [StartupAction(DoNotAwait = true)]
			var lazy = bootstrapper.GetExport<IStartupAction, IStartupActionMetadata>();
            Assert.IsTrue(lazy.Metadata.DoNotAwait);
			await lazy.Value.StartAsync();
        }
	}
}
