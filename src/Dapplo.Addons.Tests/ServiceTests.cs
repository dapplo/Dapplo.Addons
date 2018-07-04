#region Dapplo 2016-2018 - GNU Lesser General Public License

// Dapplo - building blocks for .NET applications
// Copyright (C) 2016-2018 Dapplo
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

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Autofac;
using Autofac.Features.Metadata;
using Dapplo.Addons.Bootstrapper;
using Dapplo.Addons.Bootstrapper.Services;
using Dapplo.Addons.Config;
using Dapplo.Addons.Services;
using Dapplo.Addons.Tests.TestModules;
using Dapplo.Log;
using Dapplo.Log.XUnit;
using Xunit;
using Xunit.Abstractions;

namespace Dapplo.Addons.Tests
{
    public class ServiceTests
    {
        public ServiceTests(ITestOutputHelper testOutputHelper) => LogSettings.RegisterDefaultLogger<XUnitLogger>(LogLevels.Verbose, testOutputHelper);

        [Fact]
        public async Task Test_StartupShutdown()
        {
            bool didFirstStartRun = false;
            bool didSecondStartRun = false;
            bool didFirstShutdownRun = false;
            bool didSecondShutdownRun = false;
            var applicationConfig = ApplicationConfigBuilder
                .Create()
                .WithApplicationName("StartupTest")
                .WithConfigSupport()
                .WithIniSectionResolving()
                .BuildApplicationConfig();

            using (var bootstrapper = new ApplicationBootstrapper(applicationConfig))
            {
                bootstrapper.Configure();

                bootstrapper.Builder
                    .RegisterInstance(TaskScheduler.Current)
                    .Named<TaskScheduler>("test")
                    .SingleInstance();

                bootstrapper.Builder.Register(context => new FirstStartupAction
                {
                    MyStartAction = () => didFirstStartRun = true,
                    MyStopAction = () => didFirstShutdownRun = true
                }).As<IService>().SingleInstance();

                bootstrapper.Builder.Register(context => new SecondStartupAction
                {
                    MyStartAction = () => didSecondStartRun = true,
                    MyStopAction = () => didSecondShutdownRun = true
                }).As<IService>().SingleInstance();

                bootstrapper.Builder.Register(context => new FourthStartupAction
                {
                    MyStartFunc = cancellationToken => Task.Delay(10, cancellationToken),
                    MyStopFunc = cancellationToken => Task.Delay(10, cancellationToken)
                }).As<IService>().SingleInstance();
                // 
                bootstrapper.Builder.Register(context => new ThirdStartupAction()).As<IService>().SingleInstance();

                await bootstrapper.InitializeAsync();

                var serviceHandler = bootstrapper.Container.Resolve<ServiceStartupShutdown>();
                await serviceHandler.StartupAsync();

                Assert.True(didFirstStartRun);
                Assert.True(didSecondStartRun);
                await serviceHandler.ShutdownAsync();
                Assert.True(didSecondShutdownRun);
                Assert.True(didFirstShutdownRun);
            }
        }

        [Fact]
        public void Test_ServiceNodeTest()
        {
            var serviceAttributes = new List<ServiceAttribute>
            {
                new ServiceAttribute("1"),
                new ServiceAttribute("7"),
                new ServiceAttribute("2", "7"),
                new ServiceAttribute("3", "7"),
                new ServiceAttribute("5", "4"),
                new ServiceAttribute("4", "3"),
                new ServiceAttribute("6", "7")
            };

            var serviceContainer = new ServiceNodeContainer<IService>(serviceAttributes.Select(attribute => new Meta<IService, ServiceAttribute>(null, attribute)));
            // Build dictionary for lookups
            var rootNodes = serviceContainer.ServiceNodes.Values.Count(node => !node.HasPrerequisites);
            Assert.Equal(2, rootNodes);
            Assert.Equal(3, serviceContainer.ServiceNodes.Values.First(node => node.Details.Name == "7").Dependencies.Count);
        }
    }
}
