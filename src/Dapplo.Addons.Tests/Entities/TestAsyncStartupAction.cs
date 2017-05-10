using System.Threading;
using System.Threading.Tasks;

namespace Dapplo.Addons.Tests.Entities
{
    /// <summary>
    /// Used in testing the statup actions
    /// </summary>
    public class TestAsyncStartupAction : IAsyncStartupAction
    {
        private readonly int _delay;
        public TestAsyncStartupAction(int delay)
        {
            _delay = delay;
        }
        /// <inheritdoc />
        public async Task StartAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            await Task.Delay(_delay, cancellationToken);
        }
    }
}
