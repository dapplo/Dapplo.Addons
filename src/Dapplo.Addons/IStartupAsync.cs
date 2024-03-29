// Dapplo - building blocks for .NET applications
// Copyright (C) 2016-2021 Dapplo
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

using System.Threading;
using System.Threading.Tasks;

namespace Dapplo.Addons
{
    /// <summary>
    ///     Use IStartupAsync for things which need to start async
    /// </summary>
    public interface IStartupAsync : IService
    {
        /// <summary>
        ///     Perform a start of whatever needs to be started.
        ///     Make sure this can be called multiple times, e.g. do nothing when it was already started.
        ///     throw a StartupException if something went terribly wrong and the application should NOT continue
        /// </summary>
        /// <param name="cancellationToken">CancellationToken</param>
        /// <returns>Task</returns>
        Task StartupAsync(CancellationToken cancellationToken = default);
    }
}