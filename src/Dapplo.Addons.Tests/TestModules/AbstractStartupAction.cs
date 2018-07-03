#region Usings

using System;
#endregion

namespace Dapplo.Addons.Tests.TestModules
{
    public class AbstractStartupAction : IStartup, IShutdown
    {
        public Action MyStartAction { get; set; }
        public Action MyStopAction { get; set; }

        public void Start() => MyStartAction?.Invoke();

        public void Shutdown() => MyStopAction?.Invoke();
    }
}