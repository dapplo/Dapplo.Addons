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
using Autofac.Core;
using Dapplo.Log;

namespace Dapplo.Addons.Bootstrapper
{
    /// <summary>
    /// A module to enable logs
    /// </summary>
    public class LogRequestModule : Module
    {
        private static readonly LogSource Log = new LogSource();
        private int _depth;

        /// <summary>
        /// Implement the AttachToComponentRegistration, to register logging
        /// </summary>
        /// <param name="componentRegistry">IComponentRegistry</param>
        /// <param name="registration">IComponentRegistration</param>
        protected override void AttachToComponentRegistration(IComponentRegistry componentRegistry, IComponentRegistration registration)
        {
            registration.Preparing += RegistrationOnPreparing;
            registration.Activating += RegistrationOnActivating;
            registration.Activated += RegistrationOnActivated;
            base.AttachToComponentRegistration(componentRegistry, registration);
        }

        private void RegistrationOnActivated(object sender, IActivatedEventArgs<object> e)
        {
            Log.Debug().WriteLine("{0}Activated {1}", GetPrefix(), e.Component.Activator.LimitType);
        }

        private string GetPrefix()
        {
            return new string('-', _depth * 2);
        }

        private void RegistrationOnPreparing(object sender, PreparingEventArgs preparingEventArgs)
        {
            Log.Debug().WriteLine("{0}Resolving  {1}", GetPrefix(), preparingEventArgs.Component.Activator.LimitType);
            _depth++;
        }

        private void RegistrationOnActivating(object sender, ActivatingEventArgs<object> activatingEventArgs)
        {
            _depth--;
            Log.Debug().WriteLine("{0}Activating {1}", GetPrefix(), activatingEventArgs.Component.Activator.LimitType);
        }
    }
}
