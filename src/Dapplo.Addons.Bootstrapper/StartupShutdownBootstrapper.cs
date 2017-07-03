#region Dapplo 2016-2017 - GNU Lesser General Public License

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
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Dapplo.Log;

#endregion

namespace Dapplo.Addons.Bootstrapper
{
    /// <summary>
    ///     A bootstrapper, which has functionality for the startup and shutdown actions
    /// </summary>
    public class StartupShutdownBootstrapper : CompositionBootstrapper, IStartupShutdownBootstrapper
    {
        private static readonly LogSource Log = new LogSource();
        private readonly CancellationTokenSource _startupCancellationTokenSource = new CancellationTokenSource();

        [ImportMany]
        // ReSharper disable once FieldCanBeMadeReadOnly.Local
        private IEnumerable<Lazy<IShutdownModule, IShutdownMetadata>> _shutdownModules = null;

        [ImportMany]
        // ReSharper disable once FieldCanBeMadeReadOnly.Local
        private IEnumerable<Lazy<IStartupModule, IStartupMetadata>> _startupModules = null;

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

            ProvideDependencies(this);
            if (AutoStartup)
            {
                await StartupAsync(cancellationToken).ConfigureAwait(false);
            }
            return result;
        }

        /// <inheritdoc />
        public async Task ShutdownAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            Log.Debug().WriteLine("Shutdown of the shutdown actions, if any");
            if (_shutdownModules == null)
            {
                Log.Debug().WriteLine("No shutdown actions set...");
                return;
            }
            var orderedShutdownModules = from export in _shutdownModules orderby export.Metadata.ShutdownOrder ascending select export;

            var tasks = new List<KeyValuePair<Type, Task>>();

            // Variable used for grouping the shutdowns
            var groupingOrder = int.MaxValue;

            // Map the supplied cancellationToken to the _startupCancellationTokenSource, if the first cancells, _startupCancellationTokenSource also does
            CancellationTokenRegistration registration = default(CancellationTokenRegistration);
            if (cancellationToken.CanBeCanceled)
            {
                registration = cancellationToken.Register(() => _startupCancellationTokenSource.Cancel());
            }

