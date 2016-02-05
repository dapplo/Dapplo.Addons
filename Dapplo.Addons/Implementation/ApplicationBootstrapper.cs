/*
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

using System;
using Dapplo.Config.Ini;
using Dapplo.Config.Language;
using IniConfigExportProvider = Dapplo.Addons.ExportProviders.IniConfigExportProvider;
using LanguageExportProvider = Dapplo.Addons.ExportProviders.LanguageExportProvider;
using Dapplo.LogFacade;
using System.Threading.Tasks;
using System.Threading;

namespace Dapplo.Addons.Implementation
{
	/// <summary>
	/// This bootstrapper is made especially to host dapplo "apps".
	/// It initializes the IniConfig and LanguageLoader, and makes Importing possible.
	/// You can protect your application from starting multiple instances by specifying a Mutex-ID
	/// </summary>
	public class ApplicationBootstrapper : StartupShutdownBootstrapper, IDisposable
	{
		private static readonly LogSource Log = new LogSource();
		private readonly string _applicationName;
		private LanguageLoader _languageLoader;
		private IniConfig _iniConfig;
		private readonly ResourceMutex _resourceMutex;

		/// <summary>
		/// Initialize the application bootstrapper
		/// </summary>
		/// <returns></returns>
		public override async Task<bool> InitializeAsync(CancellationToken cancellationToken = default(CancellationToken))
		{
			Log.Verbose().WriteLine("Trying to initialize {0}", _applicationName);
			// Only allow if the resource is locked by us, or if no lock is needed
			if (_resourceMutex == null || _resourceMutex.IsLocked)
			{
				if (IniConfigForExport == null)
				{
					IniConfigForExport = IniConfig.Current;
				}
				if (LanguageLoaderForExport == null)
				{
					LanguageLoaderForExport = LanguageLoader.Current;
				}

				await base.InitializeAsync(cancellationToken);
			}
			else
			{
				Log.Error().WriteLine("Can't initialize {0} due to missing mutex lock", _applicationName);
			}
			return IsInitialized;
		}

		/// <summary>
		/// Create the application bootstrapper, for the specified application name
		/// </summary>
		/// <param name="applicationName">Name of your application</param>
		/// <param name="mutexId">string with an ID for your mutex, preferably a Guid. If the mutex can't be locked, the bootstapper will not "bootstrap".</param>
		/// <param name="global">Is the mutex a global or local block (false means only in this Windows session)</param>
		public ApplicationBootstrapper(string applicationName, string mutexId = null, bool global = false)
		{
			_applicationName = applicationName;
			if (mutexId != null)
			{
				_resourceMutex = ResourceMutex.Create(mutexId, applicationName, global);
			}
		}

		/// <summary>
		/// Use this to set the IniConfig which is used to resolve IIniSection imports
		/// </summary>
		public IniConfig IniConfigForExport
		{
			get
			{
				return _iniConfig;
			}
			set
			{
				if (_iniConfig != null)
				{
					throw new InvalidOperationException("IniConfig already set.");
				}
				_iniConfig = value;
				var exportProvider = new IniConfigExportProvider(value, AddonAssemblies, this);
				Add(exportProvider);
			}
		}

		/// <summary>
		/// Use this to set the LanguageLoader which is used resolve ILanguage imports
		/// </summary>
		public LanguageLoader LanguageLoaderForExport
		{
			get
			{
				return _languageLoader;
			}
			set
			{
				if (_languageLoader != null)
				{
					throw new InvalidOperationException("LanguageLoader already set.");
				}
				_languageLoader = value;
				var exportProvider = new LanguageExportProvider(value, AddonAssemblies, this);
				Add(exportProvider);
			}
		}

		#region IDisposable Support
		// To detect redundant calls
		private bool _disposedValue = false; 

		protected override void Dispose(bool disposing)
		{
			if (!_disposedValue)
			{
				if (disposing)
				{
					// dispose managed state (managed objects) here.
				}

				_resourceMutex?.Dispose();
			}
			base.Dispose(disposing);
		}
		#endregion
	}
}
