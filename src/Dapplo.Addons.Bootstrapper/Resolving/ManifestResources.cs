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
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Resources;
using System.Text.RegularExpressions;
using Dapplo.Addons.Bootstrapper.Extensions;
using Dapplo.Log;

#endregion

namespace Dapplo.Addons.Bootstrapper.Resolving
{
    /// <summary>
    ///     Utilities for embedded resources
    /// </summary>
    public class ManifestResources : IResourceProvider
    {
        private static readonly LogSource Log = new LogSource();
        private readonly Func<string, Assembly> _findAssembly;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="findAssembly">Func to find an Assembly by name</param>
        public ManifestResources(Func<string, Assembly> findAssembly)
        {
            _findAssembly = findAssembly;
        }

        /// <summary>
        /// Mapping between Assemblies and the contained resources files
        /// </summary>
        public IDictionary<Assembly, string[]> AssemblyResourceNames { get; } = new ConcurrentDictionary<Assembly, string[]>();

        /// <summary>
        /// Get the ManifestResourceNames for the specified assembly from cache or directly.
        /// </summary>
        /// <param name="possibleResourceAssembly">Assembly</param>
        /// <returns>string array with resources names</returns>
        public string[] GetCachedManifestResourceNames(Assembly possibleResourceAssembly)
        {
            // Get the resources from the cache
            if (AssemblyResourceNames.TryGetValue(possibleResourceAssembly, out var manifestResourceNames))
            {
                return manifestResourceNames;
            }

            // If there was no cache, create it by retrieving the ManifestResourceNames for non dynamic assemblies
            manifestResourceNames = possibleResourceAssembly.IsDynamic ? new string[]{}: possibleResourceAssembly.GetManifestResourceNames();
            AssemblyResourceNames.Add(possibleResourceAssembly, manifestResourceNames);
            if (Log.IsVerboseEnabled() && manifestResourceNames.Length > 0)
            {
                Log.Verbose().WriteLine("Assembly {0} contains the following resources: {1}", possibleResourceAssembly.FullName, string.Join(", ", manifestResourceNames));
            }

            return manifestResourceNames;
        }

        /// <summary>
        /// Returns the fully qualified resource name of a resource
        /// </summary>
        /// <param name="type">The type whose namespace is used to scope the manifest resource name.</param>
        /// <param name="names">The case-sensitive name, parts, of the manifest resource being requested.</param>
        /// <returns>string</returns>
        public string Find(Type type, params string [] names)
        {
            var assemblyName = type.Assembly.GetName().Name;
            var assembly = _findAssembly(assemblyName);
            if (assembly == null)
            {
                return null;
            }

            var fqName = $"{type.Namespace}.{string.Join(".", names)}";

            var resources = GetCachedManifestResourceNames(assembly);

            return resources.Contains(fqName) ? fqName : null;
        }

        /// <summary>
        /// Get a resource as stream
        /// </summary>
        /// <param name="type">Type, used as the base to find the resource</param>
        /// <param name="names">string array, used to specify the location and name of the resource</param>
        /// <returns>Stream</returns>
        public Stream ResourceAsStream(Type type, params string [] names)
        {
            var assemblyName = type.Assembly.GetName().Name;
            var assembly = _findAssembly(assemblyName);
            if (assembly == null)
            {
                return null;
            }

            var name = string.Join(".", names);
            try
            {
                return assembly.GetManifestResourceStream(type, name);
            }
            catch (Exception ex)
            {
                Log.Error().WriteLine(ex, "Couldn't find resource {0} in the assembly {1} and namespace {2}", name, assemblyName, type.Namespace);
            }

            return null;
        }

