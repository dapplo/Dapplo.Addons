using Autofac;

namespace Dapplo.Addons.TestAddon
{
    public class TestAddonModule : AddonModule
    {
        protected override void Load(ContainerBuilder builder)
        {
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
