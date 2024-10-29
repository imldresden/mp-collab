using IMLD.MixedReality.Core;
using IMLD.MixedReality.Network;
using NAudio.Wave;
using OpusDotNet;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace IMLD.MixedReality.Audio
{
    public class AudioTransmitterNAudio : MonoBehaviour
    {
        private INetworkServiceManager _networkServiceManager;
        private INetworkService _audioService;
        private ISessionManager _sessionManager;
        private OpusEncoder _encoder;
        private RingBuffer<byte> _buffer = new RingBuffer<byte>(48000*2);
        private int SAMPLE_LENGTH = sizeof(short);
        private int NUM_CHANNELS = 1;
        private WaveInEvent _waveIn;
        private System.Diagnostics.Stopwatch _watch;
        private int _deviceNumber = -1;
        private Guid _userId = Guid.Empty;

        [SerializeField]
        private string _microphone;

        // Start is called before the first frame update
        void Start()
        {
            _networkServiceManager = ServiceLocator.Instance.Get<INetworkServiceManager>();

            if (_networkServiceManager != null)
            {
                _audioService = _networkServiceManager.StartServer(NetworkServiceDescription.ServiceType.AUDIO);
            }

            _sessionManager = ServiceLocator.Instance.Get<ISessionManager>();
            if (_sessionManager != null)
            {
                _userId = _sessionManager.CurrentUserId;
            }

            // create encoder
            _encoder = new OpusEncoder(OpusDotNet.Application.VoIP, 48000, 1);

            // enumerate microphones, find correct one
            int waveInDevices = WaveIn.DeviceCount;
            for (int waveInDevice = 0; waveInDevice < waveInDevices; waveInDevice++)
            {
                WaveInCapabilities deviceInfo = WaveIn.GetCapabilities(waveInDevice);
                Debug.Log("Device " + waveInDevice + ": " + deviceInfo.ProductName + ", " + deviceInfo.SupportsWaveFormat(SupportedWaveFormat.WAVE_FORMAT_48M16));
                if (deviceInfo.ProductName.Contains(_microphone))
                {
                    _deviceNumber = waveInDevice;
                }
            }

            if (_deviceNumber == -1)
            {
                Debug.LogWarning("Microphone with name \"" + _microphone + "\" not found. Using first microphone.");
                _deviceNumber = 0;
            }
            
            // create wave-in event
            _waveIn = new WaveInEvent();
            _waveIn.DeviceNumber = _deviceNumber;
            _waveIn.BufferMilliseconds = 20;
            WaveFormat format = new WaveFormat(48000, SAMPLE_LENGTH * 8, NUM_CHANNELS);
            _waveIn.WaveFormat = format;
            _waveIn.DataAvailable += OnDataAvailable;
            _waveIn.StartRecording();

            Debug.Log("Using microphone " + WaveIn.GetCapabilities(_deviceNumber).ProductName + ", sample rate: " + _waveIn.WaveFormat.SampleRate + ", channels: " + _waveIn.WaveFormat.Channels + ", bits per sample: " + _waveIn.WaveFormat.BitsPerSample);

        }

        private void OnDataAvailable(object sender, WaveInEventArgs e)
        {
            //_watch.Stop();
            //Debug.Log("Timer: " + _watch.ElapsedMilliseconds + ", Samples: " + e.BytesRecorded);
            //_watch.Restart();
            //Debug.Log("audio: " + e.BytesRecorded);

            try
            {
                if (e.BytesRecorded > 0) // lol
                {
                    // store in buffer
                    byte[] PcmBytes = new byte[e.BytesRecorded];
                    Buffer.BlockCopy(e.Buffer, 0, PcmBytes, 0, e.BytesRecorded);
                    _buffer.Write(PcmBytes);
                }
            }
            catch (Exception ex)
            {
                Debug.LogError("Error writing audio to buffer: " + ex.Message);
            }

            try
            {
                // read as many PCM frames as possible, encode to opus and send them over network
                while (_buffer.Count >= 960 * SAMPLE_LENGTH * NUM_CHANNELS)
                {
                    // read a frame, exit loop if this fails
                    if (!_buffer.TryRead(960 * SAMPLE_LENGTH * NUM_CHANNELS, out byte[] data))
                    {
                        break;
                    }

                    // encode the frame
                    byte[] opusData = new byte[90 * SAMPLE_LENGTH * NUM_CHANNELS];
                    int lengthWritten = _encoder.Encode(data, 960 * SAMPLE_LENGTH * NUM_CHANNELS, opusData, opusData.Length);

                    // get user id
                    if (_sessionManager != null)
                    {
                        _userId = _sessionManager.CurrentUserId;
                    }

                    // send data, length of the output may be of different size than expected
                    if (lengthWritten != opusData.Length)
                    {
                        byte[] trimmedData = new byte[lengthWritten];
                        Buffer.BlockCopy(opusData, 0, trimmedData, 0, lengthWritten);
                        _audioService.SendMessage(new MessageAudioData(_userId, NUM_CHANNELS, trimmedData));
                    }
                    else
                    {
                        _audioService.SendMessage(new MessageAudioData(_userId, NUM_CHANNELS, opusData));
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError("Error sending buffered audio over network: " + ex.Message);
            }
        }

        private void OnDestroy()
        {
            if(_waveIn != null)
            {
                _waveIn.StopRecording();
                _waveIn.Dispose();
                _waveIn = null;
            }

            if (_audioService != null)
            {
                _audioService.Destroy();
                _audioService = null;
            }
        }
    }
}