        /// <summary>
        ///     Create a regex to find a resource in an assembly
        /// </summary>
        /// <param name="assembly">Assembly to look into</param>
        /// <param name="filePath">string with the filepath to find</param>
        /// <param name="ignoreCase">true, which is default, to ignore the case when comparing</param>
        /// <param name="alternativeExtensions">Besides the specified extension in the filePath, these are also allowed</param>
        /// <returns>Regex</returns>
        private Regex ResourceRegex(Assembly assembly, string filePath, bool ignoreCase = true, IEnumerable<string> alternativeExtensions = null)
        {
            // Resources don't have directory separators, they use ., fix this before creating a regex
            var resourcePath = filePath.Replace(Path.DirectorySeparatorChar, '.').Replace(Path.AltDirectorySeparatorChar, '.');
            // First get the extension to build the regex
            // TODO: this doesn't work 100% for double extensions like .png.gz etc but I will only fix this as soon as it's really needed
            var extensions = alternativeExtensions?.Concat(new[] {Path.GetExtension(filePath)}) ?? new[] {Path.GetExtension(filePath)};
            // Than get the filename without extension
            var filename = Path.GetFileNameWithoutExtension(resourcePath);
            // Assembly resources CAN have a prefix with the type namespace, use this instead of the default
            var prefix = $@"^({assembly.GetName().Name.Replace(".", @"\.")}\.)?";
            // build the regex
            return FileTools.FilenameToRegex(filename, extensions, ignoreCase, prefix);
        }

        /// <summary>
        ///     Get the stream for a assembly manifest resource based on the filePath
        ///     It will automatically uncompress if the file-ending is .gz or .compressed
        ///     Note: a GZipStream is not seekable, this might cause issues.
        /// </summary>
        /// <param name="filePath">string with the filepath to find</param>
        /// <param name="assembly">Assembly to look into</param>
        /// <param name="ignoreCase">true, which is default, to ignore the case when comparing</param>
        /// <returns>Stream for the filePath, or null if not found</returns>
        public Stream GetEmbeddedResourceAsStream(Assembly assembly, string filePath, bool ignoreCase = true)
        {
            if (assembly == null)
            {
                throw new ArgumentNullException(nameof(assembly));
            }
            if (string.IsNullOrEmpty(filePath))
            {
                throw new ArgumentNullException(nameof(filePath));
            }

            // build the regex
            var filePathRegex = ResourceRegex(assembly, filePath, ignoreCase);

            // Find the regex
            var resourceName = FindEmbeddedResources(assembly, filePathRegex).FirstOrDefault();
            if (resourceName != null)
            {
                if (Log.IsVerboseEnabled())
                {
                    Log.Verbose().WriteLine("Requested stream for path {0}, using resource {1} from assembly {2}", filePath, resourceName, assembly.FullName);
                }
                var resultStream = assembly.GetManifestResourceStream(resourceName);
                if (resultStream != null && resourceName.EndsWith(".gz"))
                {
                    resultStream = new GZipStream(resultStream, CompressionMode.Decompress);
                }

                if (resultStream != null && resourceName.EndsWith(".compressed", StringComparison.InvariantCultureIgnoreCase))
                {
                    resultStream = new DeflateStream(resultStream, CompressionMode.Decompress);
                }

                if (resultStream == null)
                {
                    Log.Warn().WriteLine("Couldn't get the resource stream for {0} from assembly {1}", resourceName, assembly.FullName);
                }
                return resultStream;
            }
            Log.Warn().WriteLine("Couldn't find the resource stream for path {0}, using regex pattern {1} from assembly {2}", filePath, filePathRegex, assembly.FullName);
            return null;
        }

        /// <summary>
        ///     Scan the manifest of the supplied Assembly with a regex pattern for embedded resources
        /// </summary>
        /// <param name="assembly">Assembly to scan</param>
        /// <param name="regexPattern">Regex pattern to scan for</param>
        /// <returns>IEnumerable with matching resource names</returns>
        public IEnumerable<string> FindEmbeddedResources(Assembly assembly, Regex regexPattern)
        {
            return from resourceName in GetCachedManifestResourceNames(assembly)
                where regexPattern.IsMatch(resourceName)
                select resourceName;
        }

        /// <summary>
        ///     Scan the manifest of the supplied Assembly elements with a regex pattern for embedded resources
        /// </summary>
        /// <param name="assemblies">IEnumerable with Assembly elements to scan</param>
        /// <param name="regex">Regex to scan for</param>
        /// <returns>IEnumerable with matching resource names</returns>
        public IEnumerable<Tuple<Assembly, string>> FindEmbeddedResources(IEnumerable<Assembly> assemblies, Regex regex)
        {
            return from assembly in assemblies
                from resourceName in GetCachedManifestResourceNames(assembly)
                where regex.IsMatch(resourceName)
                select new Tuple<Assembly, string>(assembly, resourceName);
        }


