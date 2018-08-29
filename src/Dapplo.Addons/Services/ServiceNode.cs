#region Dapplo 2016-2018 - GNU Lesser General Public License

// Dapplo - building blocks for .NET applications
// Copyright (C) 2016-2018 Dapplo
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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Dapplo.Addons.Services
{
    /// <summary>
    /// This contains the information needed for the startup and shutdown of services
    /// </summary>
    public class ServiceNode<TService>
    {
        private bool _isStartupCalled;
        private readonly TaskCompletionSource<object> _startupTaskCompletionSource = new TaskCompletionSource<object>();
        private bool _isShutdownCalled;
        private readonly TaskCompletionSource<object> _shutdownTaskCompletionSource = new TaskCompletionSource<object>();

        /// <summary>
        /// 
        /// </summary>
        /// <param name="service">TService</param>
        /// <param name="details">ServiceAttribute</param>
        public ServiceNode(TService service, ServiceAttribute details)
        {
            Service = service;
            Details = details;
            var interfaces = service?.GetType().GetInterfaces();
            // If the type doesn't implement a shutdown, mark this as done
            if (interfaces == null || (!interfaces.Contains(typeof(IShutdownAsync)) && !interfaces.Contains(typeof(IShutdown))))
            {
                _isShutdownCalled = true;
                _shutdownTaskCompletionSource.SetResult(null);
            }
            // If the type doesn't implement a startup, mark this as done
            if (interfaces == null || (!interfaces.Contains(typeof(IStartupAsync)) && !interfaces.Contains(typeof(IStartup))))
            {
                _isStartupCalled = true;
                _startupTaskCompletionSource.SetResult(null);
            }
        }

        /// <summary>
        /// The attributed details
        /// </summary>
        public ServiceAttribute Details { get; }

        /// <summary>
        /// Task of the service
        /// </summary>
        public TService Service { get; }

        /// <summary>
        /// Test if this service depends on other services
        /// </summary>
        public bool HasPrerequisites => Prerequisites.Count > 0;

        /// <summary>
        /// The service which should be started before this
        /// </summary>
        public IList<ServiceNode<TService>> Prerequisites { get; } = new List<ServiceNode<TService>>();

        /// <summary>
        /// Test if this service has dependencies
        /// </summary>
        public bool HasDependencies => Dependencies.Count > 0;

        /// <summary>
        /// The services awaiting for this
        /// </summary>
        public IList<ServiceNode<TService>> Dependencies { get; } = new List<ServiceNode<TService>>();

        /// <summary>
        /// Helper method to coordinate the shutdown
        /// </summary>
        /// <returns>true if you can begin the shutdown</returns>
        public bool TryBeginShutdown()
        {
            lock (_shutdownTaskCompletionSource)
            {
                if (_isShutdownCalled)
                {
                    return false;
                }

                return _isShutdownCalled = true;
            }
        }

        /// <summary>
        /// Stop this service
        /// </summary>
        /// <param name="taskScheduler"></param>
        /// <param name="cancellationToken"></param>
        /// <returns>Task</returns>
        public async Task Shutdown(TaskScheduler taskScheduler, CancellationToken cancellationToken = default)
        {
            var dependencyTasks = Dependencies.Select(node => node._shutdownTaskCompletionSource.Task).ToArray();
            if (dependencyTasks.Length == 1)
            {
                await dependencyTasks[0].ConfigureAwait(false);
            }
            else if (dependencyTasks.Length > 1)
            {
                await Task.WhenAll(dependencyTasks).ConfigureAwait(false);
            }

            switch (Service)
            {
                case IShutdownAsync shutdownAsync:
                    await Run(shutdownAsync.ShutdownAsync, taskScheduler, _shutdownTaskCompletionSource, cancellationToken).ConfigureAwait(false);
                    break;
                case IShutdown shutdown:
                    await Run(shutdown.Shutdown, taskScheduler, _shutdownTaskCompletionSource, cancellationToken).ConfigureAwait(false);
                    break;
            }
        }

        /// <summary>
        /// Helper method to coordinate the startup
        /// </summary>
        /// <returns>true if you can begin the startup</returns>
        public bool TryBeginStartup()
        {
            lock (_startupTaskCompletionSource)
            {
                if (_isStartupCalled)
                {
                    return false;
                }

                return _isStartupCalled = true;
            }
        }

        /// <summary>
        /// Start this service
        /// </summary>
        /// <param name="taskScheduler"></param>
        /// <param name="cancellationToken"></param>
        /// <returns>Task</returns>
        public async Task Startup(TaskScheduler taskScheduler, CancellationToken cancellationToken = default)
        {
            var prerequisiteTasks = Prerequisites.Select(node => node._startupTaskCompletionSource.Task).ToArray();

            if (prerequisiteTasks.Length == 1)
            {
                await prerequisiteTasks[0].ConfigureAwait(false);
            }
            else if (prerequisiteTasks.Length > 1)
            {
                await Task.WhenAll(prerequisiteTasks).ConfigureAwait(false);
            }

            switch (Service)
            {
                case IStartupAsync startupAsync:
                    await Run(startupAsync.StartupAsync, taskScheduler, _startupTaskCompletionSource, cancellationToken).ConfigureAwait(false);
                    break;
                case IStartup startup:
                    await Run(startup.Startup, taskScheduler, _startupTaskCompletionSource, cancellationToken).ConfigureAwait(false);
                    break;
           }
        }

        /// <summary>
        /// Start a task on a optional TaskScheduler
        /// </summary>
        /// <param name="func">Func accepting CancellationToken returning Task</param>
        /// <param name="taskScheduler">TaskScheduler</param>
        /// <param name="tcs">TaskCompletionSource</param>
        /// <param name="cancellationToken">CancellationToken</param>
        /// <returns>Task</returns>
        private static Task Run(Func<CancellationToken, Task> func, TaskScheduler taskScheduler, TaskCompletionSource<object> tcs, CancellationToken cancellationToken = default)
        {
            if (taskScheduler == null)
            {
                // Threadpool
                return Task.Run(async () =>
                {
                    try
                    {
                        await func(cancellationToken).ConfigureAwait(false);
                        tcs.TrySetResult(null);
                    }
                    catch (Exception ex)
                    {
                        tcs.TrySetException(ex);
                        throw;
                    }
                    
                }, cancellationToken);
            }

            // Use the supplied task scheduler
            return Task.Factory.StartNew(
                async () =>
                {
                    try
                    {
                        await func(cancellationToken).ConfigureAwait(false);
                        tcs.TrySetResult(null);
                    }
                    catch (Exception ex)
                    {
                        tcs.TrySetException(ex);
                        throw;
                    }
                },
                cancellationToken,
                TaskCreationOptions.None,
                taskScheduler).Unwrap();
        }

        /// <summary>
        /// Helper method to start an action
        /// </summary>
        /// <param name="action">Action</param>
        /// <param name="taskScheduler">TaskScheduler</param>
        /// <param name="tcs">TaskCompletionSource</param>
        /// <param name="cancellationToken">CancellationToken</param>
        /// <returns>Task</returns>
        private static Task Run(Action action, TaskScheduler taskScheduler, TaskCompletionSource<object> tcs, CancellationToken cancellationToken = default)
        {
            if (taskScheduler == null)
            {
                // Threadpool
                return Task.Run(() =>
                {
                    try
                    {
                        action();
                        tcs.TrySetResult(null);
                    }
                    catch (Exception ex)
                    {
                        tcs.TrySetException(ex);
                        throw;
                    }
                }, cancellationToken);
            }

            // Use the supplied task scheduler
            return Task.Factory.StartNew(
                () =>
                {
                    try
                    {
                        action();
                        tcs.TrySetResult(null);
                    }
                    catch (Exception ex)
                    {
                        tcs.TrySetException(ex);
                        throw;
                    }
                },
                cancellationToken,
                TaskCreationOptions.None,
                taskScheduler);
        }
    }
}
