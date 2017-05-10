using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Dapplo.Addons.Tests.Entities;
using Xunit;

namespace Dapplo.Addons.Tests
{
    public class StartupTests
    {
        [Fact]
        public async Task Test_Startup_Grouping()
        {
            IList<Lazy<IStartupModule, IStartupMetadata>> startupModules = new List<Lazy<IStartupModule, IStartupMetadata>>();

            var randomCreator = new Random();
            startupModules.Add(new Lazy<IStartupModule, IStartupMetadata>(
                () => new TestAsyncStartupAction(randomCreator.Next(100, 1000)),
                new StartupActionAttribute {AwaitStart = true, StartupOrder = 10})
                );

            var cancellationToken = default(CancellationToken);
            var startupGroups = startupModules.GroupBy(lazy => lazy.Metadata.StartupOrder).OrderBy(lazies => lazies.Key);
            foreach (var startupGroup in startupGroups)
            {
                var tasks = startupGroup.Select(lazy =>
                {
                    var startupAction = lazy.Value as IStartupAction;

                    return startupAction != null
                        ? Task.Run(() => startupAction.Start(), cancellationToken)
                        : (lazy.Value as IAsyncStartupAction)?.StartAsync(cancellationToken);
                });
            }
        }
    }
}