            foreach (var lazyShutdownModule in orderedShutdownModules)
            {
                if (_startupCancellationTokenSource.IsCancellationRequested)
                {
                    break;
                }
                // Check if we have all the startup actions belonging to a group
                if (tasks.Count > 0 && groupingOrder != lazyShutdownModule.Metadata.ShutdownOrder)
                {
                    groupingOrder = lazyShutdownModule.Metadata.ShutdownOrder;

                    // Await all belonging to the same order "group"
                    await WhenAll(tasks).ConfigureAwait(false);
                    // Clean the tasks, we are finished.
                    tasks.Clear();
                }
                IShutdownModule shutdownModule;
                try
                {
                    shutdownModule = lazyShutdownModule.Value;
                }
                catch (Exception ex)
                {
                    Log.Error().WriteLine(ex, "Exception instantiating IShutdownModule, probably a MEF issue. (ignoring in shutdown)");
                    continue;
                }

                if (Log.IsDebugEnabled())
                {
                    Log.Debug().WriteLine("Stopping {0}", shutdownModule.GetType());
                }

                try
                {
                    Task shutdownTask = null;

                    // Test if async / sync shutdown
                    IShutdownAction shutdownAction = shutdownModule as IShutdownAction;
                    if (shutdownAction != null)
                    {
                        shutdownTask = Task.Run(() => shutdownAction.Shutdown(), _startupCancellationTokenSource.Token);
                    }
                    else
                    {
                        IAsyncShutdownAction asyncShutdownAction = shutdownModule as IAsyncShutdownAction;
                        if (asyncShutdownAction != null)
                        {
                            // Create a task (it will start running, but we don't await it yet)
                            shutdownTask = asyncShutdownAction.ShutdownAsync(_startupCancellationTokenSource.Token);
                        }
                    }
                    if (shutdownTask != null)
                    {
                        // Store it for awaiting
                        tasks.Add(new KeyValuePair<Type, Task>(shutdownModule.GetType(), shutdownTask));
                    }
                }
                catch (Exception ex)
                {
                    Log.Error().WriteLine(ex, "Exception executing IShutdownModule {0}: ", shutdownModule.GetType());
                }
            }
            // Await all remaining tasks, as the system is shutdown we NEED to wait, and ignore but log their exceptions
            if (tasks.Count > 0)
            {
                await WhenAll(tasks).ConfigureAwait(false);
            }
            if (cancellationToken.CanBeCanceled)
            {
                registration.Dispose();
            }
        }

        /// <inheritdoc />
        public async Task StartupAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            Log.Debug().WriteLine("Starting the startup actions, if any");
            if (_startupModules == null)
            {
                Log.Debug().WriteLine("No startup actions set...");
                return;
            }

            var orderedStartupModules = from export in _startupModules orderby export.Metadata.StartupOrder ascending select export;

            var tasks = new List<KeyValuePair<Type, Task>>();
            var nonAwaitables = new List<KeyValuePair<Type, Task>>();

            // Variable used for grouping the startups
            var groupingOrder = int.MaxValue;

            foreach (var lazyStartupModule in orderedStartupModules)
            {
                // Fail fast for when the stop is called during startup
                if (_startupCancellationTokenSource.IsCancellationRequested || cancellationToken.IsCancellationRequested)
                {
                    break;
                }
                try
                {
                    // Check if we have all the startup actions belonging to a group
                    if (tasks.Count > 0 && groupingOrder != lazyStartupModule.Metadata.StartupOrder)
                    {
                        groupingOrder = lazyStartupModule.Metadata.StartupOrder;
                        // Await all belonging to the same order "group"
                        await WhenAll(tasks).ConfigureAwait(false);
                        // Clean the tasks, we are finished.
                        tasks.Clear();
                    }
                    IStartupModule startupModule;
                    try
                    {
                        startupModule = lazyStartupModule.Value;
                    }
                    catch (CompositionException cEx)
                    {
                        Log.Error().WriteLine(cEx, "MEF Exception instantiating IStartupModule.");
                        throw;
                    }
                    catch (Exception ex)
                    {
                        Log.Error().WriteLine(ex, "Exception instantiating IStartupModule, probably a MEF issue.");
                        throw;
                    }
                    if (Log.IsDebugEnabled())
                    {
                        Log.Debug().WriteLine("Starting {0}", startupModule.GetType());
                    }

                    Task startupTask = null;

                    // Test if async / sync startup
                    IStartupAction startupAction = startupModule as IStartupAction;
                    if (startupAction != null)
                    {
                        Log.Verbose().WriteLine("Trying to start {0}", startupAction.GetType());
                        // Wrap sync call as async task
                        startupTask = Task.Run(() => startupAction.Start(), cancellationToken);
                    }
                    else
                    {
                        IAsyncStartupAction asyncStartupAction = startupModule as IAsyncStartupAction;
                        if (asyncStartupAction != null)
                        {
                            Log.Verbose().WriteLine("Trying to start {0}", asyncStartupAction.GetType());
                            // Create a task (it will start running, but we don't await it yet)
                            startupTask = asyncStartupAction.StartAsync(cancellationToken);
                        }
                    }

                    if (startupTask != null)
                    {
                        if (lazyStartupModule.Metadata.AwaitStart)
                        {
                            tasks.Add(new KeyValuePair<Type, Task>(lazyStartupModule.Value.GetType(), startupTask));
                        }
                        else
                        {
                            if (Log.IsErrorEnabled())
                            {
                                // We do await for them, but just to catch any exceptions
                                nonAwaitables.Add(new KeyValuePair<Type, Task>(lazyStartupModule.Value.GetType(), startupTask));
                            }
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
                    Log.Error().WriteLine(ex, "Exception executing IStartupAction {0}: ", lazyStartupModule.Value.GetType());
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

            // Fail fast, this should halt the startup when the stop is called
            _startupCancellationTokenSource.Cancel();

            if (AutoShutdown)
            {
                await ShutdownAsync(cancellationToken).ConfigureAwait(false);
            }
            return await base.StopAsync(cancellationToken).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public void CancelStartup()
        {
            if (!_startupCancellationTokenSource.IsCancellationRequested)
            {
                _startupCancellationTokenSource.Cancel();
            }
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