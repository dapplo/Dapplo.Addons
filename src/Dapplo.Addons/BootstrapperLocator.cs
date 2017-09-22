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

#region Usings

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

#endregion

namespace Dapplo.Addons
{
    /// <summary>
    ///     A helper class to get to the current IBootstrapper
    ///     Although this is bad practice, from my point of view this is better than having to add Dapplo.Addons.Bootstrapper
    ///     as a dependency.
    /// </summary>
    public static class BootstrapperLocator
    {
        /// <summary>
        ///     Used to register / deregister the bootstrappers
        /// </summary>
        private static readonly IList<IBootstrapper> BootstrapperRegistry = new List<IBootstrapper>();

        /// <summary>
        ///     Register the bootstrapper
        /// </summary>
        /// <param name="bootstrapper">IBootstrapper</param>
        public static void Register(IBootstrapper bootstrapper)
        {
            BootstrapperRegistry.Add(bootstrapper);
        }

        /// <summary>
        ///     Unregister the bootstrapper
        /// </summary>
        /// <param name="bootstrapper">IBootstrapper</param>
        public static void Unregister(IBootstrapper bootstrapper)
        {
            BootstrapperRegistry.Remove(bootstrapper);
        }

        /// <summary>
        ///     All available bootstrappers
        /// </summary>
        public static IReadOnlyCollection<IBootstrapper> Bootstrappers => new ReadOnlyCollection<IBootstrapper>(BootstrapperRegistry);

        /// <summary>
        ///     Get the current IBootstrapper, if there are multiple than this is the latest.
        /// </summary>
        public static IBootstrapper CurrentBootstrapper => BootstrapperRegistry.Last();
    }
}