using Dapplo.Config.Ini;

namespace Dapplo.Addons.TestAddon.Config
{
    public class ThisIsConfiguration : IniSectionBase<IThisIsConfiguration>, IThisIsConfiguration
    {
        public string Name { get; set; }
        public string Company { get; set; }
    }
}
