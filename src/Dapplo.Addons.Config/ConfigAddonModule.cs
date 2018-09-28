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
using Dapplo.Addons.Config.Internal;
using Dapplo.Ini;
using Dapplo.Language;
using Dapplo.Log;

namespace Dapplo.Addons.Config
{
    /// <summary>
    /// Configure the Ini and Language functionality
    /// </summary>
    public class ConfigAddonModule : AddonModule
    {
        private static readonly LogSource Log = new LogSource();

        private IniConfig _applicationIniConfig;
        private LanguageLoader _languageLoader;

        /// <inheritdoc />
        protected override void Load(ContainerBuilder builder)
        {
            var applicationName = builder.Properties[nameof(IApplicationBootstrapper.ApplicationName)] as string;
            Log.Debug().WriteLine("Initializing the configuration for {0}", applicationName);
            _applicationIniConfig = IniConfig.Current ?? new IniConfig(applicationName, applicationName);

            if (ApplicationConfigBuilderConfigExtensions.IsIniSectionResolvingEnabled(builder.Properties))
            {
                Log.Debug().WriteLine("IniSection resolving is enabled.");
                builder.RegisterSource(new IniSectionRegistrationSource());
            }
            builder.RegisterType<IniSectionService>()
                .As<IService>()
                .SingleInstance();

            _languageLoader = LanguageLoader.Current ?? new LanguageLoader(applicationName);

            if (ApplicationConfigBuilderConfigExtensions.IsLanguageResolvingEnabled(builder.Properties))
            {
                Log.Debug().WriteLine("Language resolving is enabled.");
                builder.RegisterSource(new LanguageRegistrationSource());
            }

            builder.RegisterType<LanguageService>()
                .As<IService>()
                .SingleInstance();
        }
    }
}
