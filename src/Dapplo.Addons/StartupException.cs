#region Dapplo 2016-2017 - GNU Lesser General Public License

// Dapplo - building blocks for .NET applications
// Copyright (C) 2016-2017 Dapplo
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
using System.Runtime.Serialization;

#endregion

namespace Dapplo.Addons
{
    /// <summary>
    ///     If this exception is thrown by an startup action, the startup of your application will be terminated.
    ///     A prerequisite is that your class has the StartupActionAttribute where AwaitStart is true (this is default)
    /// </summary>
    [Serializable]
    public class StartupException : Exception
    {
        /// <summary>
        /// Make serializazion possible
        /// </summary>
        /// <param name="info"></param>
        /// <param name="context"></param>
        protected StartupException(SerializationInfo info, StreamingContext context) 
            : base(info, context)
        {
        }

        /// <summary>
        ///     Create a StartupException with a message
        /// </summary>
        /// <param name="message">string</param>
        public StartupException(string message) : base(message)
        {
        }

        /// <summary>
        ///     Create a StartupException with a message and a cause
        /// </summary>
        /// <param name="message">string</param>
        /// <param name="innerException">Exception which caused the StartupException</param>
        public StartupException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}