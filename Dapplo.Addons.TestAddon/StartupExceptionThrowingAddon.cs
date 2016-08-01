//  Dapplo - building blocks for desktop applications
//  Copyright (C) 2015-2016 Dapplo
// 
//  For more information see: http://dapplo.net/
//  Dapplo repositories are hosted on GitHub: https://github.com/dapplo
// 
//  This file is part of Dapplo.Addons
// 
//  Dapplo.Addons is free software: you can redistribute it and/or modify
//  it under the terms of the GNU Lesser General Public License as published by
//  the Free Software Foundation, either version 3 of the License, or
//  (at your option) any later version.
// 
//  Dapplo.Addons is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//  GNU Lesser General Public License for more details.
// 
//  You should have a copy of the GNU Lesser General Public License
//  along with Dapplo.Addons. If not, see <http://www.gnu.org/licenses/lgpl.txt>.

#region using

using System.ComponentModel.Composition;
using System.Threading;
using System.Threading.Tasks;

#endregion

namespace Dapplo.Addons.TestAddon
{
	[StartupAction(AwaitStart = true, StartupOrder = 1)]
	public class StartupExceptionThrowingAddon : IStartupAction
	{
		/// <summary>
		/// This imports a bool which is set in the test case and specifies if this addon needs to throw a startup exception
		/// </summary>
		[Import(AllowDefault = true)]
		private bool ThrowStartupException { get; set; }

		public Task StartAsync(CancellationToken token = new CancellationToken())
		{
			if (ThrowStartupException)
			{
				throw new StartupException("I was ordered to!!!");
			}
			return Task.FromResult(true);
		}
	}
}