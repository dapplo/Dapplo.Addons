#region Usings

using Dapplo.Addons.Tests.Entities;
#endregion

namespace Dapplo.Addons.Tests.TestModules
{
    [Service(nameof(FirstStartupAction))]
    public class FirstStartupAction : AbstractStartupAction
    {
    }
}