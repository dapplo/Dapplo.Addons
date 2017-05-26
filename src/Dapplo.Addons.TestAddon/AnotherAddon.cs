﻿#region Dapplo 2016-2017 - GNU Lesser General Public License

// Dapplo - building blocks for .NET applications
// Copyright (C) 2016-2017 Dapplo
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
using System.ComponentModel.Composition;
using System.Threading;
using System.Threading.Tasks;

#endregion

namespace Dapplo.Addons.TestAddon
{
    [StartupAction(AwaitStart = false, StartupOrder = 1)]
    [ShutdownAction]
    public class AnotherAddon : IAsyncStartupAction, IAsyncShutdownAction
    {

        [ImportingConstructor]
        public AnotherAddon(IThisIsSubConfiguration mysubConfig)
        {
            if (!string.Equals("Dapplo", mysubConfig.Company))
            {
                throw new NotSupportedException();
            }
        }
        /// <summary>
        ///     This imports a bool which is set in the test case and specifies if this addon needs to throw a startup exception
        /// </summary>
        [Import(AllowDefault = true)]
        private bool ThrowStartupException { get; set; }

        public async Task ShutdownAsync(CancellationToken token = default(CancellationToken))
        {
            await Task.Delay(100, token).ConfigureAwait(false);
        }

        public async Task StartAsync(CancellationToken token = new CancellationToken())
        {
            await Task.Delay(100, token).ConfigureAwait(false);
        }
    }
}