﻿/*
	Dapplo - building blocks for desktop applications
	Copyright (C) 2015-2016 Dapplo

	For more information see: http://dapplo.net/
	Dapplo repositories are hosted on GitHub: https://github.com/dapplo

	This file is part of Dapplo.Addons

	Dapplo.Addons is free software: you can redistribute it and/or modify
	it under the terms of the GNU General Public License as published by
	the Free Software Foundation, either version 3 of the License, or
	(at your option) any later version.

	Dapplo.Addons is distributed in the hope that it will be useful,
	but WITHOUT ANY WARRANTY; without even the implied warranty of
	MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
	GNU General Public License for more details.

	You should have received a copy of the GNU General Public License
	along with Dapplo.Addons. If not, see <http://www.gnu.org/licenses/>.
 */


using Dapplo.LogFacade;
using System;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Threading;

namespace Dapplo.Addons.Bootstrapper
{
	/// <summary>
	/// This protects your resources or application from running more than once
	/// Simplifies the usage of the Mutex class, as described here: https://msdn.microsoft.com/en-us/library/System.Threading.Mutex.aspx
	/// </summary>
	public class ResourceMutex : IDisposable
	{
		private static readonly LogSource Log = new LogSource();
		private readonly string _mutexId;
		private readonly string _resourceName;
		private Mutex _applicationMutex;

		/// <summary>
		/// Test if the Mutex was created and locked.
		/// </summary>
		public bool IsLocked
		{
			get;
			set;
		}

		/// <summary>
		/// Private constructor 
		/// </summary>
		/// <param name="mutexId"></param>
		/// <param name="resourceName"></param>
		private ResourceMutex(string mutexId, string resourceName = null)
		{
			_mutexId = mutexId;
			_resourceName = resourceName ?? "some resource";
		}

		/// <summary>
		/// Create a ResourceMutex for the specified mutex id and resource-name
		/// </summary>
		/// <param name="mutexId">ID of the mutex, preferably a Guid as string</param>
		/// <param name="resourceName">Name of the resource to lock, e.g your application name, usefull for logs</param>
		/// <param name="global">true to have a global mutex see: https://msdn.microsoft.com/en-us/library/bwe34f1k.aspx </param>
		public static ResourceMutex Create(string mutexId, string resourceName = null, bool global = false)
		{
			var applicationMutex = new ResourceMutex((global ? @"Global\" : @"Local\") + mutexId, resourceName);
			applicationMutex.Lock();
			return applicationMutex;
		}

		/// <summary>
		/// This tries to get the Mutex, which takes care of having multiple instances running
		/// </summary>
		/// <returns>true if it worked, false if another instance is already running or something went wrong</returns>
		public bool Lock()
		{
			Log.Verbose().WriteLine("{0} is trying to get Mutex {1}", _resourceName, _mutexId);

			IsLocked = true;
			// check whether there's an local instance running already, but use local so this works in a multi-user environment
			try
			{
				// Added Mutex Security, hopefully this prevents the UnauthorizedAccessException more gracefully
				var sid = new SecurityIdentifier(WellKnownSidType.WorldSid, null);
				var mutexsecurity = new MutexSecurity();
				mutexsecurity.AddAccessRule(new MutexAccessRule(sid, MutexRights.FullControl, AccessControlType.Allow));
				mutexsecurity.AddAccessRule(new MutexAccessRule(sid, MutexRights.ChangePermissions, AccessControlType.Deny));
				mutexsecurity.AddAccessRule(new MutexAccessRule(sid, MutexRights.Delete, AccessControlType.Deny));

				bool createdNew;
				// 1) Create Mutex
				_applicationMutex = new Mutex(true, _mutexId, out createdNew, mutexsecurity);
				// 2) if the mutex wasn't created new get the right to it, this returns false if it's already locked
				if (!createdNew && !_applicationMutex.WaitOne(100, false))
				{
					Log.Warn().WriteLine("{0} seems already to be running!", _resourceName);
					IsLocked = false;
					// Clean up
					_applicationMutex.Close();
					_applicationMutex = null;
				}
				else
				{
					Log.Info().WriteLine("{0} has claimed the mutex {1}", _resourceName, _mutexId);
				}
			}
			catch (AbandonedMutexException e)
			{
				// Another instance didn't cleanup correctly!
				// we can ignore the exception, it happend on the "waitone" but still the mutex belongs to us
				Log.Warn().WriteLine(e, "{0} didn't cleanup correctly!", _resourceName);
			}
			catch (UnauthorizedAccessException e)
			{
				Log.Error().WriteLine(e, "{0} is most likely already running for a different user in the same session, can't create/get mutex {1} due to error.", _resourceName, _mutexId);
				IsLocked = false;
			}
			catch (Exception ex)
			{
				Log.Error().WriteLine(ex, "Problem obtaining the Mutex {1} for {0}, assuming it was already taken!", _resourceName, _mutexId);
				IsLocked = false;
			}
			return IsLocked;
		}

		#region IDisposable Support
		//  To detect redundant Dispose calls
		private bool _disposedValue = false;

		/// <summary>
		/// The real disposing code
		/// </summary>
		/// <param name="disposing">true if dispose is called, false when the finalizer is called</param>
		protected virtual void Dispose(bool disposing)
		{
			if (!_disposedValue)
			{
				try
				{
					_applicationMutex?.ReleaseMutex();
					_applicationMutex = null;
					Log.Info().WriteLine("Released Mutex {0} for {1}", _mutexId, _resourceName);
				}
				catch (Exception ex)
				{
					Log.Error().WriteLine(ex, "Error releasing Mutex {0} for {1}", _mutexId, _resourceName);
				}
				_disposedValue = true;
			}
		}

		/// <summary>
		/// Make sure the ApplicationMutex is disposed when the finalizer is called
		/// </summary>
		~ResourceMutex() {
			// Do not change this code. Put cleanup code in Dispose(bool disposing) above.
			Dispose(false);
		}

		/// <summary>
		/// The dispose interface, which calls Dispose(true) to signal that dispose is called.
		/// </summary>
		public void Dispose()
		{
			// Do not change this code. Put cleanup code in Dispose(bool disposing) above.
			Dispose(true);
			GC.SuppressFinalize(this);
		}
		#endregion
	}
}