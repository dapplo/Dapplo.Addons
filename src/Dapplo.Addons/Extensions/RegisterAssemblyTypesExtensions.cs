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
        /// This registers all the IStartupModule implementing classes for Startup
        /// This doesn't prevent the type from being registered multiple times, e.g. when it also implements other interfaces which are registered.
        /// </summary>
        /// <param name="registerAssemblyTypes">The result of _builder.RegisterAssemblyTypes</param>
        public static void EnableStartupActions(this IRegistrationBuilder<object, ScanningActivatorData, DynamicRegistrationStyle> registerAssemblyTypes)
        {
            registerAssemblyTypes
                .AssignableTo<IStartupMarker>()
                .As<IStartupMarker>()
                .SingleInstance();
        }

        /// <summary>
        /// This registers all the IShutdownModule implementing classes for Shutdown
        /// This doesn't prevent the type from being registered multiple times, e.g. when it also implements other interfaces which are registered.
        /// </summary>
        /// <param name="registerAssemblyTypes">The result of _builder.RegisterAssemblyTypes</param>
        public static void EnableShutdownActions(this IRegistrationBuilder<object, ScanningActivatorData, DynamicRegistrationStyle> registerAssemblyTypes)
        {
            registerAssemblyTypes
                .AssignableTo<IShutdownMarker>()
                .As<IShutdownMarker>()
                .SingleInstance();
        }
    }
}
