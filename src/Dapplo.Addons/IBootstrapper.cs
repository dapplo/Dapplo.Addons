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
using System.Threading;
using System.Threading.Tasks;
using CommonServiceLocator;

#endregion

namespace Dapplo.Addons
{
    /// <summary>
    ///     This is the interface for all bootstrappers
    /// </summary>
    public interface IBootstrapper : IServiceLocator, IMefServiceLocator, IServiceExporter, IServiceRepository, IDependencyProvider, IDisposable
    {
        /// <summary>
        ///     Specifies if the bootstrapper is allowed to remove assemblies which are already embedded from the file system.
        ///     This normally prevents issues with double loading of assemblies, like casts which do not work.
        ///     This is turned off by default.
        /// </summary>
        bool AllowAssemblyCleanup { get; set; }

        /// <summary>
        ///     Register a disposable, to dispose when the IBootstrapper is disposed
        /// </summary>
        /// <param name="disposable">IDisposable to dispose together with the bootstapper</param>
        void RegisterForDisposal(IDisposable disposable);

        /// <summary>
        ///     Is this IBootstrapper initialized?
        /// </summary>
        bool IsInitialized { get; }

        /// <summary>
        ///     Is this IBootstrapper running?
        /// </summary>
        bool IsRunning { get; }

        /// <summary>
        ///     Initialize the bootstrapper
        /// </summary>
        Task<bool> InitializeAsync(CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        ///     Start the bootstrapper, initialize is automatically called when needed
        /// </summary>
        Task<bool> RunAsync(CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        ///     Stop the bootstrapper, this cleans up resources and makes it possible to hook into it.
        ///     Is also called when being disposed, but as Dispose in not Async this could cause some issues.
        /// </summary>
        Task<bool> StopAsync(CancellationToken cancellationToken = default(CancellationToken));
    }
}