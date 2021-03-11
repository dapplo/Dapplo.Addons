// Dapplo - building blocks for .NET applications
// Copyright (C) 2016-2021 Dapplo
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

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;

namespace Dapplo.Addons
{
    /// <summary>
    /// This is the interface for something which can provide you with embedded resources
    /// </summary>
    public interface IResourceProvider
    {
        /// <summary>
        /// Get a resource as stream, the resource is with offset to the namespace of the assembly
        ///     It will automatically un-compress if the file-ending is .gz or .compressed
        ///     Note: a GZipStream is not seekable, this might cause issues.
        /// </summary>
        /// <param name="type">The type whose namespace is used to scope the manifest resource name.</param>
        /// <param name="segments">The case-sensitive name (or segments added to the namespace), of the manifest resource being requested.</param>
        /// <returns></returns>
        Stream ResourceAsStream(Type type, params string[] segments);

        /// <summary>
        /// Get a resource as stream
        ///     It will automatically un-compress if the file-ending is .gz or .compressed
        ///     Note: a GZipStream is not seekable, this might cause issues.
        /// </summary>
        /// <param name="assembly">Assembly containing the resource</param>
        /// <param name="segments">string array, used to specify the location and name of the resource</param>
        /// <returns>Stream</returns>
        Stream AbsoluteResourceAsStream(Assembly assembly, params string[] segments);

        /// <summary>
        /// Get a resource as stream, the resource is with offset to the namespace of the assembly
        ///     It will automatically un-compress if the file-ending is .gz or .compressed
        ///     Note: a GZipStream is not seekable, this might cause issues.
        /// </summary>
        /// <param name="assembly">Assembly containing the resource</param>
        /// <param name="segments">string array, used to specify the location and name of the resource</param>
        /// <returns>Stream</returns>
        Stream ResourceAsStream(Assembly assembly, params string[] segments);

        /// <summary>
        ///     Get the stream for a assembly manifest resource based on the filePath
        ///     It will automatically un-compress if the file-ending is .gz or .compressed
        ///     Note: a GZipStream is not seekable, this might cause issues.
        /// </summary>
        /// <param name="assembly">Assembly to look into</param>
        /// <param name="filePath">string with the filepath to find</param>
        /// <param name="ignoreCase">true, which is default, to ignore the case when comparing</param>
        /// <returns>Stream for the filePath, or null if not found</returns>
        Stream LocateResourceAsStream(Assembly assembly, string filePath, bool ignoreCase = true);

        /// <summary>
        /// Get the ManifestResourceNames for the specified assembly from cache or directly.
        /// </summary>
        /// <param name="possibleResourceAssembly">Assembly</param>
        /// <returns>string array with resources names</returns>
        string[] GetCachedManifestResourceNames(Assembly possibleResourceAssembly);

        /// <summary>
        /// Returns the fully qualified resource name of a resource
        /// </summary>
        /// <param name="type">The type whose namespace is used to scope the manifest resource name.</param>
        /// <param name="names">The case-sensitive name, parts, of the manifest resource being requested.</param>
        /// <returns>string</returns>
        string Find(Type type, params string[] names);

        /// <summary>
        ///     Scan the manifest of the supplied Assembly with a regex pattern for embedded resources
        /// </summary>
        /// <param name="assembly">Assembly to scan</param>
        /// <param name="regexPattern">Regex pattern to scan for</param>
        /// <returns>IEnumerable with matching resource names</returns>
        IEnumerable<string> FindEmbeddedResources(Assembly assembly, Regex regexPattern);

        /// <summary>
        ///     Scan the manifest of the supplied Assembly with a regex pattern for embedded resources
        /// </summary>
        /// <param name="assembly">Assembly to scan</param>
        /// <param name="regexPattern">Regex pattern to scan for</param>
        /// <param name="regexOptions">RegexOptions.IgnoreCase as default</param>
        /// <returns>IEnumerable with matching resource names</returns>
        IEnumerable<string> FindEmbeddedResources(Assembly assembly, string regexPattern, RegexOptions regexOptions = RegexOptions.IgnoreCase);

        /// <summary>
        ///     Returns the embedded resource, as specified in the Pack-Uri as a stream.
        ///     This currently doesn't go into the embedded .g.resources files, this might be added later
        /// </summary>
        /// <param name="applicationPackUri">Uri</param>
        /// <returns>Stream</returns>
        Stream ResourceAsStream(Uri applicationPackUri);

        /// <summary>
        ///     Test if there is an embedded resource for the Pack-Uri
        ///     This is work in progress, as most of the times the files are compiled from XAML to BAML, and won't be recognized
        ///     when you specify a pack uri ending on .xaml
        /// </summary>
        /// <param name="packUri">Uri</param>
        /// <param name="ignoreCase">true to ignore the case</param>
        /// <returns>Stream</returns>
        bool EmbeddedResourceExists(Uri packUri, bool ignoreCase = true);

    }
}
