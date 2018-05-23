#region Usings

using System;
using Dapplo.Addons.Tests.Entities;
#endregion

namespace Dapplo.Addons.Tests.TestModules
{
    [ServiceOrder(Orders.First)]
    public class FirstStartupAction : IStartup
    {
        public Action MyStartAction { get; set; }

        public void Start()
        {
            MyStartAction?.Invoke();
        }
    }
}