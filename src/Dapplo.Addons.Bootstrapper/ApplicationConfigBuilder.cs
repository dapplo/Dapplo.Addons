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

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Text.RegularExpressions;
using Dapplo.Addons.Bootstrapper.Resolving;

namespace Dapplo.Addons.Bootstrapper
{
    /// <summary>
    /// This is a builder for the ApplicationConfig
    /// </summary>
    public class ApplicationConfigBuilder
    {
        private const string ApplicationconfigAlreadyBuild = "The ApplicationConfig was already build.";

        private readonly HashSet<string> _scanDirectories = new HashSet<string>(FileLocations.AssemblyResolveDirectories, StringComparer.OrdinalIgnoreCase);
        private readonly HashSet<string> _assemblyNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private readonly HashSet<Regex> _assemblyNamePatterns = new HashSet<Regex>();
        private readonly HashSet<string> _extensions = new HashSet<string>(new[] { ".dll", ".dll.compressed", ".dll.gz" }, StringComparer.OrdinalIgnoreCase);
        private readonly ApplicationConfig _applicationConfig = new ApplicationConfig();
        private readonly Dictionary<string, object> _properties = new Dictionary<string, object>();

        /// <summary>
        /// Container Properties
        /// </summary>
        public IDictionary<string, object> Properties => _properties;

        /// <summary>
        /// True if the ApplicationConfig was already build.
        /// </summary>
        public bool IsBuild { get; private set; }

        /// <summary>
        /// Private constructor, to prevent constructing this.
        /// Please use the Create factory method.
        /// </summary>
        private ApplicationConfigBuilder()
        {
        }

        /// <summary>
        /// Factory
        /// </summary>
        /// <returns>ApplicationConfig</returns>
        [Pure]
        public static ApplicationConfigBuilder Create() => new ApplicationConfigBuilder();

        /// <summary>
        /// Build or finalize the configuration, so it can be used
        /// </summary>
        /// <returns>ApplicationConfig</returns>
        [Pure]
        public ApplicationConfig BuildApplicationConfig()
        {
            if (IsBuild)
            {
                throw new NotSupportedException(ApplicationconfigAlreadyBuild);
            }
            IsBuild = true;

            // Create an application name, if there is none
            if (string.IsNullOrEmpty(_applicationConfig.ApplicationName))
            {
                using (var process = Process.GetCurrentProcess())
                {
                    _applicationConfig.ApplicationName = process.ProcessName;
                }
            }

            // Assign all values
            // TODO: When changing to 4.6 HashSet implements IReadOnlyCollection, so the extra ToList() can be removed
            _applicationConfig.Extensions = new ReadOnlyCollection<string>(_extensions.ToList());
            _applicationConfig.ScanDirectories = new ReadOnlyCollection<string>(_scanDirectories.ToList());
            _applicationConfig.AssemblyNames = new ReadOnlyCollection<string>(_assemblyNames.ToList());
            _applicationConfig.AssemblyNamePatterns = new ReadOnlyCollection<Regex>(_assemblyNamePatterns.ToList());
            _applicationConfig.Properties = _properties;
            return _applicationConfig;
        }

        /// <summary>
        /// Change the application name
        /// </summary>
        /// <param name="applicationName">string</param>
        /// <returns>ApplicationConfigBuilder</returns>
        public ApplicationConfigBuilder WithApplicationName(string applicationName)
        {
            if (IsBuild)
            {
                throw new NotSupportedException(ApplicationconfigAlreadyBuild);
            }
            _applicationConfig.ApplicationName = applicationName;
            return this;
        }

        /// <summary>
        /// Disable the embedded assembly scanning
        /// </summary>
        /// <returns>ApplicationConfigBuilder</returns>
        public ApplicationConfigBuilder WithoutScanningForEmbeddedAssemblies()
        {
            if (IsBuild)
            {
                throw new NotSupportedException(ApplicationconfigAlreadyBuild);
            }
            _applicationConfig.ScanForEmbeddedAssemblies = false;
            return this;
        }

        /// <summary>
        /// Disable the async loading of assemblies
        /// </summary>
        /// <returns>ApplicationConfigBuilder</returns>
        public ApplicationConfigBuilder WithoutAsyncAssemblyLoading()
        {
            if (IsBuild)
            {
                throw new NotSupportedException(ApplicationconfigAlreadyBuild);
            }
            _applicationConfig.UseAsyncAssemblyLoading = false;
            return this;
        }

        /// <summary>
        /// Disable the embedded assembly copying
        /// </summary>
        /// <returns>ApplicationConfigBuilder</returns>
        public ApplicationConfigBuilder WithoutCopyOfEmbeddedAssemblies()
        {
            if (IsBuild)
            {
                throw new NotSupportedException(ApplicationconfigAlreadyBuild);
            }
            _applicationConfig.CopyEmbeddedAssembliesToFileSystem = false;
            return this;
        }

        /// <summary>
        /// Disable the copying of assemblies to the probing path, this is risky as it could introduce assembly load context issues
        /// </summary>
        /// <returns>ApplicationConfigBuilder</returns>
        public ApplicationConfigBuilder WithoutCopyOfAssembliesToProbingPath()
        {
            if (IsBuild)
            {
                throw new NotSupportedException(ApplicationconfigAlreadyBuild);
            }
            _applicationConfig.CopyAssembliesToProbingPath = false;
            return this;
        }

