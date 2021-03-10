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

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Dapplo.Addons.Bootstrapper.Resolving;

namespace Dapplo.Addons.Bootstrapper.Internal
{
    /// <summary>
    /// Information on the location of an assembly
    /// </summary>
    public class AssemblyLocationInformation
    {
        private bool? _isOnProbingPath;

        /// <summary>
        /// Specifies if the assembly is embedded
        /// </summary>
        public bool IsEmbedded { get; }

        /// <summary>
        /// Assembly name
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// IS the Assembly containing the assembly
        /// </summary>
        public Assembly ContainingAssembly{ get; }

        /// <summary>
        /// Both in the containing assembly, as on the file system
        /// </summary>
        public string Filename { get; }

        /// <summary>
        /// This is the last write time of the assembly on the disk, or the containing assembly for embedded assemblies.
        /// </summary>
        public DateTime FileDate
        {
            get
            {
                if (IsEmbedded)
                {
                    var location = !string.IsNullOrEmpty(ContainingAssembly.Location) ? ContainingAssembly.Location : Assembly.GetEntryAssembly()?.Location;
                    if (location != null)
                    {
                        return File.GetLastWriteTime(location);
                    }
                }

                return File.GetLastWriteTime(Filename);
            }
        }

        /// <summary>
        /// Constructor for an embedded assembly location information
        /// </summary>
        /// <param name="name">string with name of the assembly</param>
        /// <param name="containingAssembly">Assembly which contains the assembly</param>
        /// <param name="resourceName">string with the resource name</param>
        public AssemblyLocationInformation(string name, Assembly containingAssembly, string resourceName)
        {
            Name = name;
            IsEmbedded = true;
            ContainingAssembly = containingAssembly;
            Filename = resourceName;
        }

        /// <summary>
        /// Constructor for a disk located assembly location information
        /// </summary>
        /// <param name="name">string with the assembly name</param>
        /// <param name="resourceName">string with the filename</param>
        public AssemblyLocationInformation(string name, string resourceName)
        {
            Name = name;
            IsEmbedded = false;
            Filename = resourceName;
        }

        /// <summary>
        /// Checks if the file is on the probingpath
        /// </summary>
        public bool IsOnProbingPath
        {
            get {
                if (!_isOnProbingPath.HasValue)
                {
                    _isOnProbingPath = !string.IsNullOrEmpty(Filename) && FileLocations.AssemblyResolveDirectories.Contains(Path.GetDirectoryName(Filename));
                }
                return _isOnProbingPath.Value;
            }
        }


        /// <inheritdoc />
        public override bool Equals(object obj)
        {
            return obj is AssemblyLocationInformation information &&
                   IsEmbedded == information.IsEmbedded &&
                   Name == information.Name &&
                   EqualityComparer<Assembly>.Default.Equals(ContainingAssembly, information.ContainingAssembly) &&
                   Filename == information.Filename;
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = 810003866;
                hashCode = hashCode * -1521134295 + IsEmbedded.GetHashCode();
                hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(Name);
                hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(ContainingAssembly?.FullName);
                hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(Filename);
                return hashCode;
            }
        }

        /// <inheritdoc />
        public override string ToString()
        {
            if (IsEmbedded)
            {
                return $"{Name} - {ContainingAssembly.FullName}:{Filename}";
            }
            return $"{Name} - {Filename}";
        }
    }
}
