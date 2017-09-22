#region Usings

using System;
using System.ComponentModel.Composition;

#endregion

namespace Dapplo.Addons.Tests.TestModules
{
    [StartupAction(StartupOrder = 2000)]
    public class SecondStartupAction : IStartupAction
    {
        [Import("SecondAction", AllowDefault = true)]
        private Action MyStartAction { get; set; }

        public void Start()
        {
            MyStartAction?.Invoke();
        }
    }
}