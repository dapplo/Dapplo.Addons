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
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Dapplo.Log;
using Dapplo.Addons.Bootstrapper.ExportProviders;

#endregion

namespace Dapplo.Addons.Bootstrapper
{
	/// <summary>
	///     A bootstrapper, which has functionality for the startup and shutdown actions
	/// </summary>
	public class StartupShutdownBootstrapper : CompositionBootstrapper
	{
		private static readonly LogSource Log = new LogSource();

		[ImportMany]
		// ReSharper disable once FieldCanBeMadeReadOnly.Local
		private IEnumerable<Lazy<IShutdownAction, IShutdownActionMetadata>> _shutdownActions = null;

		[ImportMany]
		// ReSharper disable once FieldCanBeMadeReadOnly.Local
		private IEnumerable<Lazy<IStartupAction, IStartupActionMetadata>> _startupActions = null;

		/// <summary>
		///     Specifies if Dispose automatically calls the shutdown
		/// </summary>
		public bool AutoShutdown { get; set; } = true;

		/// <summary>
		///     Specifies if Run automatically calls the startup
		/// </summary>
		public bool AutoStartup { get; set; } = true;

		/// <summary>
		///     Override the run to make sure "this" is injected
		/// </summary>
		public override async Task<bool> RunAsync(CancellationToken cancellationToken = default(CancellationToken))
		{
			Log.Debug().WriteLine("Starting");
			var result = await base.RunAsync(cancellationToken).ConfigureAwait(false);

			// With FillImports the Export providers will run, so they will have to be initialized before
			foreach (var exportProvider in ExportProviders)
			{
				var baseConfigExportProvider = exportProvider as BaseConfigExportProvider;
				baseConfigExportProvider?.Initialize();
			}

			FillImports(this);
			if (AutoStartup)
			{
				await StartupAsync(cancellationToken).ConfigureAwait(false);
			}
			return result;
		}

		/// <summary>
		///     Initiate Shutdown on all "Shutdown actions"
		/// </summary>
		/// <param name="cancellationToken">CancellationToken</param>
		/// <returns>Task</returns>
		public async Task ShutdownAsync(CancellationToken cancellationToken = default(CancellationToken))
		{
			Log.Debug().WriteLine("Shutdown of the shutdown actions, if any");
			if (_shutdownActions == null)
			{
				Log.Debug().WriteLine("No shutdown actions set...");
				return;
			}
			var orderedActions = from export in _shutdownActions orderby export.Metadata.ShutdownOrder ascending select export;

			var tasks = new List<KeyValuePair<Type, Task>>();

			// Variable used for grouping the shutdowns
			var groupingOrder = int.MaxValue;

			foreach (var lazyShutdownAction in orderedActions)
			{
				// Check if we have all the startup actions belonging to a group
				if (tasks.Count > 0 && groupingOrder != lazyShutdownAction.Metadata.ShutdownOrder)
				{
					groupingOrder = lazyShutdownAction.Metadata.ShutdownOrder;

					// Await all belonging to the same order "group"
					await WhenAll(tasks).ConfigureAwait(false);
					// Clean the tasks, we are finished.
					tasks.Clear();
				}
				IShutdownAction shutdownAction;
				try
				{
					shutdownAction = lazyShutdownAction.Value;
				}
				catch (Exception ex)
				{
					Log.Error().WriteLine(ex, "Exception instantiating IShutdownAction, probably a MEF issue. (ignoring in shutdown)");
					continue;
				}

				if (Log.IsDebugEnabled())
				{
					Log.Debug().WriteLine("Stopping {0}", shutdownAction.GetType());
				}

				try
				{
					// Create a task (it will start running, but we don't await it yet)
					var shutdownTask = shutdownAction.ShutdownAsync(cancellationToken);
					// Store it for awaiting
					tasks.Add(new KeyValuePair<Type, Task>(shutdownAction.GetType(), shutdownTask));
				}
				catch (Exception ex)
				{
					Log.Error().WriteLine(ex, "Exception executing IShutdownAction {0}: ", shutdownAction.GetType());
				}
			}
			// Await all remaining tasks, as the system is shutdown we NEED to wait, and ignore but log their exceptions
			if (tasks.Count > 0)
			{
				await WhenAll(tasks).ConfigureAwait(false);
			}
		}

