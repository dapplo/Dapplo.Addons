﻿//  Dapplo - building blocks for desktop applications
//  Copyright (C) 2016 Dapplo
// 
//  For more information see: http://dapplo.net/
//  Dapplo repositories are hosted on GitHub: https://github.com/dapplo
// 
//  This file is part of Dapplo.Addons
// 
//  Dapplo.Addons is free software: you can redistribute it and/or modify
//  it under the terms of the GNU Lesser General Public License as published by
//  the Free Software Foundation, either version 3 of the License, or
//  (at your option) any later version.
// 
//  Dapplo.Addons is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//  GNU Lesser General Public License for more details.
// 
//  You should have a copy of the GNU Lesser General Public License
//  along with Dapplo.Addons. If not, see <http://www.gnu.org/licenses/lgpl.txt>.

#region using

using System.Threading;
using System.Threading.Tasks;

#endregion

namespace Dapplo.Addons
{
	/// <summary>
	///     The IShutdownAction describes modules that are called on shutdown
	/// </summary>
	public interface IShutdownAction : IModule
	{
		/// <summary>
		///     Perform a shutdown of whatever needs to be shutdown.
		///     Make sure this can be called multiple times, e.g. do nothing when it was already started.
		/// </summary>
		/// <param name="token">CancellationToken</param>
		/// <returns>Task</returns>
		Task ShutdownAsync(CancellationToken token = default(CancellationToken));
	}
}