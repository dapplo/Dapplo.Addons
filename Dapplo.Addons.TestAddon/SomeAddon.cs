#region Dapplo 2016 - GNU Lesser General Public License

// Dapplo - building blocks for .NET applications
// Copyright (C) 2016 Dapplo
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
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Threading;
using System.Threading.Tasks;
using Dapplo.Config.Ini;
using Dapplo.Log.Facade;

#endregion

namespace Dapplo.Addons.TestAddon
{
	[StartupAction(AwaitStart = false, StartupOrder = 1)]
	[ShutdownAction]
	public class SomeAddon : IStartupAction, IShutdownAction
	{
		private static readonly LogSource Log = new LogSource();

		[Import]
		public IThisIsConfiguration MyConfig { get; set; }

		[ImportMany]
		public IEnumerable<IIniSection> MyConfigs { get; set; }

		/// <summary>
		///     This imports a bool which is set in the test case and specifies if this addon needs to throw a startup exception
		/// </summary>
		[Import(AllowDefault = true)]
		private bool ThrowStartupException { get; set; }

		public async Task ShutdownAsync(CancellationToken token = default(CancellationToken))
		{
			await Task.Delay(100, token).ConfigureAwait(false);
			Log.Debug().WriteLine("ShutdownAsync called!");
			throw new Exception("This should be logged!");
		}

		public async Task StartAsync(CancellationToken token = new CancellationToken())
		{
			if (ThrowStartupException)
			{
				throw new StartupException("I was ordered to!!!");
			}
			foreach (var iniSection in MyConfigs)
			{
				var name = iniSection.GetSectionName();
				Log.Debug().WriteLine("Section {0}", name);
			}
			Log.Debug().WriteLine("This shoud not give an exception!");
			await Task.Delay(100, token).ConfigureAwait(false);
			Log.Debug().WriteLine("StartAsync called!");
			Log.Debug().WriteLine("Value: {0}", MyConfig.Name);
		}
	}
}