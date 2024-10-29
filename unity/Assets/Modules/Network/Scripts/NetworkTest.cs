using IMLD.MixedReality.Core;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using UnityEngine;

namespace IMLD.MixedReality.Network
{
    public class NetworkTest : MonoBehaviour
    {
        private INetworkServiceManager _networkServiceManager;
        private INetworkService _testService;
        private long _byteCounter = 0L;
        private DateTime _timer;
        private bool _isRunning = false;
        private bool _isReadyForMessage = true;
        private float _elapsedTime = 0;

        [SerializeField] private bool _isSender = false;
        [SerializeField] private int _dataRate;

        // Start is called before the first frame update
        void Start()
        {
            _networkServiceManager = ServiceLocator.Instance.Get<INetworkServiceManager>();

            if (_networkServiceManager != null)
            {
                if (_isSender)
                {
                    _testService = _networkServiceManager.StartServer(NetworkServiceDescription.ServiceType.TEST);
                    _testService.ClientConnected += OnClientConnected;
                    _testService.ClientDisconnected += OnClientDisconnected;
                    _testService.RegisterMessageHandler(MessageContainer.MessageType.AUDIO_DATA, OnDataReceivedServer);
                }
                else
                {
                    _networkServiceManager.AvailableServicesChanged += OnAvailableServicesChanged;
                    CheckAvailableServices();
                }
            }
        }

        private void OnClientDisconnected(object sender, ConnectionInfo client)
        {
            _isRunning = false;
        }

        private void OnClientConnected(object sender, ConnectionInfo client)
        {
            _isRunning = true;
            _timer = DateTime.Now;
            byte[] data = new byte[_dataRate];
            _testService.SendMessage(new MessageAudioData(Guid.Empty, 1, data));
            _byteCounter += data.Length;
        }

        private void OnAvailableServicesChanged(object sender, EventArgs e)
        {
            CheckAvailableServices();
        }

        private void CheckAvailableServices()
        {
            if (_networkServiceManager != null)
            {
                // iterate available services, search for new audio services
                foreach (var service in _networkServiceManager.GetAvailableServices(filter: INetworkService.NetworkServiceFilter.ALL))
                {
                    if (service.Type == NetworkServiceDescription.ServiceType.TEST)
                    {
                        // subscribe to new test data service
                        INetworkService testService;
                        if (_networkServiceManager.TryConnectToService(service, out testService))
                        {
                            _testService = testService;

                            // register message handler
                            _testService.RegisterMessageHandler(MessageContainer.MessageType.AUDIO_DATA, OnDataReceivedClient);
                            _timer = DateTime.Now;
                            _isRunning = true;
                        }
                    }
                }
            }
        }

        private Task OnDataReceivedClient(MessageContainer container)
        {
            _byteCounter += container.Payload.Length;
            _testService.SendMessage(new MessageAudioData(Guid.Empty, 1, new byte[1]));
            return Task.CompletedTask;
        }

        private Task OnDataReceivedServer(MessageContainer container)
        {
            byte[] data = new byte[_dataRate];
            _testService.SendMessage(new MessageAudioData(Guid.Empty, 1, data));
            _byteCounter += data.Length;
            return Task.CompletedTask;
        }

        // Update is called once per frame
        void Update()
        {
            if (_isRunning)
            {
                //if (_isSender && _isReadyForMessage)
                //{
                //    //int messageSize = (int)(_dataRate * Time.deltaTime);
                //    int messageSize = _dataRate;
                //    byte[] data = new byte[messageSize];
                //    _testService.SendMessage(new MessageAudioData(Guid.Empty, 1, data));
                //    _byteCounter += data.Length;
                //    _isReadyForMessage = false;
                //}

                _elapsedTime += Time.deltaTime;
                if (_elapsedTime >= 10.0f)
                {
                    _elapsedTime -= 10.0f;
                    var t = DateTime.Now - _timer;
                    Debug.Log((int)((_byteCounter / t.TotalSeconds) / 1000) + "kB/s");
                    _timer = DateTime.Now;
                    _byteCounter = 0L;
                }
            }
        }
    }
}

