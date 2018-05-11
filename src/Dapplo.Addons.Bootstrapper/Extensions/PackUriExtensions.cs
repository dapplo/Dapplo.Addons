using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Dapplo.Addons.Bootstrapper.Extensions
{
    /// <summary>
    /// Extensions for Uri with the schema pack
    /// </summary>
    public static class PackUriExtensions
    {
        private static readonly Regex PackRegex = new Regex(@"/(?<assembly>[a-zA-Z\.]+);component/(?<path>.*)", RegexOptions.Compiled);

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
    }
}
