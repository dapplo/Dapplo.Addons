using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Dapplo.Log;

namespace Dapplo.Addons.Bootstrapper.Handler
{
    /// <summary>
    /// This handles the startup of all IStartupModule implementing classes
    /// </summary>
    public class StartupHandler
    {
        private static readonly LogSource Log = new LogSource();
        private readonly IEnumerable<Lazy<IStartupMarker, StartupOrderAttribute>> _startupModules;

        /// <summary>
        /// The constructor to specify the startup modules
        /// </summary>
        /// <param name="startupModules">IEnumerable</param>
        public StartupHandler(IEnumerable<Lazy<IStartupMarker, StartupOrderAttribute>> startupModules)
        {
            _startupModules = startupModules;
        }

        /// <summary>
        /// Do the startup
        /// </summary>
        /// <param name="cancellationToken">CancellationToken</param>
        /// <returns>Task</returns>
        public async Task StartupAsync(CancellationToken cancellationToken = default)
        {
            Log.Debug().WriteLine("Checking what needs to startup.");

            var orderedStartupModules = from startupModule in _startupModules orderby startupModule.Metadata.StartupOrder select startupModule;

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

            foreach (var lazyStartupModule in orderedStartupModules)
            {
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
                    // Fail fast for when the stop is called during startup
                    if (startupCancellationTokenSource.IsCancellationRequested)
                    {
                        Log.Debug().WriteLine("Startup cancelled.");
                        break;
                    }

                    IStartupMarker startupModule;
                    try
                    {
                        startupModule = lazyStartupModule.Value;
                    }
                    catch (Exception ex)
                    {
                        Log.Error().WriteLine(ex, "Exception instantiating IStartupMarker.");
                        throw;
                    }

                    Task startupTask = null;

                    // Test if async / sync startup
                    switch (startupModule)
                    {
                        case IStartup startupAction:
                            if (Log.IsVerboseEnabled())
                            {
                                Log.Verbose().WriteLine("Trying to start {0}", startupAction.GetType());
                            }
                            // Wrap sync call as async task
                            startupTask = Task.Run(() => startupAction.Start(), cancellationToken);
                            break;
                        case IStartupAsync asyncStartupAction:
                            if (Log.IsVerboseEnabled())
                            {
                                Log.Verbose().WriteLine("Trying to start {0}", asyncStartupAction.GetType());
                            }
                            // Create a task (it will start running, but we don't await it yet)
                            startupTask = asyncStartupAction.StartAsync(cancellationToken);
                            break;
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
                    Log.Error().WriteLine(ex, "Exception executing IStartup {0}: ", lazyStartupModule.Value.GetType());
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
