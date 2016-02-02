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

using System.Threading;
using System.Threading.Tasks;
using System.Linq;
using System.Collections.Generic;
using System;
using System.ComponentModel.Composition;
using Dapplo.LogFacade;

namespace Dapplo.Addons.Implementation
{
	/// <summary>
	/// A bootstrapper, which has functionality for the startup & shutdown actions
	/// </summary>
	public class StartupShutdownBootstrapper : SimpleBootstrapper
	{
		private static readonly LogSource Log = new LogSource();

		[ImportMany]
		// ReSharper disable once FieldCanBeMadeReadOnly.Local
		private IEnumerable<Lazy<IStartupAction, IStartupActionMetadata>> _startupActions = null;

		[ImportMany]
		// ReSharper disable once FieldCanBeMadeReadOnly.Local
		private IEnumerable<Lazy<IShutdownAction, IShutdownActionMetadata>> _shutdownActions = null;

		/// <summary>
		/// Startup all "Startup actions"
		/// Call this after run, it will find all IStartupAction's and start them in the specified order
		/// </summary>
		/// <param name="token">CancellationToken</param>
		/// <returns>Task</returns>
		public async Task StartupAsync(CancellationToken token = default(CancellationToken))
		{
            var orderedActions = from export in _startupActions orderby export.Metadata.StartupOrder ascending select export;

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
					Log.Debug().WriteLine("Starting {0}", startupAction.Value.GetType());

					// Create a task (it will start running, but we don't await it yet)
					var task = startupAction.Value.StartAsync(token);
					// add the task to an await list, but only if needed!
					if (!startupAction.Metadata.DoNotAwait)
					{
						tasks.Add(task);
					}
				}
				catch (Exception ex)
				{
					if (startupAction.IsValueCreated)
					{
						Log.Error().WriteLine(ex, "Exception executing startupAction {0}: ", startupAction.Value.GetType());
					}
					else
					{
						Log.Error().WriteLine(ex, "Exception instantiating startupAction {0}: ", startupAction.Value.GetType());
					}
				}
			}
			// Await all remaining tasks
			if (tasks.Any())
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
			var orderedActions = from export in _shutdownActions orderby export.Metadata.ShutdownOrder ascending select export;

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
					Log.Debug().WriteLine("Stopping {0}", shutdownAction.Value.GetType());
					// Create a task (it will start running, but we don't await it yet)
					tasks.Add(shutdownAction.Value.ShutdownAsync(token));
				}
				catch (Exception ex)
				{
					if (shutdownAction.IsValueCreated)
					{
						Log.Error().WriteLine(ex, "Exception executing shutdownAction {0}: ", shutdownAction.Value.GetType());
					}
					else
					{
						Log.Error().WriteLine(ex, "Exception instantiating shutdownAction {0}: ", shutdownAction.Value.GetType());
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
		/// Override the run to make sure "this" is injected
		/// </summary>
		public override void Run()
		{
			base.Run();
			FillImports(this);
		}
	}
}
