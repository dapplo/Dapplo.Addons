using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Dapplo.Log;

namespace Dapplo.Addons.Bootstrapper.Handler
{
    /// <summary>
    /// This handles the shutdown of all IShutdownModule implementing classes
    /// </summary>
    public class ShutdownHandler
    {
        private static readonly LogSource Log = new LogSource();
        private readonly IEnumerable<Lazy<IShutdownMarker, ShutdownOrderAttribute>> _shutdownModules;
 
        /// <summary>
        /// This is the constructo used to specify the modules
        /// </summary>
        /// <param name="shutdownModules"></param>
        public ShutdownHandler(IEnumerable<Lazy<IShutdownMarker, ShutdownOrderAttribute>> shutdownModules)
        {
            _shutdownModules = shutdownModules;
        }

        /// <summary>
        /// Start the shutdown
        /// </summary>
        /// <param name="cancellationToken">CancellationToken</param>
        public async Task ShutdownAsync(CancellationToken cancellationToken = default)
        {
            Log.Debug().WriteLine("Shutdown of the shutdown actions, if any");
            var orderedShutdownModules = from shutdownModule in _shutdownModules orderby shutdownModule.Metadata.ShutdownOrder select shutdownModule;

            var tasks = new List<KeyValuePair<Type, Task>>();

            // Variable used for grouping the shutdowns
            var groupingOrder = int.MaxValue;

            foreach (var lazyShutdownModule in orderedShutdownModules)
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
                    Log.Error().WriteLine(ex, "Exception executing IShutdownModule {0}: ", shutdownModule.GetType());
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
