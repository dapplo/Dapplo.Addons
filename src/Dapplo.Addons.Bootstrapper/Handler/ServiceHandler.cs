#region Dapplo 2016-2018 - GNU Lesser General Public License

// Dapplo - building blocks for .NET applications
// Copyright (C) 2016-2018 Dapplo
// 
// For more information see: http://dapplo.net/
// Dapplo repositories are hosted on GitHub: https://github.com/dapplo
// 
// This file is part of Dapplo.CaliburnMicro
// 
// Dapplo.CaliburnMicro is free software: you can redistribute it and/or modify
// it under the terms of the GNU Lesser General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// Dapplo.CaliburnMicro is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU Lesser General Public License for more details.
// 
// You should have a copy of the GNU Lesser General Public License
// along with Dapplo.CaliburnMicro. If not, see <http://www.gnu.org/licenses/lgpl.txt>.

#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Dapplo.Log;

namespace Dapplo.Addons.Bootstrapper.Handler
{
    /// <summary>
    /// This handles the startup of all IService implementing classes
    /// </summary>
    public class ServiceHandler
    {
        private static readonly LogSource Log = new LogSource();
        private readonly IEnumerable<Lazy<IService, ServiceOrderAttribute>> _services;

        /// <summary>
        /// The constructor to specify the startup modules
        /// </summary>
        /// <param name="services">IEnumerable</param>
        public ServiceHandler(IEnumerable<Lazy<IService, ServiceOrderAttribute>> services)
        {
            _services = services;
        }

