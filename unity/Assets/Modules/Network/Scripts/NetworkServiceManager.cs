using IMLD.MixedReality.Core;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.InputSystem;

namespace IMLD.MixedReality.Network
{
    public class NetworkServiceManager : MonoBehaviour, INetworkServiceManager
    {
        [SerializeField]
        private int _listeningPort;

        [SerializeField]
        private bool _automaticallyStartDiscovery = false;

        [SerializeField]
        private bool _logNetworkPackages = false;

        [SerializeField]
        private string _logPath;


        //private NetworkTransportWebsocket _serviceListener;
        private NetworkTransport _serviceListener;
        protected ISessionManager _sessionManager;
        private float _requestedLatency = 0;

        public Dictionary<Guid,NetworkServiceDescription> AvailableServices { get; private set; } = new Dictionary<Guid, NetworkServiceDescription>();

        public List<INetworkService> RequestedServices { get; private set; } = new List<INetworkService>();

        public Dictionary<NetworkServiceDescription.ServiceType, INetworkService> ProvidedServices { get; private set; } = new Dictionary<NetworkServiceDescription.ServiceType, INetworkService>();

        public bool AutomaticallyReconnect { get; private set; } = true;

        public Guid ClientId { get; private set; }
        public IReadOnlyList<Type> Dependencies { get; } = new List<Type> { typeof(ISessionManager) };

        public event EventHandler<EventArgs> ConnectionStatusChanged;
        public event EventHandler<EventArgs> AvailableServicesChanged;

        private bool dropTestRunning = false;

        public void SetTargetNetworkLatency(float requestedLatency)
        {
            if (requestedLatency < 0)
            {
                _requestedLatency = 0;
            }
            else
            {
                _requestedLatency = requestedLatency;
            }
            
            foreach (var connectedService in RequestedServices)
            {
                connectedService.RequestedLatency = _requestedLatency;
            }
        }
        public float GetTargetNetworkLatency()
        {
            return _requestedLatency;
        }

        public float GetEstimatedNetworkLatency()
        {
            float latency = 0;
            foreach (var connectedService in RequestedServices)
            {
                if (connectedService.Status == INetworkService.NetworkServiceStatus.CONNECTED)
                {
                    latency = Mathf.Max(latency, connectedService.ServerLatency);
                }
            }

            return latency;
        }

        public virtual bool TryConnectToService(NetworkServiceDescription serviceDescription, out INetworkService service, bool useMessageQueue = true)
        {
            // check connected services to prevent connecting twice
            foreach(var connectedService in RequestedServices)
            {
                if (connectedService.ServiceDescription.ServiceId == serviceDescription.ServiceId)
                {
                    service = connectedService; // set out parameter

                    if (connectedService.Status == INetworkService.NetworkServiceStatus.DISCONNECTED)
                    {
                        TryReconnectToService(connectedService);
                        return true;
                    }
                    else if (connectedService.Status == INetworkService.NetworkServiceStatus.CONNECTED)
                    {
                        Debug.LogWarning("Cannot connect to service " + serviceDescription.HostName + ": " + Enum.GetName(typeof(NetworkServiceDescription.ServiceType), serviceDescription.Type) + ". Already connected to service!");
                        return false;
                    }
                    else if (connectedService.Status == INetworkService.NetworkServiceStatus.CONNECTING)
                    {
                        Debug.LogWarning("Cannot connect to service " + serviceDescription.HostName + ": " + Enum.GetName(typeof(NetworkServiceDescription.ServiceType), serviceDescription.Type) + ". Already trying to connect to service!");
                        return false;
                    }
                    else
                    {
                        Debug.LogWarning("Cannot connect to service " + serviceDescription.HostName + ": " + Enum.GetName(typeof(NetworkServiceDescription.ServiceType), serviceDescription.Type) + ".");
                        return false;
                    }
                }
            }

            // create new game object & attach network service component
            var go = new GameObject("[Client " + serviceDescription.HostName + ": " + Enum.GetName(typeof(NetworkServiceDescription.ServiceType), serviceDescription.Type) + "]");
            go.transform.SetParent(transform);
            var networkService = go.AddComponent<NetworkService>();

            // configure and start network service
            networkService.RequestedLatency = _requestedLatency;
            networkService.StartAsClient(serviceDescription, useMessageQueue);

            // add network service to collection of connected services
            RequestedServices.Add(networkService);

            // attach log writer if setting is enabled
            if (_logNetworkPackages)
            {
                FileWriterNetworkFilter filter = new FileWriterNetworkFilter(_logPath + Enum.GetName(typeof(NetworkServiceDescription.ServiceType), serviceDescription.Type) + "_" + serviceDescription.ServiceId.ToString() + ".bin");
                networkService.NetworkFilter = filter;
            }

            // attach callback for connection status
            networkService.DisconnectedFromServer += OnServiceDisconnected;
            networkService.ConnectedToServer += OnServiceConnected;

            // return new network service
            service = networkService;
            return true;
        }

