// Dapplo - building blocks for .NET applications
// Copyright (C) 2016-2019 Dapplo
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

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Autofac.Features.Indexed;
using Autofac.Features.Metadata;
using Dapplo.Addons.Services;

namespace Dapplo.Addons.Bootstrapper.Services
{
    /// <summary>
    /// This handles the startup of all IService implementing classes
    /// </summary>
    public class ServiceStartupShutdown : ServiceNodeContainer<IService>, IStartupAsync, IShutdownAsync
    {
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
                var startup = serviceNode.TryBeginStartup();
                if (!startup)
                {
                    if (serviceNode.Dependencies.Count == 0)
                    {
                        continue;
                    }
                }

                var startupTask = Task.Run(async () =>
                {
                    if (startup)
                    {
                        TaskScheduler taskScheduler = null;
                        if (!string.IsNullOrEmpty(serviceNode.Details.TaskSchedulerName))
                        {
                            _taskSchedulers.TryGetValue(serviceNode.Details.TaskSchedulerName, out taskScheduler);
                        }
                        await serviceNode.Startup(taskScheduler, cancellationToken).ConfigureAwait(false);
                    }

                    if (serviceNode.Dependencies.Count > 0)
                    {
                        // Recurse into StartServices
                        await StartServices(serviceNode.Dependencies, cancellationToken).ConfigureAwait(false);
                    }
                }, cancellationToken);
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
                var shutdown = serviceNode.TryBeginShutdown();
                if (!shutdown)
                {
                    if (serviceNode.Prerequisites.Count == 0)
                    {
                        continue;
                    }
                }
                var shutdownTask = Task.Run(async () =>
                {
                    if (shutdown)
                    {
                        TaskScheduler taskScheduler = null;
                        if (!string.IsNullOrEmpty(serviceNode.Details.TaskSchedulerName))
                        {
                            _taskSchedulers.TryGetValue(serviceNode.Details.TaskSchedulerName, out taskScheduler);
                        }
                        await serviceNode.Shutdown(taskScheduler, cancellationToken).ConfigureAwait(false);
                    }

                    if (serviceNode.Prerequisites.Count > 0)
                    {
                        // Recurse into StartServices
                        await StopServices(serviceNode.Prerequisites, cancellationToken).ConfigureAwait(false);
                    }
                }, cancellationToken);
                tasks.Add(shutdownTask);
            }
            return tasks.Count > 0 ? Task.WhenAll(tasks) : Task.CompletedTask;
        }
    }
}
