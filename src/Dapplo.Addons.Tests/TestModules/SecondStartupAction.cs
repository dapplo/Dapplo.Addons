#region Usings

using Dapplo.Addons.Tests.Entities;

#endregion

namespace Dapplo.Addons.Tests.TestModules
{
    [Service(nameof(SecondStartupAction), nameof(ThirdStartupAction))]
    public class SecondStartupAction : AbstractStartupAction
    {
    }
}