        public bool TryReconnectToService(INetworkService service)
        {
            Debug.Log("Reconnecting to " + service.ServiceDescription.Description + "...");
            bool result = service.StartAsClient(service.ServiceDescription);
            return result;
        }

        private void OnServiceConnected(object sender, EventArgs e)
        {
            var service = sender as INetworkService;
            if (service != null)
            {
                Debug.Log("Connected to " + service.ServiceDescription.Description);
            }
        }
        void Update()
        {
            if (Keyboard.current.gKey.wasPressedThisFrame)
            {
                foreach (var service in ProvidedServices.Values)
                {
                    if (service.ServiceType == NetworkServiceDescription.ServiceType.KINECT_DATA)
                    {
                        if (dropTestRunning)
                        {
                            service.StartAsServer();
                        }
                        else
                        {
                            ((NetworkService)service).Drop();
                        }
                    }
                }
            }
        }

        private void OnServiceDisconnected(object sender, EventArgs e)
        {
            var service = sender as INetworkService;

            Debug.Log("Disconnected from " + service.ServiceDescription.Description);

            // check if the service exists & remove it from lists
            RequestedServices.Remove(service);
            AvailableServices.Remove(service.ServiceDescription.ServiceId);

            // clean up
            service.ConnectedToServer -= OnServiceConnected;
            service.DisconnectedFromServer -= OnServiceDisconnected;
            service.Destroy();

            // destroy game object
            if (service is MonoBehaviour)
            {
                Destroy((MonoBehaviour)service);
            }


            //// Is auto reconnect active?
            //if (AutomaticallyReconnect == false)
            //{
            //    // check if the service exists & remove it from list
            //    RequestedServices.Remove(service);

            //    // clean up
            //    service.ConnectedToServer -= OnServiceConnected;
            //    service.DisconnectedFromServer -= OnServiceDisconnected;
            //    service.Destroy();
            //}
        }

        /// <summary>
        /// Starts a UDP socket to listen for service announcements on the configured port.
        /// </summary>
        public virtual void StartDiscovery()
        {
            _serviceListener.MessageReceived += OnServiceAnnouncementReceived;
            _serviceListener.StartListening(_listeningPort);
        }

