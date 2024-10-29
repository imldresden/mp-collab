//using IMLD.MixedReality.Core;
//using IMLD.MixedReality.Network;
//using System.Collections;
//using System.Collections.Generic;
//using UnityEngine;
//using Microsoft.MixedReality.Toolkit.Audio;
//using System;
//using OpusDotNet;

//namespace IMLD.MixedReality.Audio
//{
//    public class AudioTransmitter : MonoBehaviour
//    {
//        private INetworkServiceManager _networkServiceManager;
//        private AudioClip Clip;
//        private int lastIndex = 0;

//        private INetworkService _audioService;
//        private float _timer;
//        private OpusEncoder _encoder;
//        private RingBuffer<byte> _buffer = new RingBuffer<byte>(10000);

//        // Start is called before the first frame update
//        void Start()
//        {
//            _networkServiceManager = ServiceLocator.Instance.Get<INetworkServiceManager>();

//            if (_networkServiceManager != null)
//            {
//                _audioService = _networkServiceManager.StartServer(NetworkServiceDescription.ServiceType.AUDIO);
//            }

//            Record();
//        }

//        private void FixedUpdate()
//        {
//            if (Clip == null)
//            {
//                return;
//            }

//            int SAMPLE_LENGTH = sizeof(short);  // length of a PCM sample is 16 bit
//            int NUM_CHANNELS = Clip.channels;   // number of channels, typically 1

//            if (Clip != null && Clip.samples > 0)
//            {  
//                // get current position
//                int index = Microphone.GetPosition("Mikrofon (HD Webcam C525)");

//                // compute number of new samples
//                int numSamples;
//                if (index >= lastIndex)
//                {
//                    numSamples = index - lastIndex;
//                }
//                else // we wrapped around
//                {
//                    // number of samples from last index to the end of the buffer, plus the new samples in the beginning
//                    numSamples = Clip.samples - lastIndex + index;
//                }

//                // get samples from clip
//                if (numSamples > 0) // we won't always get new samples
//                {
//                    float[] data = new float[numSamples * NUM_CHANNELS];
//                    Clip.GetData(data, lastIndex);
//                    lastIndex = index;

//                    // convert from 32bit float to 16 bit PCM
//                    short[] PcmData = new short[data.Length];
//                    for (int i = 0; i < data.Length; i++)
//                    {
//                        PcmData[i] = (short)(data[i] * 32767);
//                    }

//                    // store in buffer
//                    byte[] PcmBytes = new byte[PcmData.Length * SAMPLE_LENGTH];
//                    Buffer.BlockCopy(PcmData, 0, PcmBytes, 0, PcmBytes.Length);
//                    _buffer.Write(PcmBytes);
//                }
//            }

//            // read as many PCM frames as possible, encode to opus and send them over network
//            while (_buffer.Count >= 960 * SAMPLE_LENGTH * NUM_CHANNELS)
//            {
//                // read a frame, exit loop if this fails
//                if (!_buffer.TryRead(960 * SAMPLE_LENGTH * NUM_CHANNELS, out byte[] data))
//                {
//                    break;
//                }

//                // encode the frame
//                byte[] opusData = new byte[90 * SAMPLE_LENGTH * NUM_CHANNELS];
//                int lengthWritten = _encoder.Encode(data, 960 * SAMPLE_LENGTH * NUM_CHANNELS, opusData, opusData.Length);

//                // send data, length of the output may be of different size than expected
//                if (lengthWritten != opusData.Length)
//                {
//                    byte[] trimmedData = new byte[lengthWritten];
//                    Buffer.BlockCopy(opusData, 0, trimmedData, 0, lengthWritten);
//                    _audioService.SendMessage(new MessageAudioData(NUM_CHANNELS, trimmedData));
//                }
//                else
//                {
//                    _audioService.SendMessage(new MessageAudioData(NUM_CHANNELS, opusData));
//                }
//            }
//        }

