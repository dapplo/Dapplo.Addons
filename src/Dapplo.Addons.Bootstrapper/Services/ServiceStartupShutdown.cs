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
using Autofac.Features.Indexed;
using Autofac.Features.Metadata;
using Dapplo.Addons.Services;
using Dapplo.Log;

namespace Dapplo.Addons.Bootstrapper.Services
{
    /// <summary>
    /// This handles the startup of all IService implementing classes
    /// </summary>
    public class ServiceStartupShutdown : ServiceNodeContainer<IService>, IStartupAsync, IShutdownAsync
    {
        private static readonly LogSource Log = new LogSource();
        private readonly IIndex<string, TaskScheduler> _taskSchedulers;

        /// <summary>
        /// The constructor to specify the startup modules
        /// </summary>
        /// <param name="services">IEnumerable</param>
        /// <param name="taskSchedulers">IIndex of TaskSchedulers</param>
        public ServiceStartupShutdown(IEnumerable<Meta<IService, ServiceAttribute>> services, IIndex<string, TaskScheduler> taskSchedulers)
            : base(services) => _taskSchedulers = taskSchedulers;

        /// <summary>
        /// Start the services, begin with the root nodes and than everything that depends on these, and so on
        /// </summary>
        /// <param name="cancellationToken">CancellationToken</param>
        /// <returns>Task to await startup</returns>
        public Task StartupAsync(CancellationToken cancellationToken = default)
        {
            var rootNodes = ServiceNodes.Values.Where(node => !node.HasPrerequisites);
            return StartServices(rootNodes, cancellationToken);
        }

        /// <summary>
        /// Stop the services, begin with the nodes without dependencies and than everything that this depends on, and so on
        /// </summary>
        /// <param name="cancellationToken">CancellationToken</param>
        /// <returns>Task to await startup</returns>
        public Task ShutdownAsync(CancellationToken cancellationToken = default)
        {
            var leafNodes = ServiceNodes.Values.Where(node => !node.HasDependencies);
            return StopServices(leafNodes, cancellationToken);
        }

        /// <summary>
        /// Create a task for the startup
        /// </summary>
        /// <param name="serviceNodes">IEnumerable with ServiceNode</param>
        /// <param name="cancellationToken">CancellationToken</param>
        /// <returns>Task</returns>
        private Task StartServices(IEnumerable<ServiceNode<IService>> serviceNodes, CancellationToken cancellationToken = default)
        {
            var tasks = new List<Task>();
            foreach (var serviceNode in serviceNodes)
            {
                Log.Debug().WriteLine("Starting {0} ({1})", serviceNode.Details.Name, serviceNode.Service.GetType());

                var startupTask = Task.CompletedTask;
                switch (serviceNode.Service)
                {
                    case IStartupAsync startupAsync:
                        startupTask = Run(startupAsync.StartupAsync, serviceNode.Details.TaskSchedulerName, cancellationToken); 
                        break;
                    case IStartup startup:
                        startupTask = Run(() => startup.Startup(), serviceNode.Details.TaskSchedulerName, cancellationToken);
                        break;
                }
                // ReSharper disable once ConvertIfStatementToConditionalTernaryExpression
                if (serviceNode.Dependencies.Count > 0)
                {
                    // Recurse into StartServices
                    startupTask = startupTask.ContinueWith(task => StartServices(serviceNode.Dependencies, cancellationToken), cancellationToken).Unwrap();
                }
                if (!serviceNode.Details.SkipAwait)
                {
                    tasks.Add(startupTask);
                }
            }

            return tasks.Count > 0 ? Task.WhenAll(tasks) : Task.CompletedTask;
        }

        /// <summary>
        /// Create a task for the stop
        /// </summary>
        /// <param name="serviceNodes">IEnumerable with ServiceNode</param>
        /// <param name="cancellationToken">CancellationToken</param>
        /// <returns>Task</returns>
        private Task StopServices(IEnumerable<ServiceNode<IService>> serviceNodes, CancellationToken cancellationToken = default)
        {
            var tasks = new List<Task>();
            foreach (var serviceNode in serviceNodes)
            {
                if (!serviceNode.StartShutdown())
                {
                    // Shutdown was already started
                    continue;
                }
                var shutdownTask = Task.CompletedTask;

                Log.Debug().WriteLine("Stopping {0} ({1})", serviceNode.Details.Name, serviceNode.Service.GetType());
                switch (serviceNode.Service)
                {
                    case IShutdownAsync shutdownAsync:
                        shutdownTask = Run(shutdownAsync.ShutdownAsync, serviceNode.Details.TaskSchedulerName, cancellationToken);
                        break;
                    case IShutdown shutdown:
                        shutdownTask = Run(() => shutdown.Shutdown(), serviceNode.Details.TaskSchedulerName, cancellationToken);
                        break;
                }
                if (serviceNode.Prerequisites.Count > 0)
                {
                    // Recurse into StartServices
                    shutdownTask = shutdownTask.ContinueWith(task => StopServices(serviceNode.Prerequisites, cancellationToken), cancellationToken).Unwrap();
                }
                tasks.Add(shutdownTask);
            }

            return tasks.Count > 0 ? Task.WhenAll(tasks) : Task.CompletedTask;
        }

        /// <summary>
        /// Helper method to start a task on a optional TaskScheduler
        /// </summary>
        /// <param name="func">Func accepting CancellationToken returning Task</param>
        /// <param name="taskSchedulerName">string</param>
        /// <param name="cancellationToken">CancellationToken</param>
        /// <returns>Task</returns>
        private Task Run(Func<CancellationToken, Task> func, string taskSchedulerName, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(taskSchedulerName))
            {
                // Threadpool
                return Task.Run(() => func(cancellationToken), cancellationToken);
            }

            // Use the supplied task scheduler
            return Task.Factory.StartNew(
                () => func(cancellationToken),
                cancellationToken,
                TaskCreationOptions.None,
                _taskSchedulers[taskSchedulerName]).Unwrap();
        }

        /// <summary>
        /// Helper method to start an action
        /// </summary>
        /// <param name="action">Action</param>
        /// <param name="taskSchedulerName">string</param>
        /// <param name="cancellationToken">CancellationToken</param>
        /// <returns>Task</returns>
        private Task Run(Action action, string taskSchedulerName, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(taskSchedulerName))
            {
                // Threadpool
                return Task.Run(action, cancellationToken);
            }

            // Use the supplied task scheduler
            return Task.Factory.StartNew(
                action,
                cancellationToken,
                TaskCreationOptions.None,
                _taskSchedulers[taskSchedulerName]);
        }
    }
}
