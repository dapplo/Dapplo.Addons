#region Usings

using System;
using Dapplo.Addons.Tests.Entities;

#endregion

namespace Dapplo.Addons.Tests.TestModules
{
    [ServiceOrder(Orders.Second)]
    public class SecondStartupAction : IStartup
    {
        public Action MyStartAction { get; set; }

        public void Start()
        {
            MyStartAction?.Invoke();
        }
    }
}