using IMLD.MixedReality.Core;
using IMLD.MixedReality.Network;
using OpusDotNet;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

namespace IMLD.MixedReality.Audio
{
    public class AudioReceiver : MonoBehaviour, IAudioReceiver
    {       
        private ISessionManager _sessionManager;
        private INetworkServiceManager _networkServiceManager;
        private List<INetworkService> _audioServices = new List<INetworkService>();
        
        //private System.Diagnostics.Stopwatch _watch = new System.Diagnostics.Stopwatch();

        private void Awake()
        {
            // set audio configuration early and at one central place
            var audioConfig = AudioSettings.GetConfiguration();
            audioConfig.sampleRate = 48000;
            audioConfig.dspBufferSize = 512;
            AudioSettings.outputSampleRate = 48000;
            AudioSettings.Reset(audioConfig);
        }

        // Start is called before the first frame update
        void Start()
        {
            // get network service manager, check for services
            _networkServiceManager = ServiceLocator.Instance.Get<INetworkServiceManager>();

            //if (_networkServiceManager != null)
            //{
            //    _networkServiceManager.AvailableServicesChanged += OnAvailableServicesChanged;
            //    CheckAvailableServices();
            //}

            _sessionManager = ServiceLocator.Instance.Get<ISessionManager>();
        }

        

        //private void OnAvailableServicesChanged(object sender, EventArgs e)
        //{
        //    CheckAvailableServices();
        //}

        //private void CheckAvailableServices()
        //{
        //    if (_networkServiceManager != null)
        //    {
        //        // iterate available services, search for new audio services
        //        foreach (var service in _networkServiceManager.GetAvailableServices(filter: INetworkService.NetworkServiceFilter.DIFFERENT_ROOM_ID))
        //        {
        //            if (service.Type == NetworkServiceDescription.ServiceType.AUDIO)
        //            {
        //                // subscribe to new audio data service
        //                INetworkService audioService;
        //                if (_networkServiceManager.TryConnectToService(service, out audioService))
        //                {
        //                    _audioServices.Add(audioService);

        //                    // register message handler
        //                    audioService.RegisterMessageHandler(MessageContainer.MessageType.AUDIO_DATA, OnAudioDataReceived);
        //                }                        
        //            }
        //        }
        //    }
        //}

        public void ConnectToAudioService(NetworkServiceDescription service)
        {
            if (service.Type == NetworkServiceDescription.ServiceType.AUDIO)
            {
                // subscribe to new audio data service
                INetworkService audioService;
                if (_networkServiceManager.TryConnectToService(service, out audioService))
                {
                    _audioServices.Add(audioService);

                    // register message handler
                    audioService.RegisterMessageHandler(MessageContainer.MessageType.AUDIO_DATA, OnAudioDataReceived);
                }
            }
        }

        public Task OnAudioDataReceived(MessageContainer container)
        {
            var Message = MessageAudioData.Unpack(container);

            if (Message == null)
            {
                Debug.LogError("Error parsing audio message.");
                return Task.CompletedTask;
            }

            if (_sessionManager != null)
            {
                var user = _sessionManager.GetUser(Message.UserId);
                if (user != null)
                {
                    user.AudioPlayer.OnAudioDataReceived(Message);
                }
            }
            else
            {
                Debug.LogError("Session Manager is null.");
            }

            return Task.CompletedTask;
        }

        private void OnDestroy()
        {
            if (_audioServices != null)
            {
                foreach (var audioService in _audioServices)
                {
                    audioService.Destroy();
                }

                _audioServices.Clear();
            }
        }
    }
}