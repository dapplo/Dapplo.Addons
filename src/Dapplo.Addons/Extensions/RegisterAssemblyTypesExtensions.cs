using Autofac;
using Autofac.Builder;
using Autofac.Features.Scanning;

namespace Dapplo.Addons.Extensions
{
    /// <summary>
    /// Helper extensions for basic logic
    /// </summary>
    public static class RegisterAssemblyTypesExtensions
    {
        /// <summary>
        /// This registers all the IStartable, an internal autofac feature, implementing classes for Startup
        /// This doesn't prevent the type from being registered multiple times, e.g. when it also implements other interfaces which are registered.
        /// </summary>
        /// <param name="registerAssemblyTypes">The result of _builder.RegisterAssemblyTypes</param>
        public static void EnableStartables(this IRegistrationBuilder<object, ScanningActivatorData, DynamicRegistrationStyle> registerAssemblyTypes)
        {
            registerAssemblyTypes
                .AssignableTo<IStartable>()
                .As<IStartable>()
                .SingleInstance();
        }

        /// <summary>
        /// This registers all the IStartup and IStartupAsync implementing classes for Startup
        /// This doesn't prevent the type from being registered multiple times, e.g. when it also implements other interfaces which are registered.
        /// </summary>
        /// <param name="registerAssemblyTypes">The result of _builder.RegisterAssemblyTypes</param>
        public static void EnableStartup(this IRegistrationBuilder<object, ScanningActivatorData, DynamicRegistrationStyle> registerAssemblyTypes)
        {
            registerAssemblyTypes
                .AssignableTo<IStartupMarker>()
                .As<IStartupMarker>()
                .SingleInstance();
        }

        /// <summary>
        /// This registers all the IShutdown and IShtudownAsync implementing classes for Shutdown
        /// This doesn't prevent the type from being registered multiple times, e.g. when it also implements other interfaces which are registered.
        /// </summary>
        /// <param name="registerAssemblyTypes">The result of _builder.RegisterAssemblyTypes</param>
        public static void EnableShutdown(this IRegistrationBuilder<object, ScanningActivatorData, DynamicRegistrationStyle> registerAssemblyTypes)
        {
            registerAssemblyTypes
                .AssignableTo<IShutdownMarker>()
                .As<IShutdownMarker>()
                .SingleInstance();
        }
    }
}
