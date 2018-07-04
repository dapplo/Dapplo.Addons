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
using System.Threading.Tasks;
using Dapplo.Jira;
using Dapplo.Log;

#endregion

namespace Dapplo.Addons.TestAddonWithCostura
{
    public class SomeCosturaAddon : IStartupAsync, IShutdownAsync
    {
        private static readonly LogSource Log = new LogSource();

        public async Task ShutdownAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            await Task.Delay(100, cancellationToken).ConfigureAwait(false);
            Log.Debug().WriteLine("ShutdownAsync called!");
            throw new NotSupportedException("This should be logged!");
        }

        public async Task StartupAsync(CancellationToken cancellationToken = new CancellationToken())
        {
            Log.Debug().WriteLine("StartAsync called!");
            var serverInfo = await JiraClient.Create(new Uri("https://greenshot.atlassian.net")).Server.GetInfoAsync(cancellationToken).ConfigureAwait(false);
            Log.Debug().WriteLine("Jira server version {0}", serverInfo.Version);
            await Task.Delay(100, cancellationToken).ConfigureAwait(false);
        }
    }
}