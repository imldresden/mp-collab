//using System;
//using System.Collections;
//using System.Collections.Generic;
//using System.Runtime.InteropServices;
//using System.Threading.Tasks;
//using UnityEngine;
//using IMLD.MixedReality.Network;
//using IMLD.MixedReality.Core;

//#if ENABLE_WINMD_SUPPORT
//using Windows.Media.Audio;
//using Windows.Media.Effects;
//using Windows.Media.Render;
//using Windows.Foundation;
//using Windows.Media;
//using Windows.Media.MediaProperties;
//using Windows.Devices.Enumeration;
//using Windows.Media.Devices;
//#endif

//namespace IMLD.MixedReality.Audio
//{
//    public class AudioReceiverUWP : MonoBehaviour
//    {
//        [SerializeField]
//        private bool MonitorInput;

//        private INetworkServiceManager NetworkManager;

//#if ENABLE_WINMD_SUPPORT
//        private AudioGraph graph;
//        private AudioDeviceOutputNode deviceOutputNode;
//        private AudioFrameInputNode frameInputNode;
//#endif

//        private bool isGraphReady = false;
//        private bool IsStarted = false;
//        //private List<byte[]> _bufferList = new List<byte[]>();
//        private RingBuffer<byte> _buffer = new RingBuffer<byte>(44100 * 4);


//        // Start is called before the first frame update
//        async void Start()
//        {
//#if ENABLE_WINMD_SUPPORT
//            NetworkManager = ServiceLocator.Instance.Get<INetworkServiceManager>();
//            NetworkManager.RegisterMessageHandler(MessageContainer.MessageType.AUDIO_DATA, OnAudioDataReceived);
//            Debug.Log("Creating Audio Graph...");
//            await CreateAudioGraph();
//#endif
//        }

//        // Update is called once per frame
//        void Update()
//        {
//#if ENABLE_WINMD_SUPPORT
//            if (isGraphReady == true && IsStarted == false)
//            {
//                IsStarted = true;
//                Debug.Log("Starting Audio Graph...");
//                graph.Start();
//            }
//#endif
//        }

//#if ENABLE_WINMD_SUPPORT
//        private async Task CreateAudioGraph()
//        {
//            // Enumerate input devices
//            var inputDevices = await DeviceInformation.FindAllAsync(MediaDevice.GetAudioCaptureSelector());
//            string devices = "Audio Input Devices:\n";
//            foreach (var device in inputDevices)
//            {
//                devices += device.Name + "\n";
//            }
//            Debug.LogError(devices);

//            // Create an AudioGraph with default settings
//            AudioEncodingProperties audioEncodingProperties = new AudioEncodingProperties();
//            audioEncodingProperties.BitsPerSample = AudioTransceiverConsts.SAMPLE_BITS;
//            audioEncodingProperties.ChannelCount = AudioTransceiverConsts.CHANNELS;
//            audioEncodingProperties.SampleRate = AudioTransceiverConsts.SAMPLING_RATE;
//            audioEncodingProperties.Subtype = MediaEncodingSubtypes.Float;

//            AudioGraphSettings settings = new AudioGraphSettings(AudioRenderCategory.Communications);
//            settings.QuantumSizeSelectionMode = QuantumSizeSelectionMode.LowestLatency;

//            CreateAudioGraphResult result = await AudioGraph.CreateAsync(settings);

//            if (result.Status != AudioGraphCreationStatus.Success)
//            {
//                // Cannot create graph
//                Debug.LogError(String.Format("AudioGraph Creation Error because {0}", result.Status.ToString()));
//                return;
//            }

//            graph = result.Graph;
//            Debug.Log("AudioGraph successfully created");

//            // Creat Frame Input Node
//            frameInputNode = graph.CreateFrameInputNode(audioEncodingProperties);

//            // Create Device Output Node
//            CreateAudioDeviceOutputNodeResult deviceOutputResult = await graph.CreateDeviceOutputNodeAsync();

//            if (deviceOutputResult.Status != AudioDeviceNodeCreationStatus.Success)
//            {
//                // Cannot create device output
//                Debug.LogError(String.Format("Audio Device Output unavailable because {0}", deviceOutputResult.Status.ToString()));
//                return;
//            }

//            deviceOutputNode = deviceOutputResult.DeviceOutputNode;
//            Debug.Log("Device Output Node successfully created");

//            frameInputNode.AddOutgoingConnection(deviceOutputNode);
//            frameInputNode.QuantumStarted += FrameInputNode_QuantumStarted;
//            frameInputNode.Start();

//            isGraphReady = true;
//        }

//        private void FrameInputNode_QuantumStarted(AudioFrameInputNode sender, FrameInputNodeQuantumStartedEventArgs args)
//        {
//            int numSamples = args.RequiredSamples;
//            byte[] data;
//            bool result = _buffer.TryRead(numSamples * AudioTransceiverConsts.SAMPLE_BYTES, out data);

//            if (result)
//            {
//                var frame = GenerateAudioFrame(data);
//                if (frame != null && frameInputNode.QueuedSampleCount < 64)
//                {
//                    frameInputNode.AddFrame(frame);
//                }
//            }
//        }

//        [ComImport]
//        [Guid("5B0D3235-4DBA-4D44-865E-8F1D0E4FD04D")]
//        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
//        unsafe interface IMemoryBufferByteAccess
//        {
//            void GetBuffer(out byte* buffer, out uint capacity);
//        }

//        public Task OnAudioDataReceived(MessageContainer container)
//        {
//            if (container?.Type == MessageContainer.MessageType.AUDIO_DATA)
//            {
//                var byteData = MessageAudioData.Unpack(container).ByteData;
//                _buffer.Write(byteData);
//            }

//            return Task.CompletedTask;
//        }

//        unsafe private AudioFrame GenerateAudioFrame(byte[] byteData)
//        {
//            if (byteData == null || byteData.Length == 0)
//            {
//                return null;
//            }

//            uint bufferSize = (uint)byteData.Length;
//            AudioFrame frame = new Windows.Media.AudioFrame(bufferSize);

//            using (AudioBuffer buffer = frame.LockBuffer(AudioBufferAccessMode.Write))
//            {
//                using (IMemoryBufferReference reference = buffer.CreateReference())
//                {
//                    byte* dataInBytes;

//                    // Get the buffer from the AudioFrame
//                    ((IMemoryBufferByteAccess)reference).GetBuffer(out dataInBytes, out _);

//                    IntPtr pointer = (IntPtr)dataInBytes;
//                    Marshal.Copy(byteData, 0, pointer, byteData.Length);
//                }
//            }

//            return frame;
//        }

//#endif
//    }

//    public static class AudioTransceiverConsts
//    {
//        public const int SAMPLING_RATE = 16000;
//        public const int CHANNELS = 1;
//        public const int SAMPLE_BYTES = sizeof(float);
//        public const int SAMPLE_BITS = SAMPLE_BYTES * 8;
//    }
//}