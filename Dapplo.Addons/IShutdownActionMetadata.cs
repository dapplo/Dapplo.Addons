﻿//  Dapplo - building blocks for desktop applications
//  Copyright (C) 2015-2016 Dapplo
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
//  You should have Config a copy of the GNU Lesser General Public License
//  along with Dapplo.Addons. If not, see <http://www.gnu.org/licenses/lgpl.txt>.

#region using

using System.ComponentModel;

#endregion

namespace Dapplo.Addons
{
	/// <summary>
	///     Meta-data belonging to the ShutdownActionAttribute, which makes it possible to specify type-safe meta-data.
	/// </summary>
	public interface IShutdownActionMetadata
	{
		/// <summary>
		///     Order in which IShutdownAction.ShutdownAsync is called
		/// </summary>
		[DefaultValue(1)]
		int ShutdownOrder { get; }
	}
}