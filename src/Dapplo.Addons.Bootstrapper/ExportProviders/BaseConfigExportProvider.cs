#region Dapplo 2016 - GNU Lesser General Public License

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
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel.Composition.Hosting;
using System.ComponentModel.Composition.Primitives;
using Dapplo.Log;

#endregion

namespace Dapplo.Addons.Bootstrapper.ExportProviders
{
	/// <summary>
	///     This contains the reused caches for the generic ConfigExportProvider
	/// </summary>
	public abstract class BaseConfigExportProvider : ExportProvider
	{
		/// <summary>
		/// The logger for the ConfigExportProvider
		/// </summary>
		protected static readonly LogSource Log = new LogSource(typeof(ConfigExportProvider<>));
		/// <summary>
		/// Type-Cache for all the ConfigExportProvider 
		/// </summary>
		protected static readonly IDictionary<string, Type> TypeLookupDictionary = new ConcurrentDictionary<string, Type>(StringComparer.OrdinalIgnoreCase);
	}
}