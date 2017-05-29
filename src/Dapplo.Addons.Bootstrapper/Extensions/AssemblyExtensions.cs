#region Dapplo 2016-2017 - GNU Lesser General Public License

// Dapplo - building blocks for .NET applications
// Copyright (C) 2016-2017 Dapplo
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
using System.Reflection;

namespace Dapplo.Addons.Bootstrapper.Extensions
{
    /// <summary>
    /// Extensions to help using an Assembly
    /// </summary>
    public static class AssemblyExtensions
    {
        /// <summary>
        /// Get the location of an assembly
        /// </summary>
        /// <param name="assembly">Assembly</param>
        /// <param name="allowCodeBase">specify if it's okay to also consider the codeBase value</param>
        /// <returns>string or null if it's dynamically created</returns>
        public static string GetLocation(this Assembly assembly, bool allowCodeBase = true)
        {
            // No location if it's dynamic
            if (assembly.IsDynamic)
            {
                return null;
            }
            var location = assembly.Location;
            if (allowCodeBase && string.IsNullOrEmpty(location))
            {
                location = new Uri(assembly.CodeBase).LocalPath;
            }
            return location;
        }
    }
}
