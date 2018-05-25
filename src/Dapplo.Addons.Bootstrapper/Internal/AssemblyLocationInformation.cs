using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Dapplo.Addons.Bootstrapper.Resolving;

namespace Dapplo.Addons.Bootstrapper.Internal
{
    /// <summary>
    /// 
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
                    return File.GetLastWriteTime(!string.IsNullOrEmpty(ContainingAssembly.Location) ? ContainingAssembly.Location : Assembly.GetEntryAssembly().Location);
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
            var hashCode = 810003866;
            hashCode = hashCode * -1521134295 + IsEmbedded.GetHashCode();
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(Name);
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(ContainingAssembly?.FullName);
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(Filename);
            return hashCode;
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
