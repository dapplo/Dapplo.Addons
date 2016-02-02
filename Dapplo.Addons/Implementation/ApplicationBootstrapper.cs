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

namespace Dapplo.Addons.Implementation
{
	/// <summary>
	/// This bootstrapper is made especially for Dapplo
	/// It initializes the IniConfig and LanguageLoader, and makes Importing possible.
	/// </summary>
	public class ApplicationBootstrapper : StartupShutdownBootstrapper
	{
		private readonly string _applicationName;
		private LanguageLoader _languageLoader;
		private IniConfig _iniConfig;

		public override void Initialize()
		{
			base.Initialize();
			if (IniConfigForExport == null)
			{
				IniConfigForExport = IniConfig.Current;
			}
			if (LanguageLoaderForExport == null)
			{
				LanguageLoaderForExport = LanguageLoader.Current;
			}
		}

		/// <summary>
		/// Create the application bootstrapper, for the specified application name
		/// </summary>
		/// <param name="applicationName"></param>
		public ApplicationBootstrapper(string applicationName)
		{
			_applicationName = applicationName;
		}

		/// <summary>
		/// Use this to set the IniConfig which is used to resolv IIniSection imports
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
		/// Use this to set the LanguageLoader which is used resolv ILanguage imports
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
	}
}
