using Autofac;
using Dapplo.Addons.TestAddon.Config;

namespace Dapplo.Addons.TestAddon
{
    public class TestAddonModule : AddonModule
    {
        protected override void Load(ContainerBuilder builder)
        {
            builder
                .RegisterType<ThisIsConfiguration>()
                .As<IThisIsConfiguration>()
                .As<IThisIsSubConfiguration>()
                .SingleInstance();

            builder
                .RegisterType<SomeAddon>()
                .WithMetadata("Name", nameof(SomeAddon))
                .As<IService>()
                .SingleInstance();
            builder
                .RegisterType<AnotherAddon>()
                .WithMetadata("Name", nameof(AnotherAddon))
                .As<IService>()
                .SingleInstance();
        }
    }
}
