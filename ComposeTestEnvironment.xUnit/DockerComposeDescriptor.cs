﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ComposeTestEnvironment.xUnit
{
    public abstract class DockerComposeDescriptor
    {
        private IPortRenter? _portRenter;

        /// <summary>
        /// Detect run type (under docker-compose or from ide/command line)
        /// </summary>
        public virtual bool IsUnderCompose
            => !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("UNDER_COMPOSE"));

        /// <summary>
        /// docker-compose file with environment (can be just file name, the library tries to find this file in parent directories)
        /// </summary>
        public abstract string ComposeFileName { get; }

        /// <summary>
        /// docker-compose project name.
        /// </summary>
        public virtual string ProjectName
            => GetType().Assembly?.GetName().Name ?? throw new NotSupportedException($"Null assembly is not supported (typeof({GetType().Name}).Assembly)");

        /// <summary>
        /// Timeout to start environment.
        /// </summary>
        public virtual TimeSpan StartTimeout { get; } = TimeSpan.FromSeconds(40);

        /// <summary>
        /// Timeout to stop environment.
        /// </summary>
        public virtual TimeSpan StopTimeout { get; } = TimeSpan.FromSeconds(20);

        /// <summary>
        /// kill docker-compose environment after all tests executed (it is applicable only when run tests in IDE mode)
        /// </summary>
        public virtual bool DownOnComplete { get; } = true;

        /// <summary>
        /// By default - remove all non image based service from original docker-compose file to exclude tests
        /// </summary>
        public virtual bool GenerateImageBasedCompose { get; } = true;

        /// <summary>
        /// By default docker runs all its containers locally, but it can run containers on different host.
        /// This property allow to specify docker host.
        /// </summary>
        public virtual string DockerHost => "localhost";

        /// <summary>
        /// true for external docker compose setup/terdown
        /// </summary>
        public virtual bool IsExternalCompose => false;

        /// <summary>
        /// Component to get ports for assigning to container ports.
        /// </summary>
        public virtual IPortRenter PortRenter
        {
            get
            {
                if (_portRenter == null)
                {
                    _portRenter = TryFindExistingEnvironment ? new SerialPortRenter(FreePort.DynamicPortStart) : new FreePortRenter(FreePort.DynamicPortStart);
                }

                return _portRenter;
            }
        }

        /// <summary>
        /// docker-compose service names to remove from docker-compose file
        /// </summary>
        public virtual IReadOnlyList<string> ServicesToRemove { get; } = Array.Empty<string>();

        /// <summary>
        /// Wait for specific messages in service logs to be sure that services are started and ready to process requests
        /// </summary>
        public virtual IReadOnlyList<string> StartedMessageMarkers { get; } = Array.Empty<string>();

        /// <summary>
        /// Services are ready to process requests when `Ports` are listened
        /// </summary>
        public virtual bool WaitForPortsListen { get; } = true;

        /// <summary>
        /// Try to find existing environment (if environment already exists - use it instead up new environment).
        ///
        /// See PortRenter and DownOnComplete.
        /// </summary>
        public virtual bool TryFindExistingEnvironment { get; } = false;

        /// <summary>
        /// Default ports by services.
        /// </summary>
        public abstract IReadOnlyDictionary<string, int[]> Ports { get; }

        /// <summary>
        /// Do not wait on these ports.
        /// </summary>
        public virtual IReadOnlyDictionary<string, int[]> IgnoreWaitForPortListening { get; } = new Dictionary<string, int[]>();

        /// <summary>
        /// Custom implementation to wait until services ready.
        /// </summary>
        /// <param name="discovery">Current discovery.</param>
        /// <returns>Task.</returns>
        public virtual Task WaitForReady(Discovery discovery)
            => Task.CompletedTask;

        /// <summary>
        /// This method allows to override docker compose service's environment variables (only applicable for test run outside of compose).
        /// </summary>
        /// <param name="serviceName">Service name.</param>
        /// <param name="existing">Existing specified in compose file environment variables.</param>
        /// <param name="discovery">Discovery to allow get host/port of the sibling services.</param>
        /// <returns></returns>
        public virtual Task<IReadOnlyDictionary<string, string>> GetEnvironment(string serviceName, IReadOnlyDictionary<string, string> existing, Discovery discovery)
            => Task.FromResult(existing);
    }
}
