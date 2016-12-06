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
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Dapplo.Addons.Bootstrapper.ExportProviders;
using Dapplo.Log;

#endregion

namespace Dapplo.Addons.Bootstrapper
{
	/// <summary>
	///     This bootstrapper is made especially to host dapplo "apps".
	///     It initializes the IniConfig and LanguageLoader, and makes Importing possible.
	///     You can protect your application from starting multiple instances by specifying a Mutex-ID
	/// </summary>
	public class ApplicationBootstrapper : StartupShutdownBootstrapper
	{
		private static readonly LogSource Log = new LogSource();
		private readonly string _applicationName;
		private readonly ResourceMutex _resourceMutex;

		/// <summary>
		///     Create the application bootstrapper, for the specified application name
		///     The mutex is created and locked in the contructor, and some of your application logic might depend on this.
		/// </summary>
		/// <param name="applicationName">Name of your application</param>
		/// <param name="mutexId">
		///     string with an ID for your mutex, preferably a Guid. If the mutex can't be locked, the
		///     bootstapper will not  be able to "bootstrap".
		/// </param>
		/// <param name="global">Is the mutex a global or local block (false means only in this Windows session)</param>
		public ApplicationBootstrapper(string applicationName, string mutexId = null, bool global = false)
		{
			if (applicationName == null)
			{
				throw new ArgumentNullException(nameof(applicationName));
			}
			_applicationName = applicationName;
			if (mutexId != null)
			{
				_resourceMutex = ResourceMutex.Create(mutexId, applicationName, global);
			}
		}

		/// <summary>
		///     If this is set to true, which is the default, IniConfig and LanguageLoader will be configured automatically.
		/// </summary>
		public bool AutoConfigure { get; set; } = true;

		/// <summary>
		///     Returns if the Mutex is locked, in other words if this ApplicationBootstrapper can continue
		///     This also returns true if no mutex is used
		/// </summary>
		public bool IsMutexLocked => _resourceMutex == null || _resourceMutex.IsLocked;


		/// <summary>
		///     Initialize the application bootstrapper, this makes sure the configuration and languages can be loaded
		/// </summary>
		/// <returns>bool with value of IsInitialized</returns>
		public override async Task<bool> InitializeAsync(CancellationToken cancellationToken = default(CancellationToken))
		{
			Log.Verbose().WriteLine("Trying to initialize application {0}", _applicationName);
			await base.InitializeAsync(cancellationToken).ConfigureAwait(false);
			return IsInitialized;
		}

		/// <summary>
		///     Override the run to prevent starting when the mutex isn't locked
		/// </summary>
		public override async Task<bool> RunAsync(CancellationToken cancellationToken = new CancellationToken())
		{
			if (_resourceMutex == null || _resourceMutex.IsLocked)
			{
				return await base.RunAsync(cancellationToken);
			}
			Log.Error().WriteLine("Can't Run {0} due to missing mutex lock", _applicationName);
			return false;
		}

		#region IDisposable Support

		// To detect redundant calls
		private readonly bool _disposedValue = false;

		/// <summary>
		///     Implementation of the dispose pattern
		/// </summary>
		/// <param name="disposing">bool</param>
		protected override void Dispose(bool disposing)
		{
			// Call other stuff first, the mutex should protect untill everything is shutdown
			base.Dispose(disposing);

			// Handle our own stuff, currently only the mutex (if any)
			if (!_disposedValue)
			{
				if (disposing)
				{
					// dispose managed state (managed objects) here.
				}

				_resourceMutex?.Dispose();
			}
		}

		#endregion
	}
}