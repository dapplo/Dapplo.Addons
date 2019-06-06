#region Dapplo 2016-2019 - GNU Lesser General Public License

// Dapplo - building blocks for .NET applications
// Copyright (C) 2016-2019 Dapplo
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

using Dapplo.Log;

#endregion

namespace Dapplo.Addons.Tests.TestModules
{
    public class AbstractService : IStartup, IShutdown
    {
        private readonly OrderProvider _orderProvider;
        private readonly LogSource _log;
        public int StartupOrder { get; private set; }
        public int ShutdownOrder { get; private set; }
        public bool DidStartup { get; private set; }
        public bool DidShutdown{ get; private set; }


        public AbstractService(OrderProvider orderProvider)
        {
            _orderProvider = orderProvider;
            _log = new LogSource(GetType());
        }

        public void Startup()
        {
            _log.Info().WriteLine("Before");
            StartupOrder = _orderProvider.TakeStartupNumber();
            DidStartup = true;
            _log.Info().WriteLine("After");
        }

        public void Shutdown()
        {
            _log.Info().WriteLine("Before");
            ShutdownOrder = _orderProvider.TakeShutdownNumber();
            DidShutdown = true;
            _log.Info().WriteLine("After");
        }
    }
}