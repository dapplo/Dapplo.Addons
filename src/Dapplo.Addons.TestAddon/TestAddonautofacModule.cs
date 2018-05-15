using Autofac;

namespace Dapplo.Addons.TestAddon
{
    public class TestAddonautofacModule : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            builder
                .RegisterType<SomeAddon>()
                .As<IService>()
                .SingleInstance();
            builder
                .RegisterType<AnotherAddon>()
                .As<IService>()
                .SingleInstance();
        }
    }
}
