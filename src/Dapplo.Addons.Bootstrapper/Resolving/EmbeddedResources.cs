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
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Resources;
using System.Text.RegularExpressions;
using Dapplo.Log;

#endregion

namespace Dapplo.Addons.Bootstrapper.Resolving
{
    /// <summary>
    ///     Utilities for embedded resources
    /// </summary>
    public static class EmbeddedResources
    {
        private static readonly Regex PackRegex = new Regex(@"/(?<assembly>[a-zA-Z\.]+);component/(?<path>.*)", RegexOptions.Compiled);
        private static readonly LogSource Log = new LogSource();

        /// <summary>
        ///     Create a regex to find a resource in an assembly
        /// </summary>
        /// <param name="filePath">string with the filepath to find</param>
        /// <param name="assembly">Assembly to look into</param>
        /// <param name="ignoreCase">true, which is default, to ignore the case when comparing</param>
        /// <param name="alternativeExtensions">Besides the specified extension in the filePath, these are also allowed</param>
        /// <returns>Regex</returns>
        private static Regex ResourceRegex(this Assembly assembly, string filePath, bool ignoreCase = true, IEnumerable<string> alternativeExtensions = null)
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
        ///     It will automatically wrapped as GZipStream if the file-ending is .gz
        ///     Note: a GZipStream is not seekable, this might cause issues.
        /// </summary>
        /// <param name="filePath">string with the filepath to find</param>
        /// <param name="assembly">Assembly to look into</param>
        /// <param name="ignoreCase">true, which is default, to ignore the case when comparing</param>
        /// <returns>Stream for the filePath, or null if not found</returns>
        public static Stream GetEmbeddedResourceAsStream(this Assembly assembly, string filePath, bool ignoreCase = true)
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
            var filePathRegex = assembly.ResourceRegex(filePath, ignoreCase);

            // Find the regex
            var resourceName = assembly.FindEmbeddedResources(filePathRegex).FirstOrDefault();
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
        /// <param name="regexOptions">RegexOptions.IgnoreCase as default</param>
        /// <returns>IEnumerable with matching resource names</returns>
        public static IEnumerable<string> FindEmbeddedResources(this Assembly assembly, string regexPattern, RegexOptions regexOptions = RegexOptions.IgnoreCase)
        {
            return assembly.FindEmbeddedResources(new Regex(regexPattern, regexOptions));
        }

        /// <summary>
        ///     check if there is any resource in the specified assembly which matches the Regex
        /// </summary>
        /// <param name="assembly">Assembly</param>
        /// <param name="regexPattern">Regex</param>
        /// <returns>bool with true if there is a matching resource</returns>
        public static bool HasResource(this Assembly assembly, Regex regexPattern)
        {
            var resourceNames = assembly.GetManifestResourceNames();
            return resourceNames.Any(regexPattern.IsMatch);
        }

        /// <summary>
        ///     Scan the manifest of the supplied Assembly with a regex pattern for embedded resources
        /// </summary>
        /// <param name="assembly">Assembly to scan</param>
        /// <param name="regexPattern">Regex pattern to scan for</param>
        /// <returns>IEnumerable with matching resource names</returns>
        public static IEnumerable<string> FindEmbeddedResources(this Assembly assembly, Regex regexPattern)
        {
            return from resourceName in assembly.GetManifestResourceNames()
                where regexPattern.IsMatch(resourceName)
                select resourceName;
        }

        /// <summary>
        ///     Scan the manifest of the supplied Assembly elements with a regex pattern for embedded resources
        /// </summary>
        /// <param name="assemblies">IEnumerable with Assembly elements to scan</param>
        /// <param name="regexPattern">Regex pattern to scan for</param>
        /// <param name="regexOptions">RegexOptions.IgnoreCase as default</param>
        /// <returns>IEnumerable with matching resource names</returns>
        public static IEnumerable<Tuple<Assembly, string>> FindEmbeddedResources(this IEnumerable<Assembly> assemblies, string regexPattern,
            RegexOptions regexOptions = RegexOptions.IgnoreCase)
        {
            return assemblies.FindEmbeddedResources(new Regex(regexPattern, regexOptions));
        }

        /// <summary>
        ///     Scan the manifest of the supplied Assembly elements with a regex pattern for embedded resources
        /// </summary>
        /// <param name="assemblies">IEnumerable with Assembly elements to scan</param>
        /// <param name="regex">Regex to scan for</param>
        /// <returns>IEnumerable with matching resource names</returns>
        public static IEnumerable<Tuple<Assembly, string>> FindEmbeddedResources(this IEnumerable<Assembly> assemblies, Regex regex)
        {
            return from assembly in assemblies
                from resourceName in assembly.GetManifestResourceNames()
                where regex.IsMatch(resourceName)
                select new Tuple<Assembly, string>(assembly, resourceName);
        }

        /// <summary>
        ///     Helper method to create a regex match for the supplied Pack uri
        /// </summary>
        /// <param name="packUri">Uri</param>
        /// <returns>Match</returns>
        public static Match PackUriMatch(this Uri packUri)
        {
            if (packUri == null)
            {
                throw new ArgumentNullException(nameof(packUri));
            }
            if (!"pack".Equals(packUri.Scheme))
            {
                throw new ArgumentException("Scheme is not pack:", nameof(packUri));
            }
            if (!"application:,,,".Equals(packUri.Host))
            {
                throw new ArgumentException("pack uri is not for application", nameof(packUri));
            }
            var match = PackRegex.Match(packUri.AbsolutePath);
            if (!match.Success)
            {
                throw new ArgumentException("pack uri isn't correctly formed.", nameof(packUri));
            }
            return match;
        }

