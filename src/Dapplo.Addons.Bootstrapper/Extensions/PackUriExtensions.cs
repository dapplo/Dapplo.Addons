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
using System.Text.RegularExpressions;

namespace Dapplo.Addons.Bootstrapper.Extensions
{
    /// <summary>
    /// Extensions for Uri with the schema pack
    /// </summary>
    public static class PackUriExtensions
    {
        private static readonly Regex ApplicationPackRegex = new Regex(@"pack://application:,,,/(?<assembly>[^;]+);component/(?<path>.*)", RegexOptions.Compiled);
        
        /// <summary>
        ///     Validate PackUri
        /// </summary>
        /// <param name="packUri">Uri</param>
        /// <returns>bool true if wellformed</returns>
        public static bool IsWellformedApplicationPackUri(this Uri packUri)
        {
            if (packUri == null)
            {
                throw new ArgumentNullException(nameof(packUri));
            }
            return ApplicationPackRegex.IsMatch(packUri.AbsoluteUri);
        }

        /// <summary>
        ///     Helper method to create a regex match for the supplied Pack uri
        /// </summary>
        /// <param name="packUri">Uri</param>
        /// <returns>Match</returns>
        public static Match ApplicationPackUriMatch(this Uri packUri)
        {
            if (packUri == null)
            {
                throw new ArgumentNullException(nameof(packUri));
            }
            var match = ApplicationPackRegex.Match(packUri.AbsoluteUri);
            if (!match.Success)
            {
                throw new ArgumentException($"pack uri {packUri.AbsoluteUri} isn't correctly formed.", nameof(packUri));
            }
            return match;
        }
    }
}
