/*
	Dapplo - building blocks for desktop applications
	Copyright (C) 2015-2016 Dapplo

	For more information see: http://dapplo.net/
	Dapplo repositories are hosted on GitHub: https://github.com/dapplo

	This file is part of Dapplo.Addons

	Dapplo.Addons is free software: you can redistribute it and/or modify
	it under the terms of the GNU General Public License as published by
	the Free Software Foundation, either version 3 of the License, or
	(at your option) any later version.

	Dapplo.Addons is distributed in the hope that it will be useful,
	but WITHOUT ANY WARRANTY; without even the implied warranty of
	MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
	GNU General Public License for more details.

	You should have received a copy of the GNU General Public License
	along with Dapplo.Addons. If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.Threading;
using System.Threading.Tasks;

namespace Dapplo.Addons
{
	/// <summary>
	/// This is the interface for all bootstrappers
	/// </summary>
	public interface IBootstrapper : IServiceLocator, IDisposable
	{
		/// <summary>
		/// Initialize the bootstrapper
		/// </summary>
		Task<bool> InitializeAsync(CancellationToken cancellationToken);

		/// <summary>
		/// Start the bootstrapper, initialize is automatically called when needed
		/// </summary>
		Task<bool> RunAsync(CancellationToken cancellationToken);

		/// <summary>
		/// Stop the bootstrapper, this cleans up resources and makes it possible to hook into it.
		/// Is also called when being disposed, but as Dispose in not Async this could cause some issues.
		/// </summary>
		Task<bool> StopAsync(CancellationToken cancellationToken);
	}
}
