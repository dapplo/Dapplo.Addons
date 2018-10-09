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
        public async Task Test_ServiceStartupShutdownOrder()
        {
            var applicationConfig = ApplicationConfigBuilder
                .Create()
                .WithApplicationName("StartupTest")
                .BuildApplicationConfig();

            using (var bootstrapper = new ApplicationBootstrapper(applicationConfig))
            {
                bootstrapper.Configure();

                bootstrapper.Builder
                    .RegisterInstance(TaskScheduler.Current)
                    .Named<TaskScheduler>("test")
                    .SingleInstance();

                bootstrapper.Builder
                    .RegisterType<OrderProvider>()
                    .AsSelf()
                    .SingleInstance();

                bootstrapper.Builder
                    .RegisterType<ParentService>()
                    .As<IService>()
                    .SingleInstance();

                bootstrapper.Builder
                    .RegisterType<ServiceOne>()
                    .AsSelf()
                    .As<IService>()
                    .SingleInstance();

                bootstrapper.Builder
                    .RegisterType<ServiceTwoA>()
                    .AsSelf()
                    .As<IService>()
                    .SingleInstance();

                bootstrapper.Builder
                    .RegisterType<ServiceTwoB>()
                    .AsSelf()
                    .As<IService>()
                    .SingleInstance();

                bootstrapper.Builder
                    .RegisterType<ServiceThree>()
                    .AsSelf()
                    .As<IService>()
                    .SingleInstance();

                bootstrapper.Builder
                    .RegisterType<ServiceFour>()
                    .AsSelf()
                    .As<IService>()
                    .SingleInstance();

                await bootstrapper.InitializeAsync();

                var serviceHandler = bootstrapper.Container.Resolve<ServiceStartupShutdown>();

                await serviceHandler.StartupAsync();

                var serviceOne = bootstrapper.Container.Resolve<ServiceOne>();
                var serviceTwoA = bootstrapper.Container.Resolve<ServiceTwoA>();
                var serviceTwoB = bootstrapper.Container.Resolve<ServiceTwoB>();
                var serviceThree = bootstrapper.Container.Resolve<ServiceThree>();
                var serviceFour = bootstrapper.Container.Resolve<ServiceFour>();

                Assert.True(serviceOne.DidStartup);
                Assert.True(serviceTwoA.DidStartup);
                Assert.True(serviceTwoB.DidStartup);
                Assert.True(serviceThree.DidStartup);
                Assert.True(serviceFour.DidStartup);
                Assert.Equal(1, serviceOne.StartupOrder);
                Assert.True(serviceTwoA.StartupOrder == 2 || serviceTwoA.StartupOrder == 3, $"Value should be 2 or 3, but is {serviceTwoA.StartupOrder}");
                Assert.True(serviceTwoB.StartupOrder == 2 || serviceTwoB.StartupOrder == 3, $"Value should be 2 or 3, but is {serviceTwoB.StartupOrder}");
                Assert.Equal(4, serviceThree.StartupOrder);
                Assert.Equal(5, serviceFour.StartupOrder);

                await serviceHandler.ShutdownAsync();

                Assert.True(serviceOne.DidShutdown);
                Assert.True(serviceTwoA.DidShutdown);
                Assert.True(serviceTwoB.DidShutdown);
                Assert.True(serviceThree.DidShutdown);
                Assert.True(serviceFour.DidShutdown);
                Assert.Equal(1, serviceFour.ShutdownOrder);
                Assert.Equal(2, serviceThree.ShutdownOrder);
                Assert.True(serviceTwoA.ShutdownOrder == 3 || serviceTwoA.ShutdownOrder == 4, $"Value should be 3 or 4, but is {serviceTwoA.ShutdownOrder}");
                Assert.True(serviceTwoB.ShutdownOrder == 3 || serviceTwoB.ShutdownOrder == 4, $"Value should be 3 or 4, but is {serviceTwoB.ShutdownOrder}");
                Assert.Equal(5, serviceOne.ShutdownOrder);
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