//        //IEnumerator RecordData()
//        //{
//        //    float[] sampleBuffer = new float[FrameLength];
//        //    int startReadPos = 0;

//        //    while (true)
//        //    {
//        //        int curClipPos = Microphone.GetPosition("Mikrofon (HD Webcam C525)");
//        //        if (curClipPos < startReadPos)
//        //            curClipPos += Clip.samples;

//        //        int samplesAvailable = curClipPos - startReadPos;
//        //        if (samplesAvailable < FrameLength)
//        //        {
//        //            yield return null;
//        //            continue;
//        //        }

//        //        int endReadPos = startReadPos + FrameLength;
//        //        if (endReadPos > Clip.samples)
//        //        {
//        //            // fragmented read (wraps around to beginning of clip)
//        //            // read bit at end of clip
//        //            int numSamplesClipEnd = Clip.samples - startReadPos;
//        //            float[] endClipSamples = new float[numSamplesClipEnd];
//        //            Clip.GetData(endClipSamples, startReadPos);

//        //            // read bit at start of clip
//        //            int numSamplesClipStart = endReadPos - Clip.samples;
//        //            float[] startClipSamples = new float[numSamplesClipStart];
//        //            Clip.GetData(startClipSamples, 0);

//        //            // combine to form full frame
//        //            Buffer.BlockCopy(endClipSamples, 0, sampleBuffer, 0, numSamplesClipEnd);
//        //            Buffer.BlockCopy(startClipSamples, 0, sampleBuffer, numSamplesClipEnd, numSamplesClipStart);
//        //        }
//        //        else
//        //        {
//        //            Clip.GetData(sampleBuffer, startReadPos);
//        //        }

//        //        startReadPos = endReadPos % Clip.samples;

//        //        byte[] byteData = new byte[sampleBuffer.Length * 4];
//        //        Buffer.BlockCopy(sampleBuffer, 0, byteData, 0, byteData.Length);
//        //        _audioService.SendMessage(new MessageAudioData(byteData));

//        //        yield return new WaitForSeconds(1 / 50f);
//        //    }

//        //}

//        //// Update is called once per frame
//        //void Update()
//        //{
//        //    if (Clip != null && Clip.samples > 0)
//        //    {
//        //        // get current position
//        //        int index = Microphone.GetPosition("Mikrofon (HD Webcam C525)");

//        //        // compute number of new samples
//        //        int numSamples;
//        //        if (index >= lastIndex)
//        //        {
//        //            numSamples = index - lastIndex;
//        //        }
//        //        else // we wrapped around
//        //        {
//        //            // number of samples from last index to the end of the buffer, plus the new samples in the beginning
//        //            numSamples = Clip.samples - lastIndex + index;
//        //        }

//        //        // get samples from clip
//        //        if (numSamples > 0) // we won't always get new samples
//        //        {
//        //            float[] data = new float[numSamples * Clip.channels];
//        //            Clip.GetData(data, lastIndex);
//        //            lastIndex = index;
//        //            byte[] byteData = new byte[data.Length * 4];
//        //            Buffer.BlockCopy(data, 0, byteData, 0, byteData.Length);
//        //            _audioService.SendMessage(new MessageAudioData(byteData));
//        //        }
//        //    }
//        //    ////if (MicStream == null) { return; }

//        //    ////// Read the microphone stream data.
//        //    ////float[] buffer = new float[500];
//        //    ////WindowsMicrophoneStreamErrorCode result = MicStream.ReadAudioFrame(buffer, 2);
//        //    ////if (result != WindowsMicrophoneStreamErrorCode.Success)
//        //    ////{
//        //    ////    Debug.Log($"Failed to read the microphone stream data. {result}");
//        //    ////}

//        //    ////SendData(buffer);

//        //    //// get current position
//        //    //int index = Microphone.GetPosition("Mikrofon (HD Webcam C525)");

