// Dapplo - building blocks for .NET applications
// Copyright (C) 2016-2019 Dapplo
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

using System.Collections.Generic;
using System.Reflection;
using System.Text.RegularExpressions;

namespace Dapplo.Addons
{
    /// <summary>
    /// The interface for the AssemblyResolver
    /// </summary>
    public interface IAssemblyResolver
    {
        /// <summary>
        /// A regex with all the assemblies which we should ignore
        /// </summary>
        Regex AssembliesToIgnore { get; }

        /// <summary>
        /// A dictionary with all the loaded assemblies, for caching and analysing
        /// </summary>
        IDictionary<string, Assembly> LoadedAssemblies { get; }

        /// <summary>
        /// Gives access to the resources in assemblies
        /// </summary>
        IResourceProvider Resources { get; }

        /// <summary>
        /// Specify if embedded assemblies written to disk before using will be removed again when the process exits
        /// </summary>
        bool CleanupAfterExit { get; set; }

        /// <summary>
        /// Get a list of all embedded assemblies
        /// </summary>
        /// <returns>IEnumerable with a tutple containing the name of the resource and of the assemblie</returns>
        IEnumerable<string> EmbeddedAssemblyNames(IEnumerable<Assembly> assembliesToCheck = null);

        /// <summary>
        /// Remove event registrations
        /// </summary>
        void Dispose();
    }
}