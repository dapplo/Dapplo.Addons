#region Dapplo 2016-2018 - GNU Lesser General Public License

// Dapplo - building blocks for .NET applications
// Copyright (C) 2016-2018 Dapplo
// 
// For more information see: http://dapplo.net/
// Dapplo repositories are hosted on GitHub: https://github.com/dapplo
// 
// This file is part of Dapplo.Addons
// 
// Dapplo.Addons is free software: you can redistribute it and/or modify
// it under the terms of the GNU Lesser General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// Dapplo.Addons is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU Lesser General Public License for more details.
// 
// You should have a copy of the GNU Lesser General Public License
// along with Dapplo.Addons. If not, see <http://www.gnu.org/licenses/lgpl.txt>.

#endregion

using Autofac;
using Dapplo.Addons.Bootstrapper.AttributeMetaData;
using Dapplo.Addons.Bootstrapper.Services;

namespace Dapplo.Addons.Bootstrapper
{
    /// <summary>
    /// Register all types for the application bootstrapper itself
    /// </summary>
    public class ApplicationBootstrapperAutofacModule : AddonModule
    {
        /// <inheritdoc />
        protected override void Load(ContainerBuilder builder)
        {;
            // Provide the startup & shutdown functionality
            builder.RegisterType<ServiceStartupShutdown>().AsSelf().IfNotRegistered(typeof(ServiceStartupShutdown)).SingleInstance();
        }
    }
}
