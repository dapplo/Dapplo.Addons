//  Dapplo - building blocks for desktop applications
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

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition.Hosting;
using System.Reflection;
using System.Text.RegularExpressions;

#endregion

namespace Dapplo.Addons
{
	/// <summary>
	///     This interface is one of many which the Dapplo.Addon CompositionBootstrapper (ApplicationBootstrapper) implements.
	///     The Bootstrapper will automatically export itself as IServiceRepository, so framework code can dlls and assemblies
	///     A IServiceRepository should only be used for cases where assemblies need to be scanned or exported
	/// </summary>
	public interface IServiceRepository
	{
		/// <summary>
		///     all assemblies this bootstrapper knows
		/// </summary>
		IList<Assembly> KnownAssemblies { get; }

		/// <summary>
		///     All addon files this bootstrapper knows
		/// </summary>
		IList<string> KnownFiles { get; }

		/// <summary>
		/// Add a scan directory
		/// </summary>
		/// <param name="directory">string with the directory</param>
		void AddScanDirectory(string directory);

		/// <summary>
		///     Add an assembly to the AggregateCatalog.Catalogs
		///     In english: make the items in the assembly discoverable
		/// </summary>
		/// <param name="assemblyName">Name of the Assembly to add</param>
		void Add(string assemblyName);

		/// <summary>
		///     Add an assembly to the AggregateCatalog.Catalogs
		///     In english: make the items in the assembly discoverable
		/// </summary>
		/// <param name="assembly">Assembly to add</param>
		void Add(Assembly assembly);

		/// <summary>
		///     Add an AssemblyCatalog AggregateCatalog.Catalogs
		///     But only if the AssemblyCatalog has parts
		/// </summary>
		/// <param name="assemblyCatalog">AssemblyCatalog to add</param>
		void Add(AssemblyCatalog assemblyCatalog);

		/// <summary>
		///     Add the assemblies (with parts) found in the specified directory or embedded
		/// </summary>
		/// <param name="directory">Directory to scan</param>
		/// <param name="pattern">Regex pattern to use for the scan, default is all assemblies</param>
		/// <param name="loadEmbedded">true, which is default, to also load embedded matching dlls</param>
		void Add(string directory, Regex pattern = null, bool loadEmbedded = true);

		/// <summary>
		///     Add the assemblies (with parts) found in the specified directory or embedded
		/// </summary>
		/// <param name="directory">Directory to scan</param>
		/// <param name="pattern">string with regex pattern to use for the scan, default is all assemblies</param>
		/// <param name="loadEmbedded">true, which is default, to also load embedded matching dlls</param>
		void Add(string directory, string pattern, bool loadEmbedded = true);

		/// <summary>
		///     Add the assembly for the specified type
		/// </summary>
		/// <param name="type">The assembly for the type is retrieved add added via the Add(Assembly) method</param>
		void Add(Type type);
	}
}