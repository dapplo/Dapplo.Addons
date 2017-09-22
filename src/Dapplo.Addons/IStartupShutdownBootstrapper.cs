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

using System.Threading;
using System.Threading.Tasks;

#endregion

namespace Dapplo.Addons
{
    /// <summary>
    ///     Interface for the StartupShutdownBootstrapper
    /// </summary>
    public interface IStartupShutdownBootstrapper : IBootstrapper
    {
        /// <summary>
        ///     Startup all "Startup actions"
        ///     Call this after run, it will find all IStartupAction's and start them in the specified order
        /// </summary>
        /// <param name="cancellationToken">CancellationToken</param>
        /// <returns>Task</returns>
        Task StartupAsync(CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        ///     Initiate Shutdown on all "Shutdown actions"
        /// </summary>
        /// <param name="cancellationToken">CancellationToken</param>
        /// <returns>Task</returns>
        Task ShutdownAsync(CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        ///     This cancels the startup
        /// </summary>
        void CancelStartup();
    }
}