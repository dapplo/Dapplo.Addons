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

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition.Hosting;
using System.ComponentModel.Composition.Primitives;

#endregion

namespace Dapplo.Addons
{
	/// <summary>
	///     This interface is one of many which the Dapplo.Addon CompositionBootstrapper (ApplicationBootstrapper) implements.
	///     The Bootstrapper will automatically export itself as IServiceExporter, so framework code can specify what exports
	///     are available
	///     A IServiceExporter should only be used for cases where a simple export can't work.
	/// </summary>
	public interface IServiceExporter
	{
		/// <summary>
		///     Export an object
		/// </summary>
		/// <typeparam name="T">Type to export</typeparam>
		/// <param name="obj">object to add</param>
		/// <param name="metadata">Metadata for the export</param>
		/// <returns>ComposablePart, this can be used to remove the export later</returns>
		ComposablePart Export<T>(T obj, IDictionary<string, object> metadata = null);

		/// <summary>
		///     Export an object
		/// </summary>
		/// <param name="type">Type to export</param>
		/// <param name="obj">object to add</param>
		/// <param name="metadata">Metadata for the export</param>
		/// <returns>ComposablePart, this can be used to remove the export later</returns>
		ComposablePart Export(Type type, object obj, IDictionary<string, object> metadata = null);

		/// <summary>
		///     Export an object
		/// </summary>
		/// <typeparam name="T">Type to export</typeparam>
		/// <param name="contractName">contractName under which the object of Type T is registered</param>
		/// <param name="obj">object to add</param>
		/// <param name="metadata">Metadata for the export</param>
		/// <returns>ComposablePart, this can be used to remove the export later</returns>
		ComposablePart Export<T>(string contractName, T obj, IDictionary<string, object> metadata = null);

		/// <summary>
		///     Export an object
		/// </summary>
		/// <param name="type">Type to export</param>
		/// <param name="contractName">contractName under which the object of Type T is registered</param>
		/// <param name="obj">object to add</param>
		/// <param name="metadata">Metadata for the export</param>
		/// <returns>ComposablePart, this can be used to remove the export later</returns>
		ComposablePart Export(Type type, string contractName, object obj, IDictionary<string, object> metadata = null);

		/// <summary>
		///     Release an export which was previously added with the Export method
		/// </summary>
		/// <param name="part">ComposablePart from Export call</param>
		void Release(ComposablePart part);

		/// <summary>
		/// The list of export providers used when an export cannot be found, these need to be added before the bootstrapper is started
		/// </summary>
		IList<ExportProvider> ExportProviders { get; }
	}
}