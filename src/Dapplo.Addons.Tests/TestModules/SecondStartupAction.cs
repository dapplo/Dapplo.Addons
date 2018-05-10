#region Usings

using System;
using System.ComponentModel.Composition;

#endregion

namespace Dapplo.Addons.Tests.TestModules
{
    [StartupOrder(2000)]
    public class SecondStartupAction : IStartup
    {
        public Action MyStartAction { get; set; }

        public void Start()
        {
            MyStartAction?.Invoke();
        }
    }
}