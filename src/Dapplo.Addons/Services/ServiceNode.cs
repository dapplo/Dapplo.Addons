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

using System.Collections.Generic;

namespace Dapplo.Addons.Services
{
    /// <summary>
    /// This contains the information needed for the startup and shutdown of services
    /// </summary>
    public class ServiceNode<TService>
    {
        private bool _isShutdownStarted;

        /// <summary>
        /// The attributed details
        /// </summary>
        public ServiceAttribute Details { get; set; }

        /// <summary>
        /// Used to define if the Shutdown was already started
        /// </summary>
        public bool StartShutdown()
        {
            lock (this)
            {
                if (_isShutdownStarted)
                {
                    return false;
                }

                _isShutdownStarted = true;
                return true;
            }
        }

        /// <summary>
        /// Task of the service
        /// </summary>
        public TService Service { get; set; }

        /// <summary>
        /// Test if this service depends on other services
        /// </summary>
        public bool HasPrerequisites => Prerequisites.Count > 0;

        /// <summary>
        /// The service which should be started before this
        /// </summary>
        public IList<ServiceNode<TService>> Prerequisites { get; } = new List<ServiceNode<TService>>();

        /// <summary>
        /// Test if this service has dependencies
        /// </summary>
        public bool HasDependencies => Dependencies.Count > 0;

        /// <summary>
        /// The services awaiting for this
        /// </summary>
        public IList<ServiceNode<TService>> Dependencies { get; } = new List<ServiceNode<TService>>();
    }
}
