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
using System.ComponentModel.Composition;

#endregion

namespace Dapplo.Addons
{
    /// <summary>
    ///     Use this attribute to specify the details of a service, like the name and on what this depends
    /// </summary>
    [MetadataAttribute]
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public class ServiceAttribute : Attribute
    {
        /// <summary>
        /// Default service, used by autofac, this should actually not be used directly...
        /// </summary>
        public ServiceAttribute()
        {
        }

        /// <summary>
        /// Specify the name of the service and an optional a service this depends on
        /// </summary>
        /// <param name="name">string</param>
        /// <param name="prerequisites">string, optional</param>
        public ServiceAttribute(string name, params string[] prerequisites)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            if (prerequisites != null)
            {
                Prerequisites = prerequisites;
            }
        }

        /// <summary>
        ///     Here the order of the startup action can be specified, starting startup with low values and ending with high.
        ///     With this a cheap form of "dependency" management is made.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        ///     Describes the service (name) this service depends on
        /// </summary>
        [DefaultValue(null)]
        public string [] Prerequisites { get; set; }

        /// <summary>
        /// The name of the TaskScheduler, or null if the threadpool is used
        /// </summary>
        [DefaultValue(null)]
        public string TaskSchedulerName { get; set; }

        /// <summary>
        /// If this is true, the service will not be started if the prerequisite is missing.
        /// Default is false, which will stop the startup if it is missing.
        /// </summary>
        [DefaultValue(false)]
        public bool SkipIfPrerequisiteIsMissing { get; set; }

        /// <summary>
        /// If this is true, the startup of the whole branch of services is not awaited.
        /// This does not have have influence on the shutdown.
        /// </summary>
        [DefaultValue(false)]
        public bool SkipAwait { get; set; }

    }
}