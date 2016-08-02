﻿#region Dapplo 2016 - GNU Lesser General Public License

// Dapplo - building blocks for .NET applications
// Copyright (C) 2016 Dapplo
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

#endregion

namespace Dapplo.Addons
{
	/// <summary>
	///     This is the interface for all bootstrappers
	/// </summary>
	public interface IBootstrapper : IServiceLocator, IServiceExporter, IServiceRepository, IDisposable
	{
		/// <summary>
		///     Is this IBootstrapper initialized?
		/// </summary>
		bool IsInitialized { get; }

		/// <summary>
		///     Initialize the bootstrapper
		/// </summary>
		Task<bool> InitializeAsync(CancellationToken cancellationToken);

		/// <summary>
		///     Start the bootstrapper, initialize is automatically called when needed
		/// </summary>
		Task<bool> RunAsync(CancellationToken cancellationToken);

		/// <summary>
		///     Stop the bootstrapper, this cleans up resources and makes it possible to hook into it.
		///     Is also called when being disposed, but as Dispose in not Async this could cause some issues.
		/// </summary>
		Task<bool> StopAsync(CancellationToken cancellationToken);
	}
}