		/// <summary>
		///     Startup all "Startup actions"
		///     Call this after run, it will find all IStartupAction's and start them in the specified order
		/// </summary>
		/// <param name="cancellationToken">CancellationToken</param>
		/// <returns>Task</returns>
		public async Task StartupAsync(CancellationToken cancellationToken = default(CancellationToken))
		{
			Log.Debug().WriteLine("Starting the startup actions, if any");
			if (_startupActions == null)
			{
				Log.Debug().WriteLine("No startup actions set...");
				return;
			}
			var orderedActions = from export in _startupActions orderby export.Metadata.StartupOrder ascending select export;

			var tasks = new List<KeyValuePair<Type, Task>>();
			var nonAwaitables = new List<KeyValuePair<Type, Task>>();

			// Variable used for grouping the startups
			var groupingOrder = int.MaxValue;

			foreach (var lazyStartupAction in orderedActions)
			{
				try
				{
					// Check if we have all the startup actions belonging to a group
					if (tasks.Count > 0 && groupingOrder != lazyStartupAction.Metadata.StartupOrder)
					{
						groupingOrder = lazyStartupAction.Metadata.StartupOrder;
						// Await all belonging to the same order "group"
						await WhenAll(tasks).ConfigureAwait(false);
						// Clean the tasks, we are finished.
						tasks.Clear();
					}
					IStartupAction startupAction;
					try
					{
						startupAction = lazyStartupAction.Value;
					}
					catch (CompositionException cEx)
					{
						Log.Error().WriteLine(cEx, "MEF Exception instantiating IStartupAction.");
						throw;
					}
					catch (Exception ex)
					{
						Log.Error().WriteLine(ex, "Exception instantiating IStartupAction, probably a MEF issue.");
						continue;
					}
					if (Log.IsDebugEnabled())
					{
						Log.Debug().WriteLine("Starting {0}", startupAction.GetType());
					}

					// Create a task (it will start running, but we don't await it yet)
					var task = startupAction.StartAsync(cancellationToken);
					// add the task to an await list, but only if needed!
					if (lazyStartupAction.Metadata.AwaitStart)
					{
						tasks.Add(new KeyValuePair<Type, Task>(lazyStartupAction.Value.GetType(), task));
					}
					else
					{
						if (Log.IsErrorEnabled())
						{
							// We do await for them, but just to catch any exceptions
							nonAwaitables.Add(new KeyValuePair<Type, Task>(lazyStartupAction.Value.GetType(), task));
						}
					}
				}
				catch (StartupException)
				{
					Log.Fatal().WriteLine("StartupException cancels the startup.");
					throw;
				}
				catch (Exception ex)
				{
					Log.Error().WriteLine(ex, "Exception executing IStartupAction {0}: ", lazyStartupAction.Value.GetType());
				}
			}

			// Await all remaining tasks
			if (tasks.Any())
			{
				try
				{
					await WhenAll(tasks, false).ConfigureAwait(false);
				}
				catch (StartupException)
				{
					Log.Fatal().WriteLine("Startup will be cancelled due to the previous StartupException.");
					throw;
				}
			}
			// If there is logging, log any exceptions of tasks that aren't awaited
			if (nonAwaitables.Count > 0 && Log.IsErrorEnabled())
			{
				// ReSharper disable once UnusedVariable
				var ignoreTask = Task.Run(async () =>
				{
					try
					{
						await WhenAll(nonAwaitables).ConfigureAwait(false);
					}
					catch (StartupException)
					{
						// Ignore the exception, just log information
						Log.Info().WriteLine("Ignoring StartupException, as the startup has AwaitStart set to false.");
					}
				}, cancellationToken);
			}
		}

		/// <summary>
		///     Stop the Bootstrapper
		/// </summary>
		/// <param name="cancellationToken">CancellationToken</param>
		/// <returns>Task</returns>
		public override async Task<bool> StopAsync(CancellationToken cancellationToken = default(CancellationToken))
		{
			Log.Debug().WriteLine("Stopping bootstrapper");
			if (AutoShutdown)
			{
				await ShutdownAsync(cancellationToken).ConfigureAwait(false);
			}
			return await base.StopAsync(cancellationToken).ConfigureAwait(false);
		}

		/// <summary>
		///     Special WhenAll, this awaits the supplied values and log any exceptions they had.
		///     This is not optimized, like Task.WhenAll...
		/// </summary>
		/// <param name="tasksToAwait"></param>
		/// <param name="ignoreExceptions">if true (default) the exceptions will be logged but ignored.</param>
		/// <returns>Task</returns>
		private async Task WhenAll(IEnumerable<KeyValuePair<Type, Task>> tasksToAwait, bool ignoreExceptions = true)
		{
			foreach (var taskInfo in tasksToAwait)
			{
				try
				{
					await taskInfo.Value.ConfigureAwait(false);
				}
				catch (StartupException ex)
				{
					Log.Error().WriteLine(ex, "StartupException in {0}", taskInfo.Key);
					// Break execution
					throw;
				}
				catch (Exception ex)
				{
					Log.Error().WriteLine(ex, "Exception in {0}", taskInfo.Key);
					if (!ignoreExceptions)
					{
						throw;
					}
				}
			}
		}
	}
}