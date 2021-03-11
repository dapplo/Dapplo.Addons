// Dapplo - building blocks for .NET applications
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
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Dapplo.Addons.TestAddon.Config;
using Dapplo.Config.Ini;
using Dapplo.Log;

namespace Dapplo.Addons.TestAddon
{
    public class SomeAddon : IStartupAsync, IShutdownAsync
    {
        private static readonly LogSource Log = new LogSource();

        public SomeAddon(IThisIsConfiguration myConfig, IThisIsSubConfiguration mysubConfig, IEnumerable<IIniSection> myConfigs, bool throwStartupException = false)
        {
            MyConfig = myConfig;
            MysubConfig = mysubConfig;
            MyConfigs = myConfigs;
            ThrowStartupException = throwStartupException;
        }

        public IThisIsConfiguration MyConfig { get; }
        public IThisIsSubConfiguration MysubConfig { get; }
        public IEnumerable<IIniSection> MyConfigs { get; }

        /// <summary>
        ///     This imports a bool which is set in the test case and specifies if this addon needs to throw a startup exception
        /// </summary>
        private bool ThrowStartupException { get; }

        public async Task ShutdownAsync(CancellationToken cancellationToken = default)
        {
            await Task.Delay(100, cancellationToken).ConfigureAwait(false);
            Log.Debug().WriteLine("ShutdownAsync called!");
        }

        public async Task StartupAsync(CancellationToken cancellationToken = new CancellationToken())
        {
            if (ThrowStartupException)
            {
                throw new NotSupportedException("I was ordered to!!!");
            }
            foreach (var iniSection in MyConfigs)
            {
                var name = iniSection.GetSectionName();
                Log.Debug().WriteLine("Section {0}", name);
            }
            Log.Debug().WriteLine("This shoud not give an exception!");
            await Task.Delay(100, cancellationToken).ConfigureAwait(false);
            Log.Debug().WriteLine("StartAsync called!");
            Log.Debug().WriteLine("Value: {0}", MyConfig.Name);
        }
    }
}