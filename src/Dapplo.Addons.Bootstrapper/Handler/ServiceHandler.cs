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
using Autofac.Features.Metadata;
using Dapplo.Addons.Bootstrapper.Internal;
using Dapplo.Log;

namespace Dapplo.Addons.Bootstrapper.Handler
{
    /// <summary>
    /// This handles the startup of all IService implementing classes
    /// </summary>
    public class ServiceHandler
    {
        private static readonly LogSource Log = new LogSource();
        private readonly IDictionary<string, ServiceNode> _serviceNodes;

        /// <summary>
        /// The constructor to specify the startup modules
        /// </summary>
        /// <param name="services">IEnumerable</param>
        public ServiceHandler(IEnumerable<Meta<IService, ServiceAttribute>> services)
        {
            _serviceNodes = CreateServiceDictionary(services);
        }

        /// <summary>
        /// Internal helper, used for the test cases too
        /// </summary>
        /// <param name="newServices">IEnumerable with Meta of IService and ServiceAttribute</param>
        /// <returns>IDictionary</returns>
        internal static IDictionary<string, ServiceNode> CreateServiceDictionary(IEnumerable<Meta<IService, ServiceAttribute>> newServices)
        {
            var serviceNodes = newServices.ToDictionary(meta => meta.Metadata.Name, meta => new ServiceNode
            {
                Details = meta.Metadata,
                Service = meta.Value
            });

            // Enrich the information
            foreach (var serviceNode in serviceNodes.Values)
            {
                var serviceAttribute = serviceNode.Details;
                // check if this depends on anything
                if (string.IsNullOrEmpty(serviceAttribute.DependsOn))
                {
                    // Doesn't depend
                    continue;
                }

                if (!serviceNodes.TryGetValue(serviceAttribute.Name, out var thisNode))
                {
                    throw new NotSupportedException($"Coudn't find service with ID {serviceAttribute.Name}");
                }
                if (!serviceNodes.TryGetValue(serviceAttribute.DependsOn, out var parentNode))
                {
                    throw new NotSupportedException($"Coudn't find service with ID {serviceAttribute.DependsOn}, service {serviceAttribute.Name} depends on this.");
                }
                thisNode.DependensOn.Add(parentNode);
                parentNode.Dependencies.Add(thisNode);
            }

            return serviceNodes;
        }

        /// <summary>
        /// Start the services, begin with the root nodes and than everything that depends on these, and so on
        /// </summary>
        /// <param name="cancellationToken">CancellationToken</param>
        /// <returns>Task to await startup</returns>
        public Task StartupAsync(CancellationToken cancellationToken = default)
        {
            var rootNodes = _serviceNodes.Values.Where(node => !node.IsDependendOn);
            return StartServices(rootNodes, cancellationToken);
        }

        /// <summary>
        /// Stop the services, begin with the nodes without dependencies and than everything that this depends on, and so on
        /// </summary>
        /// <param name="cancellationToken">CancellationToken</param>
        /// <returns>Task to await startup</returns>
        public Task ShutdownAsync(CancellationToken cancellationToken = default)
        {
            var leafNodes = _serviceNodes.Values.Where(node => !node.HasDependencies);
            return StopServices(leafNodes, cancellationToken);
        }

        /// <summary>
        /// Create a task for the startup
        /// </summary>
        /// <param name="serviceNodes">IEnumerable with ServiceNode</param>
        /// <param name="cancellation">CancellationToken</param>
        /// <returns>Task</returns>
        private Task StartServices(IEnumerable<ServiceNode> serviceNodes, CancellationToken cancellation = default)
        {
            var tasks = new List<Task>();
            foreach (var serviceNode in serviceNodes)
            {
                Log.Debug().WriteLine("Starting {0}", serviceNode.Service.GetType());

                switch (serviceNode.Service)
                {
                    case IStartupAsync startupAsync:
                        {
                            var serviceTask = startupAsync.StartAsync(cancellation);
                            // ReSharper disable once ConvertIfStatementToConditionalTernaryExpression
                            if (serviceNode.Dependencies.Count > 0)
                            {
                                // Recurse into StartServices
                                serviceTask = serviceTask.ContinueWith(task => StartServices(serviceNode.Dependencies, cancellation), cancellation).Unwrap();
                            }
                            tasks.Add(serviceTask);
                            break;
                        }
                    case IStartup startup:
                        {
                            var serviceTask = Task.Run(() => startup.Start(), cancellation);
                            // ReSharper disable once ConvertIfStatementToConditionalTernaryExpression
                            if (serviceNode.Dependencies.Count > 0)
                            {
                                // Recurse into StartServices
                                serviceTask = serviceTask.ContinueWith(task => StartServices(serviceNode.Dependencies, cancellation), cancellation).Unwrap();
                            }
                            tasks.Add(serviceTask);
                            break;
                        }
                }
            }

            return Task.WhenAll(tasks);
        }

        /// <summary>
        /// Create a task for the stop
        /// </summary>
        /// <param name="serviceNodes">IEnumerable with ServiceNode</param>
        /// <param name="cancellation">CancellationToken</param>
        /// <returns>Task</returns>
        private Task StopServices(IEnumerable<ServiceNode> serviceNodes, CancellationToken cancellation = default)
        {
            var tasks = new List<Task>();
            foreach (var serviceNode in serviceNodes)
            {
                if (!serviceNode.StartShutdown())
                {
                    // Shutdown was already started
                    continue;
                }
                Log.Debug().WriteLine("Stopping {0}", serviceNode.Service.GetType());
                switch (serviceNode.Service)
                {
                    case IShutdownAsync shutdownAsync:
                        {

                            var serviceTask = shutdownAsync.ShutdownAsync(cancellation);
                            if (serviceNode.DependensOn.Count > 0)
                            {
                                // Recurse into StartServices
                                serviceTask = serviceTask.ContinueWith(async task => await StopServices(serviceNode.DependensOn, cancellation), cancellation).Unwrap();
                            }
                            tasks.Add(serviceTask);
                            break;
                        }
                    case IShutdown shutdown:
                        {
                            var serviceTask = Task.Run(() => shutdown.Shutdown(), cancellation);
                            // ReSharper disable once ConvertIfStatementToConditionalTernaryExpression
                            if (serviceNode.DependensOn.Count > 0)
                            {
                                // Recurse into StartServices
                                serviceTask = serviceTask.ContinueWith(task => StopServices(serviceNode.DependensOn, cancellation), cancellation).Unwrap();
                            }
                            tasks.Add(serviceTask);
                            break;
                        }
                }
            }

            return Task.WhenAll(tasks);
        }
    }
}
