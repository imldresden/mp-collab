using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

namespace IMLD.MixedReality.Network
{
    public interface INetworkServiceManager : IService
    {
        /// <summary>
        /// Event raised when connected or disconnected from a service.
        /// </summary>
        public event EventHandler<EventArgs> ConnectionStatusChanged;

        /// <summary>
        /// Event raised when the list of available services changes.
        /// </summary>
        public event EventHandler<EventArgs> AvailableServicesChanged;

        ///// <summary>
        ///// The list of services that have been discovered on the network.
        ///// </summary>
        //public List<NetworkServiceDescription> AvailableServices { get; }

        public List<NetworkServiceDescription> GetAvailableServices(INetworkService.NetworkServiceFilter filter = INetworkService.NetworkServiceFilter.DEFAULT);

        /// <summary>
        /// The list of requested, i.e., connected or (re-)connecting, services.
        /// </summary>
        public List<INetworkService> RequestedServices { get; }

        public bool AutomaticallyReconnect { get; }

        public Dictionary<NetworkServiceDescription.ServiceType, INetworkService> ProvidedServices { get; }

        public Guid ClientId { get; }

        public bool TryConnectToService(NetworkServiceDescription serviceDescription, out INetworkService service, bool useMessageQueue = true);

        public bool TryReconnectToService(INetworkService service);

        public void DisconnectFromService(INetworkService service);

        public INetworkService StartServer(NetworkServiceDescription.ServiceType serviceType, string serviceData = "", bool useMessageQueue = true);

        public void StopServer(NetworkServiceDescription.ServiceType serviceType);

        public void StartDiscovery();

        public void StopDiscovery();

        /// <summary>
        /// Sets the requested latency for all currently connected and future network services.
        /// Messages with an estimated latency below this are delayed until this latency is reached.
        /// </summary>
        /// <param name="requestedLatency">the requested latency in seconds</param>
        public void SetTargetNetworkLatency(float requestedLatency);

        /// <summary>
        /// Returns the maximum estimated network latency over all connected services.
        /// </summary>
        /// <returns>the estimated latency in seconds</returns>
        public float GetEstimatedNetworkLatency();

        /// <summary>
        /// Returns the requested network latency for all currently connected and future network services.
        /// </summary>
        /// <returns>the requested latency in seconds</returns>
        public float GetTargetNetworkLatency();
    }
}

