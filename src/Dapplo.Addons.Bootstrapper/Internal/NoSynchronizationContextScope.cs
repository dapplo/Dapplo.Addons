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
using System.Threading;

#endregion

namespace Dapplo.Addons.Bootstrapper.Internal
{
    /// <summary>
    ///     Create a scope in which code doesn't have a SynchronizationContext, dispose this to leave the scope
    ///     From answer to StackOverflow question:
    ///     http://stackoverflow.com/questions/28305968/use-task-run-in-synchronous-method-to-avoid-deadlock-waiting-on-async-method/28307965#28307965
    /// </summary>
    public sealed class NoSynchronizationContextScope : IDisposable
    {
        private readonly SynchronizationContext _synchronizationContext;

        /// <summary>
        ///     Create a scope in which code doesn't have a SynchronizationContext, dispose this to leave the scope
        /// </summary>
        public NoSynchronizationContextScope()
        {
            _synchronizationContext = SynchronizationContext.Current;
            SynchronizationContext.SetSynchronizationContext(null);
        }

        /// <summary>
        ///     Set the SynchronizationContext back, this "leaves" the "no synchronization context scope"
        /// </summary>
        public void Dispose()
        {
            SynchronizationContext.SetSynchronizationContext(_synchronizationContext);
        }
    }
}