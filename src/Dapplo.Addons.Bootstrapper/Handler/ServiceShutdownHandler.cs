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
    /// This handles the shutdown of all IShutdownMarker implementing services
    /// </summary>
    public class ServiceShutdownHandler
    {
        private static readonly LogSource Log = new LogSource();
        private readonly IEnumerable<Lazy<IShutdownMarker, ServiceOrderAttribute>> _servicesToShutdown;
 
        /// <summary>
        /// This is the constructo used to specify the modules
        /// </summary>
        /// <param name="servicesToShutdown"></param>
        public ServiceShutdownHandler(IEnumerable<Lazy<IShutdownMarker, ServiceOrderAttribute>> servicesToShutdown)
        {
            _servicesToShutdown = servicesToShutdown;
        }

        /// <summary>
        /// Start the shutdown
        /// </summary>
        /// <param name="cancellationToken">CancellationToken</param>
        public async Task ShutdownAsync(CancellationToken cancellationToken = default)
        {
            Log.Debug().WriteLine("Shutdown of services, if any");
            var orderedServicesToShutdown = from shutdownModule in _servicesToShutdown orderby shutdownModule.Metadata.ShutdownOrder select shutdownModule;

            var tasks = new List<KeyValuePair<Type, Task>>();

            // Variable used for grouping the shutdowns
            var groupingOrder = int.MaxValue;

            foreach (var lazyShutdownModule in orderedServicesToShutdown)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    Log.Debug().WriteLine("Shutdown cancelled.");
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
                IShutdownMarker shutdownModule;
                try
                {
                    shutdownModule = lazyShutdownModule.Value;
                }
                catch (Exception ex)
                {
                    Log.Error().WriteLine(ex, "Exception instantiating IShutdownMarker (ignoring in shutdown)");
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
                    switch (shutdownModule)
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
                        tasks.Add(new KeyValuePair<Type, Task>(shutdownModule.GetType(), shutdownTask));
                    }
                }
                catch (Exception ex)
                {
                    Log.Error().WriteLine(ex, "Exception executing shutdown {0}: ", shutdownModule.GetType());
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