        /// <summary>
        ///     Returns the embedded resource, as specified in the Pack-Uri as a stream.
        ///     This currently doesn't go into the embedded .g.resources files, this might be added later
        /// </summary>
        /// <param name="packUri">Uri</param>
        /// <returns>Stream</returns>
        public static Stream GetEmbeddedResourceAsStream(this Uri packUri)
        {
            var match = packUri.PackUriMatch();

            var assemblyName = match.Groups["assembly"].Value;
            var assembly = AssemblyResolver.FindAssembly(assemblyName);
            if (assembly == null)
            {
                throw new ArgumentException($"Pack uri references unknown assembly {assemblyName}.", nameof(packUri));
            }
            var path = match.Groups["path"].Value;
            return assembly.GetEmbeddedResourceAsStream(path);
        }

        /// <summary>
        ///     Test if there is an embedded resourcefor the Pack-Uri
        ///     This is work in progress, as most of the times the files are compiled from xaml to baml, and won't be recognized
        ///     when you specify a pack uri ending on .xaml
        /// </summary>
        /// <param name="packUri">Uri</param>
        /// <param name="ignoreCase">true to ignore the case</param>
        /// <returns>Stream</returns>
        public static bool EmbeddedResourceExists(this Uri packUri, bool ignoreCase = true)
        {
            var match = packUri.PackUriMatch();

            var assemblyName = match.Groups["assembly"].Value;

            var assembly = AssemblyResolver.FindAssembly(assemblyName);
            if (assembly == null)
            {
                return false;
            }

            var path = match.Groups["path"].Value;

            var resourceRegex = assembly.ResourceRegex(path, ignoreCase);

            if (assembly.HasResource(resourceRegex))
            {
                return true;
            }
            return assembly.HasEmbeddedDotResourcesResource(path);
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
        public static bool HasEmbeddedDotResourcesResource(this Assembly assembly, string filePath, bool ignoreCase = true)
        {
            var resourceNames = assembly.GetManifestResourceNames();
            foreach (var resourcesFile in resourceNames.Where(x => x.EndsWith(".g.resources")))
            {
                Log.Verbose().WriteLine("Resource not directly found, trying {0}", resourcesFile);
                using (var resourceStream = assembly.GetEmbeddedResourceAsStream(resourcesFile))
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
        ///     Get the stream for the calling assembly from the manifest resource based on the filePath
        /// </summary>
        /// <param name="filePath">string with the filepath to find</param>
        /// <param name="ignoreCase">true, which is default, to ignore the case when comparing</param>
        /// <returns>Stream for the filePath, or null if not found</returns>
        public static Stream GetEmbeddedResourceAsStream(string filePath, bool ignoreCase = true)
        {
            return Assembly.GetCallingAssembly().GetEmbeddedResourceAsStream(filePath, ignoreCase);
        }

        /// <summary>
        ///     Scan the manifest of the calling Assembly with a regex pattern for embedded resources
        /// </summary>
        /// <param name="regexPattern">Regex pattern to scan for</param>
        /// <param name="regexOptions">RegexOptions.IgnoreCase as default</param>
        /// <returns>IEnumerable with matching resource names</returns>
        public static IEnumerable<string> FindEmbeddedResources(string regexPattern, RegexOptions regexOptions = RegexOptions.IgnoreCase)
        {
            var assembly = Assembly.GetCallingAssembly();
            return assembly.FindEmbeddedResources(regexPattern, regexOptions);
        }

        /// <summary>
        ///     Scan the manifest of all assemblies in the AppDomain with a regex pattern for embedded resources
        ///     Usually this would be used with AppDomain.Current
        /// </summary>
        /// <param name="appDomain">AppDomain to scan</param>
        /// <param name="regexPattern">Regex pattern to scan for</param>
        /// <param name="regexOptions">RegexOptions.IgnoreCase as default</param>
        /// <returns>IEnumerable with matching assembly resource name tuples</returns>
        public static IEnumerable<Tuple<Assembly, string>> FindEmbeddedResources(this AppDomain appDomain, string regexPattern,
            RegexOptions regexOptions = RegexOptions.IgnoreCase)
        {
            return appDomain.GetAssemblies().FindEmbeddedResources(regexPattern, regexOptions);
        }

        /// <summary>
        ///     Scan the manifest of the Assembly of the supplied Type with a regex pattern for embedded resources
        /// </summary>
        /// <param name="type">Type is used to get the assembly </param>
        /// <param name="regexPattern">Regex pattern to scan for</param>
        /// <param name="regexOptions">RegexOptions.IgnoreCase as default</param>
        /// <returns>IEnumerable with matching resource names</returns>
        public static IEnumerable<string> FindEmbeddedResources(this Type type, string regexPattern, RegexOptions regexOptions = RegexOptions.IgnoreCase)
        {
            return type.Assembly.FindEmbeddedResources(regexPattern, regexOptions);
        }

        /// <summary>
        ///     Scan the manifest of the Assembly of the supplied Type with a regex pattern for embedded resources
        /// </summary>
        /// <param name="type">Type is used to get the assembly </param>
        /// <param name="regex">Regex to scan for</param>
        /// <returns>IEnumerable with matching resource names</returns>
        public static IEnumerable<string> FindEmbeddedResources(this Type type, Regex regex)
        {
            return type.Assembly.FindEmbeddedResources(regex);
        }
    }
}