#region Usings

using Dapplo.Addons.Tests.Entities;
#endregion

namespace Dapplo.Addons.Tests.TestModules
{
    [Service(nameof(ThirdStartupAction),nameof(FirstStartupAction))]
    public class ThirdStartupAction : AbstractStartupAction
    {
    }
}