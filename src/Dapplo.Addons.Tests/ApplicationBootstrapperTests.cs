﻿#region Dapplo 2016 - GNU Lesser General Public License

// Dapplo - building blocks for .NET applications
// Copyright (C) 2016 Dapplo
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
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Dapplo.Addons.Bootstrapper;
using Dapplo.Log.Facade;
using Dapplo.Log.XUnit;
using Xunit;
using Xunit.Abstractions;

#endregion

namespace Dapplo.Addons.Tests
{
	public class ApplicationBootstrapperTests
	{
		public ApplicationBootstrapperTests(ITestOutputHelper testOutputHelper)
		{
			LogSettings.RegisterDefaultLogger<XUnitLogger>(LogLevels.Verbose, testOutputHelper);
		}

		private const string ApplicationName = "Dapplo";

		/// <summary>
		///     Allows setting the Entry Assembly when needed.
		///     Use SetEntryAssembly() only for tests
		/// </summary>
		/// <param name="assembly">Assembly to set as entry assembly</param>
		private static void SetEntryAssembly(Assembly assembly)
		{
			if (Assembly.GetEntryAssembly() != null)
			{
				return;
			}
			var manager = new AppDomainManager();
			var entryAssemblyfield = manager.GetType().GetField("m_entryAssembly", BindingFlags.Instance | BindingFlags.NonPublic);
			if (entryAssemblyfield != null)
			{
				entryAssemblyfield.SetValue(manager, assembly);
			}

			var domain = AppDomain.CurrentDomain;
			var domainManagerField = domain.GetType().GetField("_domainManager", BindingFlags.Instance | BindingFlags.NonPublic);
			if (domainManagerField != null)
			{
				domainManagerField.SetValue(domain, manager);
			}
		}

		[Fact]
		public async Task Test_StartupException()
		{
			using (var bootstrapper = new ApplicationBootstrapper(ApplicationName))
			{
				// Add all file starting with Dapplo and ending on .dll or .dll.gz
				bootstrapper.FindAndLoadAssemblies("Dapplo*");
				// Add test project, without having a direct reference
#if DEBUG
				bootstrapper.AddScanDirectory(@"..\..\..\Dapplo.Addons.TestAddon\bin\Debug");
#else
				bootstrapper.AddScanDirectory(@"..\..\..\Dapplo.Addons.TestAddon\bin\Release");
#endif
				bootstrapper.FindAndLoadAssembly("Dapplo.Addons.TestAddon");

				// Initialize, so we can export
				Assert.True(await bootstrapper.InitializeAsync().ConfigureAwait(false), "Not initialized");

				bootstrapper.Export(true);
				// Start the composition, and IStartupActions
				await Assert.ThrowsAsync<StartupException>(async () => await bootstrapper.RunAsync().ConfigureAwait(false));
			}
			// Dispose automatically calls IShutdownActions
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
			using (var bootstrapper = new ApplicationBootstrapper("Test", Guid.NewGuid().ToString()))
			{
				Assert.True(bootstrapper.IsMutexLocked);
			}
		}

		[Fact]
		public void TestNewNullApplicationName()
		{
			Assert.Throws<ArgumentNullException>(() => new ApplicationBootstrapper(null));
		}

		[Fact]
		public async Task TestStartupShutdown()
		{
			// Especially for testing
			SetEntryAssembly(GetType().Assembly);
			using (var bootstrapper = new ApplicationBootstrapper(ApplicationName))
			{
				// Add all file starting with Dapplo and ending on .dll or .dll.gz
				bootstrapper.FindAndLoadAssemblies("Dapplo*");
				// Add test project, without having a direct reference
#if DEBUG
				bootstrapper.AddScanDirectory(@"..\..\..\Dapplo.Addons.TestAddon\bin\Debug");
#else
				bootstrapper.AddScanDirectory(@"..\..\..\Dapplo.Addons.TestAddon\bin\Release");
#endif
				bootstrapper.FindAndLoadAssembly("Dapplo.Addons.TestAddon");

				// Test if our test addon was loaded
				Assert.True(bootstrapper.KnownFiles.Count(addon => addon.EndsWith("TestAddon.dll")) > 0);

				// Initialize, so we can export
				Assert.True(await bootstrapper.InitializeAsync().ConfigureAwait(false), "Not initialized");

				// test Export, this should work before Run as some of the addons might need some stuff.

				var part = bootstrapper.Export(this);

				// Start the composition, and IStartupActions
				Assert.True(await bootstrapper.RunAsync().ConfigureAwait(false), "Couldn't run");

				// test import
				Assert.NotNull(bootstrapper.GetExport<ApplicationBootstrapperTests>().Value);

				// test release
				bootstrapper.Release(part);
				Assert.False(bootstrapper.GetExports<ApplicationBootstrapperTests>().Any());

				// Test localization of a test addon, with the type specified. This is possible due to Export[typeof(SomeAddon)]
				Assert.True(bootstrapper.GetExports<IStartupAction>().Count() == 2);

				// Test localization of a IStartupAction with meta-data, which is exported via [StartupAction(DoNotAwait = true)]
				var hasAwaitStartFalse = bootstrapper.GetExports<IStartupAction, IStartupActionMetadata>().Any(x => x.Metadata.AwaitStart == false);
				Assert.True(hasAwaitStartFalse);
			}
			// Dispose automatically calls IShutdownActions
		}
	}
}