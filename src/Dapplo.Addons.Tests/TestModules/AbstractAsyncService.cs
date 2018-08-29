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

using System.Threading;
using System.Threading.Tasks;
using Dapplo.Log;

#endregion

namespace Dapplo.Addons.Tests.TestModules
{
    public class AbstractAsyncService : IStartupAsync, IShutdownAsync
    {
        private readonly OrderProvider _orderProvider;
        private readonly LogSource _log;
        public int StartupOrder { get; private set; }
        public int ShutdownOrder { get; private set; }
        public bool DidStartup { get; private set; }
        public bool DidShutdown { get; private set; }

        public AbstractAsyncService(OrderProvider orderProvider)
        {
            _orderProvider = orderProvider;
            _log = new LogSource(GetType());
        }

        public async Task StartupAsync(CancellationToken cancellationToken = default)
        {
            _log.Info().WriteLine("Before");
            await Task.Delay(100, cancellationToken).ConfigureAwait(false);
            StartupOrder = _orderProvider.TakeStartupNumber();
            DidStartup = true;
            _log.Info().WriteLine("After");
        }

        public async Task ShutdownAsync(CancellationToken cancellationToken = default)
        {
            _log.Info().WriteLine("Before");
            await Task.Delay(100, cancellationToken).ConfigureAwait(false);
            ShutdownOrder = _orderProvider.TakeShutdownNumber();
            DidShutdown = true;
            _log.Info().WriteLine("After");
        }
    }
}