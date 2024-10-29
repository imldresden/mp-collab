using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using IMLD.MixedReality.Core;
using IMLD.MixedReality.Network;
using System;
using OpusDotNet;

namespace IMLD.MixedReality.Audio
{
    public class AudioPlayer : MonoBehaviour
    {
        [SerializeField]
        private AudioSource AudioSource;
        private RingBuffer<float> _ringBuffer = new RingBuffer<float>(960 * 6); // the ring buffer for the audio data
        private int SAMPLE_LENGTH = sizeof(short);  // length of a PCM sample is 16 bit
        private int NUM_CHANNELS = 1;   // number of channels, typically 1
        private OpusDecoder _decoder;

        public void DebugReceiveAudioData(byte[] data, int channels)
        {
            // setup audio if the number of audio channels in our data changed
            if (channels != NUM_CHANNELS)
            {
                NUM_CHANNELS = channels;
                Debug.LogWarning("Audio channels changed!");
                SetupAudio();
            }

            // decode data
            byte[] ByteData = new byte[960 * SAMPLE_LENGTH * NUM_CHANNELS]; // we always transmit 960 samples, so this *should* work
            int lengthWritten = _decoder.Decode(data, data.Length, ByteData, ByteData.Length);
            if (lengthWritten != ByteData.Length)
            {
                Debug.LogError("Length written was expected to be " + ByteData.Length + " but was " + lengthWritten);
                return;
            }

            // convert from byte[] to short[]
            short[] PcmData = new short[960 * NUM_CHANNELS];
            Buffer.BlockCopy(ByteData, 0, PcmData, 0, ByteData.Length);

            // convert from 16 bit PCM to 32bit float
            float[] FloatData = new float[960 * NUM_CHANNELS];
            short minPcm = short.MaxValue;
            short maxPcm = short.MinValue;
            float minFloat = float.MaxValue;
            float maxFloat = float.MinValue;
            for (int i = 0; i < FloatData.Length; i++)
            {
                minPcm = Math.Min(minPcm, PcmData[i]);
                maxPcm = Math.Max(maxPcm, PcmData[i]);
                FloatData[i] = PcmData[i] / 32768f;
                minFloat = Math.Min(minFloat, FloatData[i]);
                maxFloat = Math.Max(maxFloat, FloatData[i]);
            }

            if (minFloat < -1.0f || maxFloat > 1.0f)
            {
                Debug.LogWarning("AudioClipping! min/max PCM: " + minPcm + ", " + maxPcm + "; min/max Float: " + minFloat + ", " + maxFloat);
            }


            // write data to buffer
            _ringBuffer.Write(FloatData);
        }


        public void OnAudioDataReceived(MessageAudioData message)
        {

            // setup audio if the number of audio channels in our data changed
            if (message.Channels != NUM_CHANNELS)
            {
                NUM_CHANNELS = message.Channels;
                Debug.LogWarning("Audio channels changed!");
                SetupAudio();
            }

            // decode data
            byte[] ByteData = new byte[960 * SAMPLE_LENGTH * NUM_CHANNELS]; // we always transmit 960 samples, so this *should* work
            int lengthWritten = _decoder.Decode(message.ByteData, message.ByteData.Length, ByteData, ByteData.Length);
            if (lengthWritten != ByteData.Length)
            {
                Debug.LogError("Length written was expected to be " + ByteData.Length + " but was " + lengthWritten);
                return;
            }

            // convert from byte[] to short[]
            short[] PcmData = new short[960 * NUM_CHANNELS];
            Buffer.BlockCopy(ByteData, 0, PcmData, 0, ByteData.Length);

            // convert from 16 bit PCM to 32bit float
            float[] FloatData = new float[960 * NUM_CHANNELS];
            int minPcm = short.MaxValue;
            int maxPcm = short.MinValue;
            for (int i = 0; i < FloatData.Length; i++)
            {
                minPcm = Math.Min(minPcm, PcmData[i]);
                maxPcm = Math.Max(maxPcm, PcmData[i]);
                FloatData[i] = PcmData[i] / 32767f;
            }

            // write data to buffer
            _ringBuffer.Write(FloatData);
            //Debug.Log("audio buffer (w): " + _ringBuffer.Count);
        }

        // Start is called before the first frame update
        void Start()
        {
            SetupAudio();
        }

        public void setAudioSource(AudioSource audioSource)
        {
            AudioSource = audioSource;
            SetupAudio();
        }

        private void SetupAudio()
        {
            if (AudioSource != null)
            {
                AudioSource.spatialBlend = 1;
                AudioSource.loop = true;
                AudioSource.Play();
            }

            if (_decoder != null)
            {
                _decoder.Dispose();
            }

            _decoder = new OpusDecoder(48000, NUM_CHANNELS);
        }

        private void OnAudioFilterRead(float[] buffer, int numChannels)
        {
            // fill the requested buffer with data from the ring buffer
            if (numChannels == NUM_CHANNELS)
            {
                _ringBuffer.TryRead(buffer.Length, out buffer);
                //Debug.Log("audio buffer (r): " + _ringBuffer.Count);
                if (buffer == null)
                {
                    Debug.LogError("Audio buffer underflow.");
                }

                return;
            }

            if (numChannels == 2 && NUM_CHANNELS == 1)
            {
                float[] AudioBuffer;

                _ringBuffer.TryRead(buffer.Length / 2, out AudioBuffer);
                //Debug.Log("audio buffer (r): " + _ringBuffer.Count);
                if (AudioBuffer != null)
                {
                    for (int i = 0; i < buffer.Length / 2; i++)
                    {
                        buffer[2 * i] = AudioBuffer[i];
                        buffer[2 * i + 1] = AudioBuffer[i];
                    }
                }
                //else
                //{
                //    Debug.LogError("Audio buffer underflow.");
                //    //for (int i = 0; i < buffer.Length / 2; i++)
                //    //{
                //    //    buffer[2 * i] = 0;
                //    //    buffer[2 * i + 1] = 0;
                //    //}
                //}

                return;
            }

            Debug.LogError("Channel mismatch: " + NUM_CHANNELS + ", " + numChannels);


            //FillBuffer(ref buffer, numChannels);
        }
    }
}