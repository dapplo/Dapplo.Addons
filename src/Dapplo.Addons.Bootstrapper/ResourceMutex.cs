﻿// Dapplo - building blocks for .NET applications
// Copyright (C) 2016-2021 Dapplo
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

#if !NETSTANDARD2_0
using System.Security.AccessControl;
using System.Security.Principal;
#endif

using System.Threading;
using Dapplo.Log;

namespace Dapplo.Addons.Bootstrapper
{
    /// <summary>
    ///     This protects your resources or application from running more than once
    ///     Simplifies the usage of the Mutex class, as described here:
    ///     https://msdn.microsoft.com/en-us/library/System.Threading.Mutex.aspx
    /// </summary>
    public sealed class ResourceMutex : IDisposable
    {
        // Special case, the _log is not readonly
        private static readonly LogSource Log = new LogSource();

        private readonly string _mutexId;
        private readonly string _resourceName;
        private Mutex _applicationMutex;

        /// <summary>
        ///     Private constructor
        /// </summary>
        /// <param name="mutexId">string with a unique Mutex ID</param>
        /// <param name="resourceName">optional name for the resource</param>
        private ResourceMutex(string mutexId, string resourceName = null)
        {
            _mutexId = mutexId;
            _resourceName = resourceName ?? mutexId;
        }

        /// <summary>
        ///     Test if the Mutex was created and locked.
        /// </summary>
        public bool IsLocked { get; set; }

        /// <summary>
        ///     Create a ResourceMutex for the specified mutex id and resource-name
        /// </summary>
        /// <param name="mutexId">ID of the mutex, preferably a Guid as string</param>
        /// <param name="resourceName">Name of the resource to lock, e.g your application name, usefull for logs</param>
        /// <param name="global">true to have a global mutex see: https://msdn.microsoft.com/en-us/library/bwe34f1k.aspx </param>
        public static ResourceMutex Create(string mutexId, string resourceName = null, bool global = false)
        {
            if (mutexId == null)
            {
                throw new ArgumentNullException(nameof(mutexId));
            }
            var resourceMutex = new ResourceMutex((global ? @"Global\" : @"Local\") + mutexId, resourceName);
            resourceMutex.Lock();
            return resourceMutex;
        }

        /// <summary>
        ///     This tries to get the Mutex, which takes care of having multiple instances running
        /// </summary>
        /// <returns>true if it worked, false if another instance is already running or something went wrong</returns>
        public bool Lock()
        {
            if (Log.IsVerboseEnabled())
            {
                Log.Verbose().WriteLine("{0} is trying to get Mutex {1} on Thread {2}", _resourceName, _mutexId, Thread.CurrentThread.ManagedThreadId);
            }

            IsLocked = true;
            // check whether there's an local instance running already, but use local so this works in a multi-user environment
            try
            {
#if NET471
                // Added Mutex Security, hopefully this prevents the UnauthorizedAccessException more gracefully
                var sid = new SecurityIdentifier(WellKnownSidType.WorldSid, null);
                var mutexsecurity = new MutexSecurity();
                mutexsecurity.AddAccessRule(new MutexAccessRule(sid, MutexRights.FullControl, AccessControlType.Allow));
                mutexsecurity.AddAccessRule(new MutexAccessRule(sid, MutexRights.ChangePermissions, AccessControlType.Deny));
                mutexsecurity.AddAccessRule(new MutexAccessRule(sid, MutexRights.Delete, AccessControlType.Deny));

                // 1) Create Mutex
                _applicationMutex = new Mutex(true, _mutexId, out var createdNew, mutexsecurity);
#else
                // 1) Create Mutex
                _applicationMutex = new Mutex(true, _mutexId, out var createdNew);
#endif
                // 2) if the mutex wasn't created new get the right to it, this returns false if it's already locked
                if (!createdNew)
                {
                    IsLocked = _applicationMutex.WaitOne(2000, false);
                    if (!IsLocked)
                    {
                        Log.Warn().WriteLine("Mutex {0} is already in use and couldn't be locked for the caller {1}", _mutexId, _resourceName);
                        // Clean up
                        _applicationMutex.Dispose();
                        _applicationMutex = null;
                    }
                    else
                    {
                        Log.Info().WriteLine("{0} has claimed the mutex {1}", _resourceName, _mutexId);
                    }
                }
                else
                {
                    Log.Info().WriteLine("{0} has created & claimed the mutex {1}", _resourceName, _mutexId);
                }
            }
            catch (AbandonedMutexException e)
            {
                // Another instance didn't cleanup correctly!
                // we can ignore the exception, it happend on the "waitone" but still the mutex belongs to us
                Log.Warn().WriteLine(e, "{0} didn't cleanup correctly, but we got the mutex {1}.", _resourceName, _mutexId);
            }
            catch (UnauthorizedAccessException e)
            {
                Log.Error().WriteLine(e, "{0} is most likely already running for a different user in the same session, can't create/get mutex {1} due to error.",
                    _resourceName, _mutexId);
                IsLocked = false;
            }
            catch (Exception ex)
            {
                Log.Error().WriteLine(ex, "Problem obtaining the Mutex {1} for {0}, assuming it was already taken!", _resourceName, _mutexId);
                IsLocked = false;
            }
            return IsLocked;
        }

        //  To detect redundant Dispose calls
        private bool _disposedValue;

        /// <summary>
        ///     Dispose the application mutex
        /// </summary>
        public void Dispose()
        {
            if (_disposedValue)
            {
                return;
            }
            _disposedValue = true;
            if (_applicationMutex == null)
            {
                return;
            }
            try
            {
                if (IsLocked)
                {
                    _applicationMutex.ReleaseMutex();
                    IsLocked = false;
                    Log.Info().WriteLine("Released Mutex {0} for {1} from Thread {2}", _mutexId, _resourceName, Thread.CurrentThread.ManagedThreadId);
                }
                _applicationMutex.Dispose();
            }
            catch (Exception ex)
            {
                Log.Error().WriteLine(ex, "Error releasing Mutex {0} for {1} from Thread {2}", _mutexId, _resourceName, Thread.CurrentThread.ManagedThreadId);
            }
            _applicationMutex = null;
        }
    }

}
