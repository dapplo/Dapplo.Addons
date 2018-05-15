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

using Autofac;
using Dapplo.Ini;
using Dapplo.Language;

namespace Dapplo.Addons.Config
{
    /// <summary>
    /// Configure the Ini and Language functionality
    /// </summary>
    public class ConfigAutofacModule : Module
    {
        private IniConfig _applicationIniConfig;
        private LanguageLoader _languageLoader;

        /// <inheritdoc />
        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterType<IniSectionService>()
                .As<IService>()
                .SingleInstance();

            var applicationName = builder.Properties["applicationName"] as string;

            _applicationIniConfig = IniConfig.Current;
            if (_applicationIniConfig == null)
            {
                _applicationIniConfig = new IniConfig(applicationName, applicationName);
            }
            builder.RegisterSource(new IniSectionRegistrationSource());

            builder.RegisterType<LanguageService>()
                .As<IService>()
                .SingleInstance();

            _languageLoader = LanguageLoader.Current;
            if (_languageLoader == null)
            {
                _languageLoader = LanguageLoader.Current ?? new LanguageLoader(applicationName);
            }
            builder.RegisterSource(new LanguageRegistrationSource());
        }
    }
}