        /// <summary>
        ///     Returns the embedded resource, as specified in the Pack-Uri as a stream.
        ///     This currently doesn't go into the embedded .g.resources files, this might be added later
        /// </summary>
        /// <param name="packUri">Uri</param>
        /// <returns>Stream</returns>
        public Stream GetEmbeddedResourceAsStream(Uri packUri)
        {
            var match = packUri.PackUriMatch();

            var assemblyName = match.Groups["assembly"].Value;
            var assembly = _findAssembly(assemblyName);
            if (assembly == null)
            {
                throw new ArgumentException($"Pack uri references unknown assembly {assemblyName}.", nameof(packUri));
            }
            var path = match.Groups["path"].Value;
            return GetEmbeddedResourceAsStream(assembly, path);
        }

        /// <summary>
        ///     Test if there is an embedded resourcefor the Pack-Uri
        ///     This is work in progress, as most of the times the files are compiled from xaml to baml, and won't be recognized
        ///     when you specify a pack uri ending on .xaml
        /// </summary>
        /// <param name="packUri">Uri</param>
        /// <param name="ignoreCase">true to ignore the case</param>
        /// <returns>Stream</returns>
        public bool EmbeddedResourceExists(Uri packUri, bool ignoreCase = true)
        {
            var match = packUri.PackUriMatch();

            var assemblyName = match.Groups["assembly"].Value;

            var assembly = _findAssembly(assemblyName);
            if (assembly == null)
            {
                return false;
            }

            var path = match.Groups["path"].Value;

            var resourceRegex = ResourceRegex(assembly, path, ignoreCase);

            if (GetCachedManifestResourceNames(assembly).Any(name => resourceRegex.IsMatch(name)))
            {
                return true;
            }
            return HasEmbeddedDotResourcesResource(assembly, path);
        }

        /// <summary>
        ///     check if there is any resource in the specified assembly's .g.resources which matches the Regex
        ///     This is work in progress, as most of the times the files are compiled from xaml to baml, and won't be recognized
        ///     when you specify .xaml
        /// </summary>
        /// <param name="assembly">Assembly</param>
        /// <param name="filePath">filePath</param>
        /// <param name="ignoreCase">true to ignore the case</param>
        /// <returns>bool with true if there is a matching resource</returns>
        public bool HasEmbeddedDotResourcesResource(Assembly assembly, string filePath, bool ignoreCase = true)
        {
            var resourceNames = GetCachedManifestResourceNames(assembly);
            foreach (var resourcesFile in resourceNames.Where(x => x.EndsWith(".g.resources")))
            {
                Log.Verbose().WriteLine("Resource not directly found, trying {0}", resourcesFile);
                using (var resourceStream = GetEmbeddedResourceAsStream(assembly, resourcesFile))
                {
                    if (resourceStream == null)
                    {
                        continue;
                    }

                    using (var resourceReader = new ResourceReader(resourceStream))
                    {
                        // Check if it contains the filename
                        return resourceReader.OfType<DictionaryEntry>()
                               .Select(x => x.Key as string)
                               .Any(x => string.Equals(x, filePath, ignoreCase ? StringComparison.InvariantCultureIgnoreCase : StringComparison.InvariantCulture));
                    }
                }
            }
            return false;
        }

        /// <summary>
        ///     Scan the manifest of the supplied Assembly with a regex pattern for embedded resources
        /// </summary>
        /// <param name="assembly">Assembly to scan</param>
        /// <param name="regexPattern">Regex pattern to scan for</param>
        /// <param name="regexOptions">RegexOptions.IgnoreCase as default</param>
        /// <returns>IEnumerable with matching resource names</returns>
        public IEnumerable<string> FindEmbeddedResources(Assembly assembly, string regexPattern, RegexOptions regexOptions = RegexOptions.IgnoreCase)
        {
            return FindEmbeddedResources(assembly, new Regex(regexPattern, regexOptions));
        }

        /// <summary>
        ///     Scan the manifest of the Assembly of the supplied Type with a regex pattern for embedded resources
        /// </summary>
        /// <param name="type">Type is used to get the assembly </param>
        /// <param name="regexPattern">Regex pattern to scan for</param>
        /// <param name="regexOptions">RegexOptions.IgnoreCase as default</param>
        /// <returns>IEnumerable with matching resource names</returns>
        public IEnumerable<string> FindEmbeddedResources(Type type, string regexPattern, RegexOptions regexOptions = RegexOptions.IgnoreCase)
        {
            return FindEmbeddedResources(type.Assembly, regexPattern, regexOptions);
        }
    }
}