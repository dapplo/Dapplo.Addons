using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Dapplo.Addons.Bootstrapper;

namespace Dapplo.Addons.Tests
{
	[TestClass]
	public class TestApplicationBootstrapper
	{
		[TestMethod]
		public void TestConstructorAndCleanup()
		{
			var bootstrapper = new ApplicationBootstrapper("Test");
			bootstrapper.Dispose();
		}

		[TestMethod]
		public void TestConstructorWithMutexAndCleanup()
		{
			var bootstrapper = new ApplicationBootstrapper("Test", Guid.NewGuid().ToString());
			bootstrapper.Dispose();
		}
	}
}
