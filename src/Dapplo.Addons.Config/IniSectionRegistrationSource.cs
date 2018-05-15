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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Autofac.Builder;
using Autofac.Core;
using Dapplo.Ini;
using Dapplo.Log;

namespace Dapplo.Addons.Config
{
    /// <summary>
    /// This is an IRegistrationSource for the IniSection / IniSubSection
    /// </summary>
    internal class IniSectionRegistrationSource : IRegistrationSource
    {
        private static readonly LogSource Log = new LogSource();
        private static readonly MethodInfo BuildIniSectionMethod = typeof(IniSectionRegistrationSource).GetMethod(nameof(BuildIniSectionRegistration), BindingFlags.Static | BindingFlags.NonPublic);
        private static readonly MethodInfo BuildIniSubSectionMethod = typeof(IniSectionRegistrationSource).GetMethod(nameof(BuildIniSubSectionRegistration), BindingFlags.Static | BindingFlags.NonPublic);

        /// <inheritdoc />
        public bool IsAdapterForIndividualComponents => false;

        /// <inheritdoc />
        public IEnumerable<IComponentRegistration> RegistrationsFor(Service service, Func<Service, IEnumerable<IComponentRegistration>> registrations)
        {
            var typedService = service as TypedService;

            var registration = CreateRegistration(typedService?.ServiceType);
            if (registration == null)
            {
                return Enumerable.Empty<IComponentRegistration>();
            }
            return new[] { registration };
        }

        /// <summary>
        /// Helper method to create the registration
        /// </summary>
        /// <param name="typeToCreate">Type</param>
        /// <returns>IComponentRegistration or null</returns>
        private IComponentRegistration CreateRegistration(Type typeToCreate)
        {
            if (typeToCreate == null)
            {
                return null;
            }
            if (typeToCreate == typeof(IIniSubSection) || typeToCreate == typeof(IIniSection))
            {
                return null;
            }

            MethodInfo buildMethod = null;
            if (typeof(IIniSection).IsAssignableFrom(typeToCreate))
            {
                buildMethod = BuildIniSectionMethod.MakeGenericMethod(typeToCreate);
            } else if (typeof(IIniSubSection).IsAssignableFrom(typeToCreate))
            {
                buildMethod = BuildIniSubSectionMethod.MakeGenericMethod(typeToCreate);
            }

            if (buildMethod == null)
            {
                return null;
            }
            Log.Verbose().WriteLine("Creating registration for {0}", typeToCreate);
            return (IComponentRegistration)buildMethod.Invoke(null, null);
        }

        /// <summary>
        /// This creates a component registration for the specified type
        /// </summary>
        /// <typeparam name="TIniSection">interface extending IIniSection</typeparam>
        /// <returns>IComponentRegistration</returns>
        private static IComponentRegistration BuildIniSectionRegistration<TIniSection>() where TIniSection : IIniSection
        {
            return RegistrationBuilder
                .ForDelegate((c, p) => IniConfig.Current.Get<TIniSection>())
                .CreateRegistration();
        }

        /// <summary>
        /// This creates a component registration for the specified type
        /// </summary>
        /// <typeparam name="TIniSubSection">interface extending IIniSubSection</typeparam>
        /// <returns>IComponentRegistration</returns>
        private static IComponentRegistration BuildIniSubSectionRegistration<TIniSubSection>() where TIniSubSection : IIniSubSection
        {
            return RegistrationBuilder
                .ForDelegate((c, p) => IniConfig.Current.GetSubSection<TIniSubSection>())
                .CreateRegistration();
        }
    }
}
