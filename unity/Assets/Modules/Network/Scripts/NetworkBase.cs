using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using UnityEngine;

namespace IMLD.MixedReality.Network
{
    public class NetworkBase
    {
        public event MessageEventHandler MessageReceived;

        private const int MESSAGE_HEADER_LENGTH = MESSAGE_SIZE_LENGTH + MESSAGE_TYPE_LENGTH + MESSAGE_TIMESTAMP_LENGTH;
        private const int MESSAGE_SIZE_LENGTH = sizeof(int);
        private const int MESSAGE_TYPE_LENGTH = sizeof(byte);
        private const int MESSAGE_TIMESTAMP_LENGTH = sizeof(long);

        private readonly ConcurrentQueue<MessageContainer> _messageQueue = new ConcurrentQueue<MessageContainer>();
        private readonly Dictionary<IPEndPoint, EndPointState> _endPointStates = new Dictionary<IPEndPoint, EndPointState>();

        internal bool GetNextMessage(out MessageContainer message)
        {
            return _messageQueue.TryDequeue(out message);
        }

        internal void OnDataReceived(IPEndPoint remoteEndPoint, Memory<byte> data)
        {
            Span<byte> dataSpan = data.Span;
            int currentByte = 0;
            int dataLength = dataSpan.Length;
            EndPointState state;
            try
            {
                if (_endPointStates.ContainsKey(remoteEndPoint))
                {
                    state = _endPointStates[remoteEndPoint];
                }
                else
                {
                    state = new EndPointState();
                    _endPointStates[remoteEndPoint] = state;
                }

                state.Sender = remoteEndPoint;
                while (currentByte < dataLength)
                {
                    int messageSize;

                    // currently still reading a (large) message?
                    if (state.IsMessageIncomplete)
                    {
                        Debug.Log("resuming message");
                        // 1. get size of current message
                        messageSize = state.MessageBuffer.Length;

                        // 2. read data
                        // decide how much to read: not more than remaining message size, not more than remaining data size
                        int lengthToRead = Math.Min(messageSize - state.MessageBytesRead, dataLength - currentByte);

                        //Array.Copy(dataArray, currentByte, state.CurrentMessageBuffer, state.CurrentMessageBytesRead, lengthToRead); // copy data from data to message buffer
                        dataSpan.Slice(currentByte, lengthToRead).CopyTo(state.MessageBuffer.AsSpan(state.MessageBytesRead)); // copy data from data to message buffer

                        currentByte += lengthToRead; // increase "current byte pointer"
                        state.MessageBytesRead += lengthToRead; // increase amount of message bytes read

                        // 3. decide how to proceed
                        if (state.MessageBytesRead == messageSize)
                        {
                            Debug.Log("message complete " + Enum.GetName(typeof(MessageContainer.MessageType), state.MessageType));
                            // Message is completed
                            state.IsMessageIncomplete = false;
                            _messageQueue.Enqueue(MessageContainer.Deserialize(state.Sender, state.MessageBuffer, state.MessageType, state.MessageTimestamp));
                        }
                        else
                        {
                            Debug.Log("message incomplete, " + state.MessageBytesRead + " Bytes read");
                            // We did not read the whole message yet
                            state.IsMessageIncomplete = true;
                        }
                    }
                    else if (state.IsHeaderIncomplete)
                    {
                        Debug.Log("resuming header");
                        // currently still reading a header
                        // decide how much to read: not more than remaining message size, not more than remaining header size
                        int lengthToRead = Math.Min(MESSAGE_HEADER_LENGTH - state.HeaderBytesRead, dataLength - currentByte);

                        //Array.Copy(dataArray, currentByte, state.CurrentHeaderBuffer, state.CurrentHeaderBytesRead, lengthToRead); // read header data into header buffer
                        dataSpan.Slice(currentByte, lengthToRead).CopyTo(state.HeaderBuffer.AsSpan(state.HeaderBytesRead)); // read header data into header buffer

                        currentByte += lengthToRead;
                        state.HeaderBytesRead += lengthToRead;
                        if (state.HeaderBytesRead == MESSAGE_HEADER_LENGTH)
                        {
                            // Message header is completed
                            // read size of message from header buffer
                            messageSize = BitConverter.ToInt32(state.HeaderBuffer, 0);
                            state.MessageBuffer = new byte[messageSize];
                            state.MessageBytesRead = 0;
                            Debug.Log("message size: " + messageSize);

                            // read type of next message
                            state.MessageType = state.HeaderBuffer[MESSAGE_SIZE_LENGTH];

                            // read timestamp of message
                            state.MessageTimestamp = BitConverter.ToInt64(state.HeaderBuffer, MESSAGE_SIZE_LENGTH + MESSAGE_TYPE_LENGTH);

                            Debug.Log("header complete " + Enum.GetName(typeof(MessageContainer.MessageType), state.MessageType));

                            state.IsHeaderIncomplete = false;
                            state.IsMessageIncomplete = true;
                        }
                        else
                        {
                            Debug.Log("header incomplete");
                            // We did not read the whole header yet
                            state.IsHeaderIncomplete = true;
                        }
                    }
                    else
                    {
                        // start reading a new message
                        Debug.Log("new message");
                        // 1. check if remaining data sufficient to read message header
                        if (currentByte < dataLength - MESSAGE_HEADER_LENGTH)
                        {
                            // 2. read size of next message
                            messageSize = BitConverter.ToInt32(dataSpan.Slice(currentByte));
                            state.MessageBuffer = new byte[messageSize];
                            state.MessageBytesRead = 0;
                            currentByte += MESSAGE_SIZE_LENGTH;
                            Debug.Log("message size: " + messageSize);

                            // 3. read type of next message
                            state.MessageType = dataSpan[currentByte];
                            currentByte += MESSAGE_TYPE_LENGTH;

                            // 4. read timestamp of next message
                            state.MessageTimestamp = BitConverter.ToInt64(dataSpan.Slice(currentByte));
                            currentByte += MESSAGE_TIMESTAMP_LENGTH;

                            Debug.Log("header complete " + Enum.GetName(typeof(MessageContainer.MessageType), state.MessageType));

                            // 5. read data
                            // decide how much to read: not more than remaining message size, not more than remaining data size
                            int lengthToRead = Math.Min(messageSize - state.MessageBytesRead, dataLength - currentByte);

                            //Array.Copy(dataArray, currentByte, state.CurrentMessageBuffer, state.CurrentMessageBytesRead, lengthToRead); // copy data from data to message buffer
                            dataSpan.Slice(currentByte, lengthToRead).CopyTo(state.MessageBuffer.AsSpan(state.MessageBytesRead));

                            currentByte += lengthToRead; // increase "current byte pointer"
                            state.MessageBytesRead += lengthToRead; // increase amount of message bytes read

                            // 4. decide how to proceed
                            if (state.MessageBytesRead == messageSize)
                            {
                                Debug.Log("message complete, " + Enum.GetName(typeof(MessageContainer.MessageType), state.MessageType));
                                // Message is completed
                                state.IsMessageIncomplete = false;
                                _messageQueue.Enqueue(MessageContainer.Deserialize(state.Sender, state.MessageBuffer, state.MessageType, state.MessageTimestamp));
                            }
                            else
                            {
                                Debug.Log("message incomplete, " + state.MessageBytesRead + " Bytes read");
                                // We did not read the whole message yet
                                state.IsMessageIncomplete = true;
                            }
                        }
                        else
                        {
                            Debug.Log("header incomplete");
                            // not enough data to read complete header for new message
                            state.HeaderBuffer = new byte[MESSAGE_HEADER_LENGTH]; // create new header data buffer to store a partial message header
                            int lengthToRead = dataLength - currentByte;

                            //Array.Copy(dataArray, currentByte, state.CurrentHeaderBuffer, 0, lengthToRead); // read header data into header buffer
                            dataSpan.Slice(currentByte, lengthToRead).CopyTo(state.HeaderBuffer.AsSpan()); // read header data into header buffer

                            currentByte += lengthToRead;
                            state.HeaderBytesRead = lengthToRead;
                            state.IsHeaderIncomplete = true;
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogError("Error while parsing network data. Message: " + e.Message + "\nInner Exception Message: " + e.InnerException.Message + "\nStack Trace: " + e.StackTrace);
            }
        }
    }

}