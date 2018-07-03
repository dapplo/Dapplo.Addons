using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Autofac;
using Autofac.Features.Metadata;
using Dapplo.Addons.Bootstrapper;
using Dapplo.Addons.Bootstrapper.Handler;
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
                .BuildApplicationConfig();

            using (var bootstrapper = new ApplicationBootstrapper(applicationConfig))
            {
                bootstrapper.Configure();

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

                // 
                bootstrapper.Builder.Register(context => new ThirdStartupAction()).As<IService>().SingleInstance();

                await bootstrapper.InitializeAsync();

                var serviceHandler = bootstrapper.Container.Resolve<ServiceHandler>();
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

            // Build dictionary for lookups
            var serviceDictionary = ServiceHandler.CreateServiceDictionary(serviceAttributes.Select(attribute => new Meta<IService, ServiceAttribute>(null, attribute)));
            var rootNodes = serviceDictionary.Values.Count(node => !node.IsDependendOn);
            Assert.Equal(2, rootNodes);
            Assert.Equal(3, serviceDictionary.Values.First(node => node.Details.Name == "7").Dependencies.Count);
        }
    }
}
