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
		ISet<string> KnownAssemblies { get; }

		/// <summary>
		///     All addon files this bootstrapper knows
		/// </summary>
		IList<string> KnownFiles { get; }

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
		///     Add the assembly for the specified type
		/// </summary>
		/// <param name="type">The assembly for the type is retrieved add added via the Add(Assembly) method</param>
		void Add(Type type);

		/// <summary>
		///     Add a scan directory, which is used by the assembly resolving
		/// </summary>
		/// <param name="directory">string with the directory</param>
		void AddScanDirectory(string directory);

		/// <summary>
		///     Find the the assembly with the specified name, and load it.
		///     The assembly will be searched in the directories added by AddScanDirectory, and also in the embedded resources.
		/// </summary>
		/// <param name="assemblyName">string with the assembly name</param>
		/// <param name="extensions">IEnumerable with extensions to look for, defaults will be set if null was passed</param>
		void FindAndLoadAssembly(string assemblyName, IEnumerable<string> extensions = null);

		/// <summary>
		///     Find the assemblies (with parts) found in default directories, or manifest resources, matching the specified
		///     filepattern.
		/// </summary>
		/// <param name="pattern">File-Pattern to use for the scan, default all dlls will be found</param>
		/// <param name="loadEmbedded">bool specifying if embedded resources should be used. default is true</param>
		/// <param name="extensions">IEnumerable with extensions to look for, defaults will be set if null was passed</param>
		void FindAndLoadAssemblies(string pattern = "*", bool loadEmbedded = true, IEnumerable<string> extensions = null);

		/// <summary>
		///     Find the assemblies (with parts) found in the specified directory, or manifest resources, matching the specified
		///     regex.
		/// </summary>
		/// <param name="directory">Directory to scan</param>
		/// <param name="pattern">Regex to use for the scan, when null all dlls will be found</param>
		/// <param name="loadEmbedded">bool specifying if embedded resources should be used. default is true</param>
		void FindAndLoadAssembliesFromDirectory(string directory, Regex pattern = null, bool loadEmbedded = true);

		/// <summary>
		///     Find the assemblies (with parts) found in the specified directory, or manifest resources, matching the specified
		///     filepattern.
		/// </summary>
		/// <param name="directory">Directory to scan</param>
		/// <param name="pattern">File-Pattern to use for the scan, default all dlls will be found</param>
		/// <param name="loadEmbedded">bool specifying if embedded resources should be used. default is true</param>
		/// <param name="extensions">IEnumerable with extensions to look for, defaults will be set if null was passed</param>
		void FindAndLoadAssembliesFromDirectory(string directory, string pattern = "*", bool loadEmbedded = true, IEnumerable<string> extensions = null);

		/// <summary>
		///     Find the assemblies (with parts) found in the specified directories, or manifest resources, matching the specified
		///     regex.
		/// </summary>
		/// <param name="directories">Directory to scan</param>
		/// <param name="pattern">Regex to use for the scan, when null all dlls will be found</param>
		/// <param name="loadEmbedded">bool specifying if embedded resources should be used. default is true</param>
		void FindAndLoadAssemblies(IEnumerable<string> directories, Regex pattern, bool loadEmbedded = true);
	}
}