using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace IMLD.MixedReality.Network
{
    public class ReplayNetworkServiceManager : NetworkServiceManager
    {
        [SerializeField]
        private string _replayPath;

        private string[] _filePaths;

        private Dictionary<NetworkServiceDescription, INetworkService> _networkServices = new Dictionary<NetworkServiceDescription, INetworkService>();
        private long _minFilestamp, _maxFilestamp;

        private void Awake()
        {
            // read list of network dump files from config or from available dump files in directory
            if (System.IO.File.Exists(_replayPath))
            {
                try
                {
                    using (StreamReader sr = new StreamReader(_replayPath))
                    {
                        string line;
                        List<string> files = new List<string>();
                        while ((line = sr.ReadLine()) != null)
                        {
                            // read all lines from config, add all non-comment lines ending on ".bin"
                            if (!line.StartsWith("//") && line.EndsWith(".bin"))
                            {
                                files.Add(line);
                            }
                        }

                        _filePaths = files.ToArray();
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine("The file could not be read:");
                    Console.WriteLine(e.Message);
                }
            }
            else if (System.IO.Directory.Exists(_replayPath))
            {
                // alternatively, add all .bin files from the directory
                _filePaths = System.IO.Directory.GetFiles(_replayPath, "*.bin");
            }            
        }

        private INetworkService StartServerFromFile(string filePath, string data = "")
        {
            // create new game object & attach network service component
            var go = new GameObject();
            go.transform.SetParent(transform);
            var networkService = go.AddComponent<ReplayNetworkService>();

            // configure and start replay network service
            networkService.SetFile(filePath);
            networkService.StartAsServer();

            // rename game object
            go.name = "[Server " + networkService.ServiceDescription.HostName + ": " + Enum.GetName(typeof(NetworkServiceDescription.ServiceType), networkService.ServiceDescription.Type) + "]";

            // add network service to collection of provided services
            ProvidedServices.Add(networkService.ServiceType, networkService);

            // return new network service
            return networkService;
        }

        private ReplayNetworkService LoadServiceFromFile(string filePath, string data = "")
        {
            // create new game object & attach network service component
            var go = new GameObject();
            go.transform.SetParent(transform);
            var networkService = go.AddComponent<ReplayNetworkService>();

            // configure and start replay network service
            networkService.SetFile(filePath);

            // rename game object
            go.name = "[Client " + networkService.ServiceDescription.HostName + ": " + Enum.GetName(typeof(NetworkServiceDescription.ServiceType), networkService.ServiceDescription.Type) + "]";

            // add network service to collection of available services
            AvailableServices[networkService.ServiceDescription.ServiceId] = networkService.ServiceDescription;

            // return new network service
            return networkService;
        }

        public override void StartDiscovery()
        {
            // start replay services from list of available dump files
            foreach (var file in _filePaths)
            {
                var service = LoadServiceFromFile(file);
                _networkServices[service.ServiceDescription] = service;
            }

            OnAvailableServicesChanged();
        }

        public override void StopDiscovery() { }

        public override List<NetworkServiceDescription> GetAvailableServices(INetworkService.NetworkServiceFilter filter = INetworkService.NetworkServiceFilter.DEFAULT)
        {
            List<NetworkServiceDescription> availableServices = new List<NetworkServiceDescription>();
            foreach (NetworkServiceDescription service in AvailableServices.Values)
            {
                // don't connect to yourself
                if (ProvidedServices.TryGetValue(service.Type, out var providedService))
                {
                    if (providedService.ServiceDescription.ServiceId == service.ServiceId)
                    {
                        continue;
                    }
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

        public override bool TryConnectToService(NetworkServiceDescription serviceDescription, out INetworkService service, bool useMessageQueue = true)
        {
            Debug.Log("Trying to connect to replay service of type " + Enum.GetName(typeof(NetworkServiceDescription.ServiceType), serviceDescription.Type) + "...");

            // check connected services to prevent connecting twice
            foreach (var connectedService in RequestedServices)
            {
                if (connectedService.ServiceDescription == serviceDescription)
                {
                    Debug.LogWarning("Cannot connect to service" + serviceDescription.HostName + ": " + Enum.GetName(typeof(NetworkServiceDescription.ServiceType), serviceDescription.Type) + "Already connected to service!");
                    service = null;
                    return false;
                }
            }

            // get service
            if (_networkServices.TryGetValue(serviceDescription, out var networkService))
            {
                // configure and start network service
                networkService.StartAsClient(serviceDescription, useMessageQueue);

                // add network service to collection of connected services
                RequestedServices.Add(networkService);

                // return new network service
                service = networkService;

                Debug.Log("connected.");
                return true;
            }
            else
            {
                service = null;
                Debug.Log("failed.");
                return false;
            }            
        }
    }
}