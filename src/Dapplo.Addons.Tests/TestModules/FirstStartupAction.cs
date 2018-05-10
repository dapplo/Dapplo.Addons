#region Usings

using System;

#endregion

namespace Dapplo.Addons.Tests.TestModules
{
    [StartupOrder(1000)]
    public class FirstStartupAction : IStartup
    {
        public Action MyStartAction { get; set; }

        public void Start()
        {
            MyStartAction?.Invoke();
        }
    }
}