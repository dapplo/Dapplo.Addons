#region Usings

using System;

#endregion

namespace Dapplo.Addons.Tests.TestModules
{
    [ServiceOrder(2000)]
    public class SecondStartupAction : IStartup
    {
        public Action MyStartAction { get; set; }

        public void Start()
        {
            MyStartAction?.Invoke();
        }
    }
}