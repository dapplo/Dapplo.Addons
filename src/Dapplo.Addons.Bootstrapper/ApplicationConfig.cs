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

using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Dapplo.Addons.Bootstrapper
{
    /// <summary>
    /// This specifies the configuration for the ApplicationBootstrapper
    /// </summary>
    public class ApplicationConfig
    {
        /// <summary>
        /// The properties for the container
        /// </summary>
        public IReadOnlyDictionary<string, object> Properties { get; internal set; }

        /// <summary>
        /// Specifies if the application bootstrapper should scan embedded assemblies
        /// </summary>
        public bool ScanForEmbeddedAssemblies { get; internal set; } = true;

        /// <summary>
        /// Specifies if the application bootstrapper copy embedded assemblies to the file system
        /// </summary>
        public bool CopyEmbeddedAssembliesToFileSystem { get; internal set; } = true;

        /// <summary>
        /// Specifies if assemblies outside the probing path can be copied to the probing path
        /// </summary>
        public bool CopyAssembliesToProbingPath { get; internal set; }
#if !NETSTANDARD2_0
            = true;
#endif
        /// <summary>
        /// This specifies if the loading of assemblies can be done async
        /// </summary>
        public bool UseAsyncAssemblyLoading { get; internal set; } = true;

        /// <summary>
        /// The directories to scan for addons
        /// </summary>
        public IReadOnlyList<string> ScanDirectories { get; internal set; }

        /// <summary>
        /// The names of assemblies to load
        /// </summary>
        public IReadOnlyList<string> AssemblyNames { get; internal set; }

        /// <summary>
        /// The patterns of assembly names to load
        /// </summary>
        public IReadOnlyList<Regex> AssemblyNamePatterns { get; internal set; }

        /// <summary>
        /// The allowed assembly extensions to load, default .dll
        /// </summary>
        public IReadOnlyList<string> Extensions { get; internal set; }

        /// <summary>
        /// The name of the application
        /// </summary>
        public string ApplicationName { get; internal set; }

        /// <summary>
        /// The id of the mutex, if any
        /// </summary>
        public string Mutex { get; internal set; }

        /// <summary>
        /// Specify if the mutex is global, default is false
        /// </summary>
        public bool UseGlobalMutex { get; internal set; }

        /// <summary>
        /// Test if a mutex is set
        /// </summary>
        public bool HasMutex => !string.IsNullOrEmpty(Mutex);

        /// <summary>
        /// Strict checking, especially useful to prevent wrong configurations.
        /// Currently this forces a check for the ScanDirectories
        /// </summary>
        public bool UseStrictChecking { get; internal set; } = true;
    }
}
