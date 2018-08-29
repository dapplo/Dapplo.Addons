using System.Threading;

namespace Dapplo.Addons.Tests.TestModules
{
    /// <summary>
    /// A simple tool to be able to test the startup order
    /// </summary>
    public class OrderProvider
    {
        private int _startupOrder;
        private int _shutdownOrder;
        public int TakeStartupNumber() => Interlocked.Add(ref _startupOrder, 1);
        public int TakeShutdownNumber() => Interlocked.Add(ref _shutdownOrder, 1);
    }
}