        /// <summary>
        /// Do the startup of the services who can startup
        /// </summary>
        /// <param name="cancellationToken">CancellationToken</param>
        /// <returns>Task</returns>
        public async Task StartupAsync(CancellationToken cancellationToken = default)
        {
            Log.Debug().WriteLine("Checking what needs to startup.");

            var orderedServicesToStartup = from startupModule in _services orderby startupModule.Metadata.StartupOrder select startupModule;

            var tasks = new List<KeyValuePair<Type, Task>>();
            var nonAwaitables = new List<KeyValuePair<Type, Task>>();

            // Variable used for grouping the startups
            var groupingOrder = int.MaxValue;

            var startupCancellationTokenSource = new CancellationTokenSource();

            // Map the supplied cancellationToken to the _startupCancellationTokenSource, if the first cancells, _startupCancellationTokenSource also does
            var cancellationTokenRegistration = default(CancellationTokenRegistration);
            if (cancellationToken.CanBeCanceled)
            {
                cancellationTokenRegistration = cancellationToken.Register(() => startupCancellationTokenSource.Cancel());
            }

            foreach (var lazyService in orderedServicesToStartup)
            {
                try
                {
                    // Check if we have all the services belonging to a group
                    if (tasks.Count > 0 && groupingOrder != lazyService.Metadata.StartupOrder)
                    {
                        groupingOrder = lazyService.Metadata.StartupOrder;
                        // Await all belonging to the same order "group"
                        await WhenAll(tasks).ConfigureAwait(false);
                        // Clean the tasks, we are finished.
                        tasks.Clear();
                    }
                    // Fail fast for when the stop is called during startup
                    if (startupCancellationTokenSource.IsCancellationRequested)
                    {
                        Log.Debug().WriteLine("Startup cancelled.");
                        break;
                    }

                    IService service;
                    try
                    {
                        service = lazyService.Value;
                    }
                    catch (Exception ex)
                    {
                        Log.Error().WriteLine(ex, "Exception instantiating service.");
                        throw;
                    }

                    Task startupTask = null;

                    if (Log.IsVerboseEnabled())
                    {
                        Log.Verbose().WriteLine("Starting {0}", service.GetType());
                    }
                    // Test if async / sync startup
                    switch (service)
                    {
                        case IStartup serviceToStartup:
                            // Wrap sync call as async task
                            startupTask = Task.Run(() => serviceToStartup.Start(), cancellationToken);
                            break;
                        case IStartupAsync serviceToStartupAsync:
                            // Create a task (it will start running, but we don't await it yet)
                            startupTask = serviceToStartupAsync.StartAsync(cancellationToken);
                            break;
                    }

                    if (startupTask != null)
                    {
                        if (lazyService.Metadata.AwaitStart)
                        {
                            tasks.Add(new KeyValuePair<Type, Task>(lazyService.Value.GetType(), startupTask));
                        }
                        else
                        {
                            if (Log.IsErrorEnabled())
                            {
                                // We do await for them, but just to catch any exceptions
                                nonAwaitables.Add(new KeyValuePair<Type, Task>(lazyService.Value.GetType(), startupTask));
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
                    Log.Error().WriteLine(ex, "Exception executing startup {0}: ", lazyService.Value.GetType());
                }
                if (cancellationToken.CanBeCanceled)
                {
                    cancellationTokenRegistration.Dispose();
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
                await Task.Run(async () =>
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
        /// Start the shutdown
        /// </summary>
        /// <param name="cancellationToken">CancellationToken</param>
        public async Task ShutdownAsync(CancellationToken cancellationToken = default)
        {
            Log.Debug().WriteLine("Shutdown of services, if any");
            var orderedServicesToShutdown = from shutdownModule in _services orderby shutdownModule.Metadata.ShutdownOrder descending select shutdownModule;

            var tasks = new List<KeyValuePair<Type, Task>>();

            // Variable used for grouping the shutdowns
            var groupingOrder = int.MinValue;

            foreach (var lazyService in orderedServicesToShutdown)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    Log.Debug().WriteLine("Shutdown cancelled.");
                    break;
                }
                // Check if we have all the startup actions belonging to a group
                if (tasks.Count > 0 && groupingOrder != lazyService.Metadata.ShutdownOrder)
                {
                    groupingOrder = lazyService.Metadata.ShutdownOrder;

                    // Await all belonging to the same order "group"
                    await WhenAll(tasks).ConfigureAwait(false);
                    // Clean the tasks, we are finished.
                    tasks.Clear();
                }
                IService service;
                try
                {
                    service = lazyService.Value;
                }
                catch (Exception ex)
                {
                    Log.Error().WriteLine(ex, "Exception instantiating IService (ignoring in shutdown)");
                    continue;
                }

                if (Log.IsDebugEnabled())
                {
                    Log.Debug().WriteLine("Stopping {0}", service.GetType());
                }

                try
                {
                    Task shutdownTask = null;
                    // Test if async / sync shutdown
                    switch (service)
                    {
                        case IShutdown shutdownAction:
                            shutdownTask = Task.Run(() => shutdownAction.Shutdown(), cancellationToken);
                            break;
                        case IShutdownAsync asyncShutdownAction:
                            // Create a task (it will start running, but we don't await it yet)
                            shutdownTask = asyncShutdownAction.ShutdownAsync(cancellationToken);
                            break;
                    }
                    if (shutdownTask != null)
                    {
                        // Store it for awaiting
                        tasks.Add(new KeyValuePair<Type, Task>(service.GetType(), shutdownTask));
                    }
                }
                catch (Exception ex)
                {
                    Log.Error().WriteLine(ex, "Exception executing shutdown {0}: ", service.GetType());
                }
            }
            // Await all remaining tasks, as the system is shutdown we NEED to wait, and ignore but log their exceptions
            if (tasks.Count > 0)
            {
                await WhenAll(tasks).ConfigureAwait(false);
            }
        }

        /// <summary>
        ///     Special WhenAll, this awaits the supplied values and log any exceptions they had.
        ///     This is not optimized, like Task.WhenAll...
        /// </summary>
        /// <param name="tasksToAwait"></param>
        /// <param name="ignoreExceptions">if true (default) the exceptions will be logged but ignored.</param>
        /// <returns>Task</returns>
        private static async Task WhenAll(IEnumerable<KeyValuePair<Type, Task>> tasksToAwait, bool ignoreExceptions = true)
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
