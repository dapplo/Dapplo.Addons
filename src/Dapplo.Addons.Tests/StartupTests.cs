#region Usings

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Autofac;
using Dapplo.Addons.Bootstrapper;
using Dapplo.Addons.Bootstrapper.Handler;
using Dapplo.Addons.Tests.Entities;
using Dapplo.Addons.Tests.TestModules;
using Xunit;

#endregion

namespace Dapplo.Addons.Tests
{
    public class StartupTests
    {
        [Fact]
        public void Test_Startup_Grouping()
        {
            IList<Lazy<IStartupMarker, StartupOrderAttribute>> startupModules = new List<Lazy<IStartupMarker, StartupOrderAttribute>>();

            var randomCreator = new Random();
            startupModules.Add(new Lazy<IStartupMarker, StartupOrderAttribute>(
                () => new TestAsyncStartupAction(randomCreator.Next(100, 1000)),
                new StartupOrderAttribute { AwaitStart = true, StartupOrder = 10})
            );

            var cancellationToken = default(CancellationToken);
            var startupGroups = startupModules.GroupBy(lazy => lazy.Metadata.StartupOrder).OrderBy(lazies => lazies.Key);
            foreach (var startupGroup in startupGroups)
            {
                var tasks = startupGroup.Select(lazy =>
                {
                    return lazy.Value is IStartup startupAction
                        ? Task.Run(() => startupAction.Start(), cancellationToken)
                        : (lazy.Value as IStartupAsync)?.StartAsync(cancellationToken);
                });
            }
        }

        [Fact]
        public async Task Test_Startup()
        {
            bool didFirstRun = false;
            bool didSecondRun = false;
            using (var bootstrapper = new ApplicationBootstrapper("StartupTest"))
            {
                bootstrapper.Configure();

                bootstrapper.Builder.RegisterType<StartupHandler>();

                bootstrapper.Builder.Register(context => new FirstStartupAction
                {
                    MyStartAction = () => didFirstRun = true
                }).As<IStartupMarker>().SingleInstance();

                bootstrapper.Builder.Register(context => new SecondStartupAction
                {
                    MyStartAction = () => didSecondRun = true
                }).As<IStartupMarker>().SingleInstance();

                await bootstrapper.InitializeAsync();
                await bootstrapper.StartupAsync();
                Assert.True(didFirstRun);
                Assert.True(didSecondRun);
            }
        }
    }
}