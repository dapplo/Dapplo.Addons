/*
 * dapplo - building blocks for desktop applications
 * Copyright (C) Dapplo 2015-2016
 * 
 * For more information see: http://dapplo.net/
 * dapplo repositories are hosted on GitHub: https://github.com/dapplo
 * 
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 1 of the License, or
 * (at your option) any later version.
 * 
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 * 
 * You should have received a copy of the GNU General Public License
 * along with this program. If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition.Primitives;

namespace Dapplo.Addons
{
	/// <summary>
	/// This interface is what the Dapplo.Addon CompositionBootstrapper (ApplicationBootstrapper) implements.
	/// The Bootstrapper will automatically export itself as IServiceLocator, so framework code can use imports to get basic servicelocator support.
	/// This IServiceLocator should only be used for cases where a simple import/export can't work.
	/// </summary>
	public interface IServiceLocator
	{
		/// <summary>
		/// Export an object
		/// </summary>
		/// <typeparam name="T">Type to export</typeparam>
		/// <param name="obj">object to add</param>
		/// <param name="metadata">Metadata for the export</param>
		/// <returns>ComposablePart, this can be used to remove the export later</returns>
		ComposablePart Export<T>(T obj, IDictionary<string, object> metadata = null);

		/// <summary>
		/// Export an object
		/// </summary>
		/// <typeparam name="T">Type to export</typeparam>
		/// <param name="contractName">contractName under which the object of Type T is registered</param>
		/// <param name="obj">object to add</param>
		/// <param name="metadata">Metadata for the export</param>
		/// <returns>ComposablePart, this can be used to remove the export later</returns>
		ComposablePart Export<T>(string contractName, T obj, IDictionary<string, object> metadata = null);

		/// <summary>
		/// Release an export which was previously added with the Export method
		/// </summary>
		/// <param name="part">ComposablePart from Export call</param>
		void Release(ComposablePart part);

		/// <summary>
		/// Fill all the imports in the object isntance
		/// </summary>
		/// <param name="importingObject">object to fill the imports for</param>
		void FillImports(object importingObject);

		/// <summary>
		/// Simple "service-locater"
		/// </summary>
		/// <typeparam name="T">Type to locate</typeparam>
		/// <returns>Lazy T</returns>
		Lazy<T> GetExport<T>();

		/// <summary>
		/// Simple "service-locater" with meta-data
		/// </summary>
		/// <typeparam name="T">Type to locate</typeparam>
		/// <typeparam name="TMetaData">interface-type for the meta-data</typeparam>
		/// <returns>Lazy T,TMetaData</returns>
		Lazy<T, TMetaData> GetExport<T, TMetaData>();

		/// <summary>
		/// Simple "service-locater" to get multiple exports
		/// </summary>
		/// <typeparam name="T">Type to locate</typeparam>
		/// <returns>IEnumerable of Lazy T</returns>
		IEnumerable<Lazy<T>> GetExports<T>();

		/// <summary>
		/// Simple "service-locater" to get multiple exports with meta-data
		/// </summary>
		/// <typeparam name="T">Type to locate</typeparam>
		/// <typeparam name="TMetaData">interface-type for the meta-data</typeparam>
		/// <returns>IEnumerable of Lazy T,TMetaData</returns>
		IEnumerable<Lazy<T, TMetaData>> GetExports<T, TMetaData>();
	}
}
