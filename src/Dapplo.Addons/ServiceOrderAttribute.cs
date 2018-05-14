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

#region Usings

using System;
using System.ComponentModel;

#endregion

namespace Dapplo.Addons
{
    /// <summary>
    ///     This attribute can be used to specify the startup and shutdown order of services
    /// </summary>
    [System.ComponentModel.Composition.MetadataAttribute]
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public class ServiceOrderAttribute : Attribute
    {
        /// <summary>
        /// Default service startup and shutdown order
        /// </summary>
        public ServiceOrderAttribute()
        {

        }

        /// <summary>
        /// Startup order
        /// </summary>
        /// <param name="startupOrder">int</param>
        public ServiceOrderAttribute(int startupOrder)
        {
            StartupOrder = startupOrder;
        }

        /// <summary>
        /// Startup and shutdown order
        /// </summary>
        /// <param name="startupOrder">int</param>
        /// <param name="shutdownOrder">int</param>
        public ServiceOrderAttribute(int startupOrder, int shutdownOrder)
        {
            StartupOrder = startupOrder;
            ShutdownOrder = shutdownOrder;
        }

        /// <summary>
        ///     Here the order of the startup action can be specified, starting with low values and ending with high.
        ///     With this a cheap form of "dependency" management is made.
        /// </summary>
        [DefaultValue(1)]
        public int StartupOrder { get; set; } = 1;

        /// <summary>
        ///     Here the order of the service shutdown can be specified, starting with high values and ending with low.
        ///     With this a cheap form of "dependency" management is made.
        /// </summary>
        [DefaultValue(1)]
        public int ShutdownOrder { get; set; } = 1;

        /// <summary>
        ///     Specify if the startup needs to be awaited, this could be set to false if you want to have a task doing something
        ///     in the background
        ///     In general you would like this to be true, otherwise depending code might be started to early
        /// </summary>
        [DefaultValue(true)]
        public bool AwaitStart { get; set; } = true;
    }
}