        /// <summary>
        /// The extensions to use for loading the assemblies
        /// </summary>
        /// <param name="extensions">string with extension, can use multiple arguments</param>
        /// <returns>ApplicationConfigBuilder</returns>
        public ApplicationConfigBuilder WithExtensions(params string[] extensions)
        {
            if (IsBuild)
            {
                throw new NotSupportedException(ApplicationconfigAlreadyBuild);
            }
            if (extensions == null || extensions.Length == 0)
            {
                // Nothing to do
                return this;
            }
            foreach (var extension in extensions)
            {
                if (string.IsNullOrEmpty(extension))
                {
                    continue;
                }
                _extensions.Add(extension);
            }
            return this;
        }

        /// <summary>
        /// The extensions NOT to use for loading the assemblies, e.g. if you do not want to use .dll call this
        /// </summary>
        /// <param name="extensions">string with extension, can use multiple arguments, when null all are removed</param>
        /// <returns>ApplicationConfig</returns>
        public ApplicationConfigBuilder WithoutExtensions(params string[] extensions)
        {
            if (IsBuild)
            {
                throw new NotSupportedException(ApplicationconfigAlreadyBuild);
            }
            if (extensions == null || extensions.Length == 0)
            {
                _extensions.Clear();
                return this;
            }
            foreach (var extension in extensions)
            {
                if (string.IsNullOrEmpty(extension))
                {
                    continue;
                }
                _extensions.Remove(extension);
            }
            return this;
        }

        /// <summary>
        /// Specify that a mutex needs to be used
        /// </summary>
        /// <param name="mutex">string</param>
        /// <param name="global">bool specifying if the mutex if global or not, default is false</param>
        /// <returns>ApplicationConfigBuilder</returns>
        public ApplicationConfigBuilder WithMutex(string mutex, bool? global = false)
        {
            if (IsBuild)
            {
                throw new NotSupportedException(ApplicationconfigAlreadyBuild);
            }
            if (global.HasValue)
            {
                _applicationConfig.UseGlobalMutex = global.Value;
            }
            _applicationConfig.Mutex = mutex ?? throw new ArgumentNullException(nameof(mutex));
            return this;
        }

        /// <summary>
        /// Add scan directory or directories
        /// </summary>
        /// <param name="scanDirectories">string []</param>
        /// <returns>ApplicationConfigBuilder</returns>
        public ApplicationConfigBuilder WithScanDirectories(params string[] scanDirectories)
        {
            if (IsBuild)
            {
                throw new NotSupportedException(ApplicationconfigAlreadyBuild);
            }
            if (scanDirectories == null || scanDirectories.Length == 0)
            {
                return this;
            }

            foreach (var scanDirectory in scanDirectories)
            {
                if (string.IsNullOrEmpty(scanDirectory))
                {
                    continue;
                }
                var normalizedDirectory = FileTools.NormalizeDirectory(scanDirectory);
                _scanDirectories.Add(normalizedDirectory);
            }

            return this;
        }

        /// <summary>
        /// This is a shortcut to add the loading of the assembly Dapplo.Addons.Config, which enables Dapplo.Ini and Dapplo.Language
        /// These assemblies ofcourse needs to be available...
        /// </summary>
        /// <returns>ApplicationConfigBuilder</returns>
        public ApplicationConfigBuilder WithConfigSupport() => WithAssemblyNames("Dapplo.Addons.Config");

        /// <summary>
        /// Add assembly name(s)
        /// </summary>
        /// <param name="assemblyNames">string [] with the names of assemblies to load</param>
        /// <returns>ApplicationConfigBuilder</returns>
        public ApplicationConfigBuilder WithAssemblyNames(params string[] assemblyNames)
        {
            if (IsBuild)
            {
                throw new NotSupportedException(ApplicationconfigAlreadyBuild);
            }
            if (assemblyNames == null || assemblyNames.Length == 0)
            {
                return this;
            }

            foreach (var assemblyName in assemblyNames)
            {
                if (string.IsNullOrEmpty(assemblyName))
                {
                    continue;
                }
               _assemblyNames.Add(assemblyName);
            }

            return this;
        }

        /// <summary>
        /// Add assembly name patterns to scan for
        /// </summary>
        /// <param name="assemblyNamePatterns">string [] with the assembly name patterns</param>
        /// <returns>ApplicationConfigBuilder</returns>
        public ApplicationConfigBuilder WithAssemblyPatterns(params string[] assemblyNamePatterns)
        {
            if (IsBuild)
            {
                throw new NotSupportedException(ApplicationconfigAlreadyBuild);
            }
            if (assemblyNamePatterns == null || assemblyNamePatterns.Length == 0)
            {
                return this;
            }

            foreach (var assemblyNamePattern in assemblyNamePatterns)
            {
                if (string.IsNullOrEmpty(assemblyNamePattern))
                {
                    continue;
                }

                _assemblyNamePatterns.Add(new Regex(assemblyNamePattern.Replace(".", @"\.").Replace('?', '.').Replace("*", @"[^\\]*"), RegexOptions.IgnoreCase));
            }

            return this;
        }
    }
}
