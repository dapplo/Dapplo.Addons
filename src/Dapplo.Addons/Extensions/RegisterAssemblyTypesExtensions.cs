#region Dapplo 2016-2018 - GNU Lesser General Public License

// Dapplo - building blocks for .NET applications
// Copyright (C) 2016-2018 Dapplo
// 
// For more information see: http://dapplo.net/
// Dapplo repositories are hosted on GitHub: https://github.com/dapplo
// 
// This file is part of Dapplo.CaliburnMicro
// 
// Dapplo.CaliburnMicro is free software: you can redistribute it and/or modify
// it under the terms of the GNU Lesser General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// Dapplo.CaliburnMicro is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU Lesser General Public License for more details.
// 
// You should have a copy of the GNU Lesser General Public License
// along with Dapplo.CaliburnMicro. If not, see <http://www.gnu.org/licenses/lgpl.txt>.

#endregion

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
