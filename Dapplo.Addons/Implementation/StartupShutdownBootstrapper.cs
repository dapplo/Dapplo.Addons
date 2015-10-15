/*
 * dapplo - building blocks for desktop applications
 * Copyright (C) 2015 Robin Krom
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

using System.Threading;
using System.Threading.Tasks;
using System.Linq;
using System.Collections.Generic;
using System;

namespace Dapplo.Addons.Implementation
{
	/// <summary>
	/// A bootstrapper, which has functionality for the startup & shutdown actions
	/// </summary>
	public class StartupShutdownBootstrapper : SimpleBootstrapper
	{
		/// <summary>
		/// Startup all "Startup actions"
		/// Call this after run, it will find all IStartupAction's and start them in the specified order
		/// </summary>
		/// <param name="token">CancellationToken</param>
		/// <returns>Task</returns>
		public async Task StartupAsync(CancellationToken token = default(CancellationToken))
		{
			var startupActions = GetExports<IStartupAction, IStartupActionMetadata>();
            var orderedActions = from export in startupActions orderby export.Metadata.StartupOrder ascending select export;

			var tasks = new List<Task>();

			// Variable used for grouping the startups
			int groupingOrder = int.MaxValue;

			foreach (var startupAction in orderedActions)
			{
				try
				{
					// Check if we have all the startup actions belonging to a group
					if (tasks.Count > 0 && groupingOrder != startupAction.Metadata.StartupOrder)
					{
						groupingOrder = startupAction.Metadata.StartupOrder;
						// Await all belonging to the same order "group"
						await Task.WhenAll(tasks);
						// Clean the tasks, we are finished.
						tasks.Clear();
					}
					// Create a task (it will start running, but we don't await it yet)
					var task = startupAction.Value.StartAsync(token);
					// add the task to an await list, but only if needed!
					if (!startupAction.Metadata.DoNotAwait)
					{
						tasks.Add(task);
					}
				}
				catch (Exception)
				{
					if (startupAction.IsValueCreated)
					{
						//LOG.Error(ex, "Exception executing startupAction {0}: ", startupAction.Value.GetType());
					}
					else
					{
						//LOG.Error(ex, "Exception instantiating startupAction: ");
					}
				}
			}
			// Await all remaining tasks
			if (tasks.Count > 0)
			{
				await Task.WhenAll(tasks);
			}
		}

		/// <summary>
		/// Initiate Shutdown on all "Shutdown actions" 
		/// </summary>
		/// <param name="token">CancellationToken</param>
		/// <returns>Task</returns>
		public async Task ShutdownAsync(CancellationToken token = default(CancellationToken))
		{
			var shutdownActions = GetExports<IShutdownAction, IShutdownActionMetadata>();

			var orderedActions = from export in shutdownActions orderby export.Metadata.ShutdownOrder ascending select export;

			var tasks = new List<Task>();

			// Variable used for grouping the shutdowns
			int groupingOrder = int.MaxValue;

			foreach (var shutdownAction in orderedActions)
			{
				try
				{
					// Check if we have all the startup actions belonging to a group
					if (tasks.Count > 0 && groupingOrder != shutdownAction.Metadata.ShutdownOrder)
					{
						groupingOrder = shutdownAction.Metadata.ShutdownOrder;
						// Await all belonging to the same order "group"
						await Task.WhenAll(tasks);
						// Clean the tasks, we are finished.
						tasks.Clear();
					}
					// Create a task (it will start running, but we don't await it yet)
					tasks.Add(shutdownAction.Value.ShutdownAsync(token));
				}
				catch (Exception)
				{
					if (shutdownAction.IsValueCreated)
					{
						//LOG.Error(ex, "Exception executing startupAction {0}: ", shutdownAction.Value.GetType());
					}
					else
					{
						//LOG.Error(ex, "Exception instantiating startupAction: ");
					}
				}
			}
			// Await all remaining tasks
			if (tasks.Count > 0)
			{
				await Task.WhenAll(tasks);
			}
		}
	}
}
