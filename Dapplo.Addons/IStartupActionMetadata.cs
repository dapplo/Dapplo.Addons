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

using System.ComponentModel;

#endregion

namespace Dapplo.Addons
{
	/// <summary>
	///     Meta-data belonging to the StartupActionAttribute, which makes it possible to specify type-safe meta-data.
	/// </summary>
	public interface IStartupActionMetadata
	{
		/// <summary>
		///     Specify if the startup needs to be awaited, this could be set to false if you want to have a task doing something
		///     in the background
		///     In general you would like this to be true, otherwise depending code might be started to early
		/// </summary>
		[DefaultValue(true)]
		bool AwaitStart { get; }

		/// <summary>
		///     Order in which IStartupAction.StartAsync is called
		/// </summary>
		[DefaultValue(1)]
		int StartupOrder { get; }
	}
}