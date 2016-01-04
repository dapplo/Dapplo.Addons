/*
 * dapplo - building blocks for desktop applications
 * Copyright (C) Dapplo 2015-2016
 * 
 * For more information see: http://dapplo.net/
 * dapplo repositories are hosted on GitHub: https://github.com/dapplo
 * 
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 1 of the License, or
 * (at your option) any later version.
 * 
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 * 
 * You should have received a copy of the GNU General Public License
 * along with this program. If not, see <http://www.gnu.org/licenses/>.
 */

using System.ComponentModel.Composition;
using NLog;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using Dapplo.Config.Ini;

namespace Dapplo.Addons.TestAddon
{
	[StartupAction(DoNotAwait = false)]
	[ShutdownAction]
	public class SomeAddon : IStartupAction, IShutdownAction
	{
		private static readonly Logger Log = LogManager.GetCurrentClassLogger();

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
			Debug.WriteLine("ShutdownAsync called!");
		}

		public async Task StartAsync(CancellationToken token = new CancellationToken())
	    {
			foreach (var iniSection in MyConfigs)
			{
				var name = iniSection.GetSectionName();
                Debug.WriteLine(name);
			}
			Log.Debug("This shoud not give an exception!");
			await Task.Delay(100, token);
            Debug.WriteLine("StartAsync called!");
			Debug.WriteLine($"Value: {MyConfig.Name}");
		}
	}
}
