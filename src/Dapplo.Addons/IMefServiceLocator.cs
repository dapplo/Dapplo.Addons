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
using System.Collections.Generic;

#endregion

namespace Dapplo.Addons
{
    /// <summary>
    ///     This interface is one of many which the Dapplo.Addon CompositionBootstrapper (ApplicationBootstrapper) implements.
    ///     The Bootstrapper will automatically export itself as IMefServiceLocator, so framework code can use imports to get
    ///     basic servicelocator support.
    ///     A IMefServiceLocator should only be used for cases where a simple import can't work.
    /// </summary>
    public interface IMefServiceLocator
    {
        /// <summary>
        ///     Simple "service-locater"
        /// </summary>
        /// <typeparam name="T">Type to locate</typeparam>
        /// <param name="contractName">Key/Name of the contract, null or an empty string</param>
        /// <returns>Lazy T</returns>
        Lazy<T> GetExport<T>(string contractName = "");

        /// <summary>
        ///     Simple "service-locater" with meta-data
        /// </summary>
        /// <typeparam name="T">Type to locate</typeparam>
        /// <typeparam name="TMetaData">interface-type for the meta-data</typeparam>
        /// <param name="contractName">Key/Name of the contract, null or an empty string</param>
        /// <returns>Lazy T,TMetaData</returns>
        Lazy<T, TMetaData> GetExport<T, TMetaData>(string contractName = "");

        /// <summary>
        ///     Simple "service-locater"
        /// </summary>
        /// <param name="type">Type to locate</param>
        /// <param name="contractName">Key/Name of the contract, null or an empty string</param>
        /// <returns>object for type</returns>
        object GetExport(Type type, string contractName = "");

        /// <summary>
        ///     Simple "service-locater" to get multiple exports
        /// </summary>
        /// <typeparam name="T">Type to locate</typeparam>
        /// <param name="contractName">Key/Name of the contract, null or an empty string</param>
        /// <returns>IEnumerable of Lazy T</returns>
        IEnumerable<Lazy<T>> GetExports<T>(string contractName = "");

        /// <summary>
        ///     Simple "service-locater" to get multiple exports
        /// </summary>
        /// <param name="type">Type to locate</param>
        /// <param name="contractName">Key/Name of the contract, null or an empty string</param>
        /// <returns>IEnumerable of Lazy object</returns>
        IEnumerable<Lazy<object>> GetExports(Type type, string contractName = "");

        /// <summary>
        ///     Simple "service-locater" to get multiple exports with meta-data
        /// </summary>
        /// <typeparam name="T">Type to locate</typeparam>
        /// <typeparam name="TMetaData">interface-type for the meta-data</typeparam>
        /// <param name="contractName">Key/Name of the contract, null or an empty string</param>
        /// <returns>IEnumerable of Lazy T,TMetaData</returns>
        IEnumerable<Lazy<T, TMetaData>> GetExports<T, TMetaData>(string contractName = "");
    }
}