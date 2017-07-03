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

namespace Dapplo.Addons
{
    /// <summary>
    ///     Interface for the ApplicationBootstrapper
    /// </summary>
    public interface IApplicationBootstrapper : IStartupShutdownBootstrapper
    {
        /// <summary>
        ///     Name of the application for this bootstrapper
        /// </summary>
        string ApplicationName { get; }

        /// <summary>
        /// Is the mutex for the application locked=
        /// </summary>
        bool IsMutexLocked { get; }
    }
}