#region Usings

using System;
using System.ComponentModel.Composition;

#endregion

namespace Dapplo.Addons.Tests.TestModules
{
    [StartupAction(StartupOrder = 1000)]
    public class FirstStartupAction : IStartupAction
    {
        [Import("FirstAction", AllowDefault = true)]
        private Action MyStartAction { get; set; }

        public void Start()
        {
            MyStartAction?.Invoke();
        }
    }
}