//        //    //// compute number of new samples
//        //    //int numSamples;
//        //    //if (index >= lastIndex)
//        //    //{
//        //    //    numSamples = index - lastIndex;
//        //    //}
//        //    //else // we wrapped around
//        //    //{
//        //    //    // number of samples from last index to the end of the buffer, plus the new samples in the beginning
//        //    //    numSamples = Clip.samples - lastIndex + index;
//        //    //}

//        //    //// get samples from clip
//        //    //if (numSamples > 0) // we won't always get new samples
//        //    //{
//        //    //    float[] Data = new float[numSamples];
//        //    //    Clip.GetData(Data, lastIndex);
//        //    //    lastIndex = index;
//        //    //    SendData(Data);
//        //    //}

//        //}

//        //private void OnAudioFilterRead(float[] buffer, int numChannels)
//        //{
//        //    //if (MicStream == null) { return; }

//        //    //// Read the microphone stream data.
//        //    //WindowsMicrophoneStreamErrorCode result = MicStream.ReadAudioFrame(buffer, numChannels);
//        //    //if (result != WindowsMicrophoneStreamErrorCode.Success)
//        //    //{
//        //    //    Debug.Log($"Failed to read the microphone stream data. {result}");
//        //    //}
//        //    //Debug.Log("rec: " + buffer.Length + ", content: " + buffer[100]);
//        //    SendData(buffer);
//        //}

//        //void SendData(float[] data)
//        //{
//        //    if (_networkServiceManager != null)
//        //    {
//        //        //NetworkManager.SendMessage(new MessageAudioData(data));
//        //        //AudioReceiver.OnAudioDataReceived(new MessageAudioData(data).Pack());
//        //    }
//        //}

//        private void Awake()
//        {
//            var audioConfig = AudioSettings.GetConfiguration();
//            audioConfig.sampleRate = 48000;
//            audioConfig.dspBufferSize = 1024;
//            AudioSettings.outputSampleRate = 48000;
//            AudioSettings.Reset(audioConfig);
//        }

//        void Record()
//        {
//            // create encoder
//            _encoder = new OpusEncoder(OpusDotNet.Application.VoIP, 48000, 1);

//            Debug.Log("Audio sample rate: " + AudioSettings.GetConfiguration().sampleRate);
//            foreach (var device in Microphone.devices)
//            {
//                Microphone.GetDeviceCaps(device, out int minFreq, out int maxFreq);
//                Debug.Log(device +" " + minFreq + ", " + maxFreq);
//            }

//            Clip = Microphone.Start("Mikrofon (HD Webcam C525)", true, 1, 48000);
//            while (!(Microphone.GetPosition("Mikrofon (HD Webcam C525)") > 0)) { }
//            Debug.Log("Microphone Input: " + Clip.channels + " channels, " + Clip.frequency + "Hz.");

//            //StartCoroutine(TransmitData());
//        }

//        //IEnumerator TransmitData()
//        //{
//        //    while (true)
//        //    {
//        //        if (Clip != null && Clip.samples > 0)
//        //        {
//        //            // get current position
//        //            int index = Microphone.GetPosition("Mikrofon (HD Webcam C525)");

//        //            // compute number of new samples
//        //            int numSamples;
//        //            if (index >= lastIndex)
//        //            {
//        //                numSamples = index - lastIndex;
//        //            }
//        //            else // we wrapped around
//        //            {
//        //                // number of samples from last index to the end of the buffer, plus the new samples in the beginning
//        //                numSamples = Clip.samples - lastIndex + index;
//        //            }

//        //            // get samples from clip
//        //            if (numSamples > 0) // we won't always get new samples
//        //            {
//        //                float[] data = new float[numSamples * Clip.channels];
//        //                Clip.GetData(data, lastIndex);
//        //                lastIndex = index;
//        //                byte[] byteData = new byte[data.Length * 4];
//        //                Buffer.BlockCopy(data, 0, byteData, 0, byteData.Length);
//        //                _audioService.SendMessage(new MessageAudioData(byteData));
//        //            } 
//        //        }

//        //        yield return new WaitForSeconds(1 / 30f);
//        //    }
//        //}
//    }
//}