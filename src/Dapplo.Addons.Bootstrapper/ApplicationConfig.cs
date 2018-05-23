#region Dapplo 2016-2018 - GNU Lesser General Public License

// Dapplo - building blocks for .NET applications
// Copyright (C) 2016-2018 Dapplo
// 
// For more information see: http://dapplo.net/
// Dapplo repositories are hosted on GitHub: https://github.com/dapplo
// 
// This file is part of Dapplo.CaliburnMicro
// 
// Dapplo.CaliburnMicro is free software: you can redistribute it and/or modify
// it under the terms of the GNU Lesser General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// Dapplo.CaliburnMicro is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU Lesser General Public License for more details.
// 
// You should have a copy of the GNU Lesser General Public License
// along with Dapplo.CaliburnMicro. If not, see <http://www.gnu.org/licenses/lgpl.txt>.

#endregion

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text.RegularExpressions;
using Dapplo.Addons.Bootstrapper.Resolving;

namespace Dapplo.Addons.Bootstrapper
{
    /// <summary>
    /// This specifies the configuration for the ApplicationBootstrapper
    /// </summary>
    public class ApplicationConfig
    {
        private readonly ISet<string> _scanDirectories;
        private readonly ISet<string> _assemblyNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private readonly ISet<Regex> _assemblyNamePatterns = new HashSet<Regex>();
        private readonly ISet<string> _extensions = new HashSet<string>(new []{".dll", ".dll.compressed", ".dll.gz"}, StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Setup all defaults
        /// </summary>
        private ApplicationConfig()
        {
            // Create the set of scan directories, including the directories which are used anyway
            _scanDirectories = new HashSet<string>(FileLocations.AssemblyResolveDirectories, StringComparer.OrdinalIgnoreCase);

            // set the process name to be the name of the application
            using (var process = Process.GetCurrentProcess())
            {
                ApplicationName = process.ProcessName;
            }
        }

        /// <summary>
        /// Factory method
        /// </summary>
        /// <returns></returns>
        public static ApplicationConfig Create()
        {
            return new ApplicationConfig();
        }

        /// <summary>
        /// Specifies if the application bootstrapper should scan embedded assemblies
        /// </summary>
        public bool ScanForEmbeddedAssemblies { get; private set; } = true;

        /// <summary>
        /// Specifies if the application bootstrapper copy embedded assemblies to the file system
        /// </summary>
        public bool CopyEmbeddedAssembliesToFileSystem { get; private set; } = true;

        /// <summary>
        /// The directories to scan for addons
        /// </summary>
        public IEnumerable<string> ScanDirectories => _scanDirectories;

        /// <summary>
        /// The names of assemblies to load
        /// </summary>
        public IEnumerable<string> AssemblyNames => _assemblyNames;

        /// <summary>
        /// The patterns of assembly names to load
        /// </summary>
        public IEnumerable<Regex> AssemblyNamePatterns => _assemblyNamePatterns;

        /// <summary>
        /// The allowed assembly extensions to load, default .dll
        /// </summary>
        public IEnumerable<string> Extensions => _extensions;

        /// <summary>
        /// The name of the application
        /// </summary>
        public string ApplicationName { get; private set; }

        /// <summary>
        /// The id of the mutex, if any
        /// </summary>
        public string Mutex { get; private set; }

        /// <summary>
        /// Test if a mutex is set
        /// </summary>
        public bool HasMutex => !string.IsNullOrEmpty(Mutex);

        /// <summary>
        /// Specify if the mutex is global
        /// </summary>
        public bool UseGlobalMutex { get; private set; } = true;

        /// <summary>
        /// Change the application name
        /// </summary>
        /// <param name="applicationName">string</param>
        /// <returns>ApplicationConfig</returns>
        public ApplicationConfig WithApplicationName(string applicationName)
        {
            ApplicationName = applicationName;
            return this;
        }

        /// <summary>
        /// Disable the embedded assembly scanning
        /// </summary>
        /// <returns>ApplicationConfig</returns>
        public ApplicationConfig WithoutScanningForEmbeddedAssemblies()
        {
            ScanForEmbeddedAssemblies = false;
            return this;
        }

        /// <summary>
        /// Disable the embedded assembly copying
        /// </summary>
        /// <returns>ApplicationConfig</returns>
        public ApplicationConfig WithoutCopyOfEmbeddedAssemblies()
        {
            CopyEmbeddedAssembliesToFileSystem = false;
            return this;
        }

        /// <summary>
        /// The extensions to use for loading the assemblies
        /// </summary>
        /// <param name="extensions">string with extension, can use multiple arguments</param>
        /// <returns>ApplicationConfig</returns>
        public ApplicationConfig WithExtensions(params string [] extensions)
        {
            if (extensions == null || extensions.Length == 0)
            {
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
        public ApplicationConfig WithoutExtensions(params string[] extensions)
        {
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
        /// <param name="global">bool specifying if the mutex if global or not</param>
        /// <returns>ApplicationConfig</returns>
        public ApplicationConfig WithMutex(string mutex, bool? global = true)
        {
            Mutex = mutex ?? throw new ArgumentNullException(nameof(mutex));
            return this;
        }

        /// <summary>
        /// Add scan directory or directories
        /// </summary>
        /// <param name="scanDirectories">string []</param>
        public ApplicationConfig WithScanDirectories(params string[] scanDirectories)
        {
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
        /// Add assembly name(s)
        /// </summary>
        /// <param name="assemblyNames">string [] with the names of assemblies to load</param>
        public ApplicationConfig WithAssemblyNames(params string[] assemblyNames)
        {
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
        public ApplicationConfig WithAssemblyPatterns(params string[] assemblyNamePatterns)
        {
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
