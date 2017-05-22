#region Dapplo 2016 - GNU Lesser General Public License

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

#endregion

namespace Dapplo.Addons.Bootstrapper.Internal
{
    /// <summary>
    ///     A simple way to return something, which calls an action on Dispose.
    /// </summary>
    internal class SimpleDisposable : IDisposable
    {
        private readonly Action _action;
        // To detect redundant calls, we store a flag
        private bool _disposed;

        private SimpleDisposable(Action action)
        {
            _action = action;
        }

        /// <summary>
        ///     Dispose will call the stored action
        /// </summary>
        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }
            _disposed = true;
            _action();
        }

        /// <summary>
        ///     Create an IDisposable which will call the passed action on Dispose.
        /// </summary>
        /// <param name="action">Action to call when the object is disposed.</param>
        /// <returns>IDisposable</returns>
        public static IDisposable Create(Action action)
        {
            return new SimpleDisposable(action);
        }
    }
}