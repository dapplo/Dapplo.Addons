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

using Dapplo.Config.Ini;
using Dapplo.LogFacade;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Threading;
using System.Threading.Tasks;

namespace Dapplo.Addons.TestAddon
{
	[StartupAction(DoNotAwait = false)]
	[ShutdownAction]
	public class SomeAddon : IStartupAction, IShutdownAction
	{
		private static readonly LogSource Log = new LogSource();

		[Import]
		public IThisIsConfiguration MyConfig
		{
			get;
			set;
		}

		[ImportMany]
		public IEnumerable<IIniSection> MyConfigs
		{
			get;
			set;
		}

		public async Task ShutdownAsync(CancellationToken token = default(CancellationToken))
		{
			await Task.Delay(100, token);
			Log.Debug().WriteLine("ShutdownAsync called!");
			throw new System.Exception("This should be logged!");
		}

		public async Task StartAsync(CancellationToken token = new CancellationToken())
	    {
			foreach (var iniSection in MyConfigs)
			{
				var name = iniSection.GetSectionName();
				Log.Debug().WriteLine("Section {0}", name);
			}
			Log.Debug().WriteLine("This shoud not give an exception!");
			await Task.Delay(100, token);
			Log.Debug().WriteLine("StartAsync called!");
			Log.Debug().WriteLine("Value: {0}", MyConfig.Name);
		}
	}
}
