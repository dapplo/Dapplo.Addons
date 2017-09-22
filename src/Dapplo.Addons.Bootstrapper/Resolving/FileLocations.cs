﻿#region Dapplo 2016 - GNU Lesser General Public License

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
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using Dapplo.Addons.Bootstrapper.Extensions;

#endregion

namespace Dapplo.Addons.Bootstrapper.Resolving
{
    /// <summary>
    ///     Some utils for managing the location of files
    /// </summary>
    public static class FileLocations
    {
        /// <summary>
        ///     Get the startup location, which is either the location of the entry assemby, or the executing assembly
        /// </summary>
        /// <returns>string with the directory of where the running code/applicationName was started</returns>
        public static string StartupDirectory => AppDomain.CurrentDomain.BaseDirectory;

        /// <summary>
        ///     Get the roaming AppData directory
        /// </summary>
        /// <returns>string with the directory the appdata roaming directory</returns>
        public static string RoamingAppDataDirectory(string applicationName)
        {
            return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), applicationName);
        }

        /// <summary>
        ///     Scan the supplied directories for files which match the passed file pattern
        /// </summary>
        /// <param name="directories"></param>
        /// <param name="filePattern">Regular expression for the filename</param>
        /// <param name="searchOption">
        ///     Makes it possible to specify if the search is recursive, SearchOption.AllDirectories is
        ///     default, use SearchOption.TopDirectoryOnly for non recursive
        /// </param>
        /// <returns>IEnumerable with paths</returns>
        public static IEnumerable<Tuple<string, Match>> Scan(IEnumerable<string> directories, Regex filePattern, SearchOption searchOption = SearchOption.AllDirectories)
        {
            return from directory in directories
                from path in DirectoriesFor(directory)
                where Directory.Exists(path)
                from file in Directory.EnumerateFiles(path, "*", searchOption)
                let match = filePattern.Match(file)
                where match.Success
                select Tuple.Create(file, match);
        }

        /// <summary>
        ///     Scan the supplied directories for files which match the passed file pattern
        /// </summary>
        /// <param name="directories">IEnumerable of string with the directories to scan</param>
        /// <param name="simplePattern"></param>
        /// <param name="searchOption">
        ///     Makes it possible to specify if the search is recursive, SearchOption.AllDirectories is
        ///     default, use SearchOption.TopDirectoryOnly for non recursive
        /// </param>
        /// <returns>IEnumerable with paths</returns>
        public static IEnumerable<string> Scan(IEnumerable<string> directories, string simplePattern, SearchOption searchOption = SearchOption.AllDirectories)
        {
            return from directory in directories
                from path in DirectoriesFor(directory)
                where Directory.Exists(path)
                from file in Scan(path, simplePattern, searchOption)
                select file;
        }

        /// <summary>
        ///     Scan the supplied directories for files which match the passed file pattern
        /// </summary>
        /// <param name="directory">string with the directory to scan</param>
        /// <param name="simplePattern">pattern</param>
        /// <param name="searchOption">
        ///     Makes it possible to specify if the search is recursive, SearchOption.AllDirectories is
        ///     default, use SearchOption.TopDirectoryOnly for non recursive
        /// </param>
        /// <returns>IEnumerable with file paths</returns>
        public static IEnumerable<string> Scan(string directory, string simplePattern, SearchOption searchOption = SearchOption.AllDirectories)
        {
            return Directory.EnumerateFiles(directory, simplePattern, searchOption);
        }

        /// <summary>
        ///     For the given directory this will return possible location.
        ///     It might be that multiple are returned, also normalization is made
        /// </summary>
        /// <param name="directory">A absolute or relative directory</param>
        /// <param name="allowCurrentDirectory">true to allow relative to current working directory</param>
        /// <returns>IEnumerable with possible directories</returns>
        public static IEnumerable<string> DirectoriesFor(string directory, bool allowCurrentDirectory = true)
        {
            var directories = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            // If the path is rooted, it's absolute
            if (Path.IsPathRooted(directory))
            {
                directories.Add(NormalizeDirectory(directory));
            }
            else
            {
                // Relative to the assembly location
                directories.Add(DirectoryRelativeToExe(directory));

                // Relative to the current working directory
                if (allowCurrentDirectory)
                {
                    directories.Add(DirectoryRelativeToCurrentWorkingDirectory(directory));
                }
            }
            return directories.Where(dir => !string.IsNullOrEmpty(dir) && Directory.Exists(dir)).OrderBy(dir => dir);
        }

        /// <summary>
        ///     A simple helper to normalize a directory name
        /// </summary>
        /// <param name="directory"></param>
        /// <returns>normalized directory name</returns>
        public static string NormalizeDirectory(string directory)
        {
            try
            {
                return Path.GetFullPath(new Uri(directory, UriKind.Absolute).LocalPath).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            }
            catch
            {
                // Do nothing
            }
            return null;
        }

        /// <summary>
        ///     Helper method which returns the directory relative to the current directory
        /// </summary>
        /// <param name="directory">directory name</param>
        /// <returns>string directory</returns>
        private static string DirectoryRelativeToCurrentWorkingDirectory(string directory)
        {
            try
            {
                return Path.GetFullPath(Path.Combine(Environment.CurrentDirectory, directory)).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            }
            catch
            {
                // Do nothing
            }
            return null;
        }

        /// <summary>
        ///     Helper method which returns the directory relative to the exe
        /// </summary>
        /// <param name="directory">directory name</param>
        /// <returns>string directory</returns>
        private static string DirectoryRelativeToExe(string directory)
        {
            try
            {
                var assemblyLocation = Assembly.GetExecutingAssembly().GetLocation();
                if (!string.IsNullOrEmpty(assemblyLocation) && File.Exists(assemblyLocation))
                {
                    var exeDirectory = Path.GetDirectoryName(assemblyLocation);
                    if (!string.IsNullOrEmpty(exeDirectory) && exeDirectory != Environment.CurrentDirectory)
                    {
                        var relativeToExe = Path.GetFullPath(Path.Combine(exeDirectory, directory)).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                        return relativeToExe;
                    }
                }
            }
            catch
            {
                // Do nothing
            }
            return null;
        }
    }
}