        private void OnServiceAnnouncementReceived(object sender, MessageContainer message)
        {
            if (message.Type == MessageContainer.MessageType.ANNOUNCEMENT)
            {
                var Announcement = MessageAnnouncement.Unpack(message);

                // check if the discovered service is actually one of ours
                if (ProvidedServices.TryGetValue(Announcement.Service.Type, out var service) && service.ServiceDescription.ServiceId == Announcement.Service.ServiceId)
                {
                    return;
                }
                
                AvailableServices[Announcement.Service.ServiceId] = Announcement.Service;
                AvailableServicesChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        protected void OnAvailableServicesChanged()
        {
            AvailableServicesChanged?.Invoke(this, EventArgs.Empty);
        }

        public void DisconnectFromService(INetworkService service)
        {
            // check if the service exists & remove it from list
            if (RequestedServices.Remove(service))
            {
                service.DisconnectedFromServer -= OnServiceDisconnected;
                // stop & dispose the network service
                service.Destroy();
            }
        }

        public virtual INetworkService StartServer(NetworkServiceDescription.ServiceType serviceType, string data = "", bool useMessageQueue = true)
        {
            // create new game object & attach network service component
            var go = new GameObject();
            go.transform.SetParent(transform);
            var networkService = go.AddComponent<NetworkService>();

            // configure and start network service
            networkService.ServiceType = serviceType;
            networkService.ServiceData = data;
            networkService.StartAsServer(useMessageQueue);

            // rename game object
            go.name = "[Server " + networkService.ServiceDescription.HostName + ": " + Enum.GetName(typeof(NetworkServiceDescription.ServiceType), networkService.ServiceDescription.Type) + "]";

            // add network service to collection of provided services
            ProvidedServices.Add(serviceType, networkService);

            // return new network service
            return networkService;
        }

        public void StopServer(NetworkServiceDescription.ServiceType serviceType)
        {
            // check if the service exists
            if (ProvidedServices.TryGetValue(serviceType, out INetworkService networkService))
            {
                // remove network service from the provided services collection
                ProvidedServices.Remove(serviceType);

                // stop & dispose the network service
                networkService.Destroy();
            }
        }

        public virtual void StopDiscovery()
        {
            if (_serviceListener != null)
            {
                _serviceListener.MessageReceived -= OnServiceAnnouncementReceived;
                _serviceListener.StopListening();
            }
        }


        // Start is called before the first frame update
        void Start()
        {
            // check dependencies
            if (ServiceLocator.Instance.CheckDependencies(this) == false)
            {
                Debug.LogError("Missing dependency, disabling component.");
                enabled = false;
                return;
            }

            // get session manager
            _sessionManager = ServiceLocator.Instance.Get<ISessionManager>();

            // create a NetworkTransport that is used to discover services on the network.
            //_serviceListener = new GameObject("[Service Listener]").AddComponent<NetworkTransportWebsocket>();
            _serviceListener = new GameObject("[Service Listener]").AddComponent<NetworkTransport>();
            _serviceListener.gameObject.transform.SetParent(transform);

            

            // load client id from config or generate new client id
            var config = ServiceLocator.Instance.Get<Config>();
            if (config != null && config.TryLoad<string>("ClientId", out var clientId))
            {
                ClientId = Guid.Parse(clientId);
            }
            else
            {
                ClientId = Guid.NewGuid();
                if (config != null)
                {
                    config.Save("ClientId", ClientId.ToString());
                }
            }

            //// for testing only!
            //this.AvailableServicesChanged += OnAvailableServicesChanges;
            //this.StartServer(NetworkServiceDescription.ServiceType.APP_STATE);
            if (_automaticallyStartDiscovery)
            {
                StartDiscovery();
            }

            //StartCoroutine(ReconnectAutomatically());
        }

        //private IEnumerator ReconnectAutomatically()
        //{
        //    while (true)
        //    {
        //        if (AutomaticallyReconnect)
        //        {
        //            foreach (var service in RequestedServices)
        //            {
        //                if (service != null && service.Status == INetworkService.NetworkServiceStatus.DISCONNECTED)
        //                {
        //                    TryReconnectToService(service);
        //                }
        //            }
        //        }

        //        yield return new WaitForSeconds(1f);
        //    }
        //}

        private void OnApplicationQuit()
        {
            StopDiscovery();

            foreach(var service in RequestedServices)
            {
                // stop & dispose the network service
                service.Destroy();
            }

            RequestedServices.Clear();

            foreach(var service in ProvidedServices.Values)
            {
                // stop & dispose the network service
                service.Destroy();
            }

            ProvidedServices.Clear();
        }

        public virtual List<NetworkServiceDescription> GetAvailableServices(INetworkService.NetworkServiceFilter filter = INetworkService.NetworkServiceFilter.DEFAULT)
        {
            List<NetworkServiceDescription> availableServices = new List<NetworkServiceDescription>();
            foreach(NetworkServiceDescription service in AvailableServices.Values)
            {
                // don't connect to yourself
                if (ProvidedServices.TryGetValue(service.Type, out var providedService))
                {
                    if (providedService.ServiceDescription.ServiceId == service.ServiceId)
                    {
                        continue;
                    }
                }

                // don't connect to services that don't match your session ID (unless it is an announcement for a session...)
                if (service.SessionId != _sessionManager.SessionId && service.Type != NetworkServiceDescription.ServiceType.APP_STATE)
                {
                    continue;
                }

                // add service if the filter is set to "ALL"
                if (filter.HasFlag(INetworkService.NetworkServiceFilter.ALL))
                {
                    availableServices.Add(service);
                    continue;
                }
                // add service if the filter is set to "APP_STATE" and this is an app state service
                else if (filter.HasFlag(INetworkService.NetworkServiceFilter.APP_STATE) && service.Type == NetworkServiceDescription.ServiceType.APP_STATE)
                {
                    availableServices.Add(service);
                    continue;
                }
                // add service if the filter is set to "SAME_ROOM_ID" and this service has the same room id not equal to -1
                else if (filter.HasFlag(INetworkService.NetworkServiceFilter.SAME_ROOM_ID) && service.RoomId == _sessionManager.Room.Id && _sessionManager.Room.Id != -1)
                {
                    availableServices.Add(service);
                    continue;
                }
                // add service if the filter is set to "DIFFERENT_ROOM_ID" and this service has a different room id not equal to -1
                else if (filter.HasFlag(INetworkService.NetworkServiceFilter.DIFFERENT_ROOM_ID) && service.RoomId != _sessionManager.Room.Id && _sessionManager.Room.Id != -1 && service.RoomId != -1)
                {
                    availableServices.Add(service);
                    continue;
                }
            }

            return availableServices;
        }
    }
}