#region Dapplo 2016-2019 - GNU Lesser General Public License

// Dapplo - building blocks for .NET applications
// Copyright (C) 2016-2019 Dapplo
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

using System;
using System.Globalization;
using System.Reflection;
using Module = Autofac.Module;

namespace Dapplo.Addons
{
    /// <summary>
    /// Extend from this to make sure your module is loaded via Dapplo.Addons
    /// </summary>
    public abstract class AddonModule : Module
    {
        /// <summary>
        /// Gets the assembly in which the concrete module type is located. To avoid bugs whereby deriving from a module will
        /// change the target assembly, this property can only be used by modules that inherit directly from
        /// <see cref="AddonModule"/>.
        /// </summary>
        protected override Assembly ThisAssembly
        {
            get
            {
                var thisType = GetType();
                var baseType = thisType.GetTypeInfo().BaseType;
                if (baseType != typeof(AddonModule))
                    throw new InvalidOperationException(string.Format(CultureInfo.CurrentCulture, "Module.ThisAssembly is only available in modules that inherit directly from AddonModule. It can't be used in '{0}' which inherits from '{1}'.", thisType, baseType));

                return thisType.GetTypeInfo().Assembly;
            }
        }
    }
}
