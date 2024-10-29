using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Text;
using UnityEngine;
using static UnityEngine.Rendering.VirtualTexturing.Debugging;

namespace IMLD.MixedReality.Network
{
    public class FileWriterNetworkFilter : INetworkFilter, IDisposable
    {
        private bool _disposed = false;
        private FileStream _fileStream;

        private bool _headerWritten = false;

        private long _firstTimestamp = 0L;
        private long _lastTimestamp = 0L;
        private long _offsetFirstTimestamp = 0L;
        private long _offsetLastTimestamp = 0L;

        private const int LENGTH_TIMESTAMP = 8;
        private const int LENGTH_LENGTH = 4;
        private const int LENGTH_TYPE = 1;
        private const string MAGIC_STRING = "IML!";
        private const int VERSION = 1;

        public void Dispose()
        {
            Dispose(disposing: true);
        }

        public FileWriterNetworkFilter(string filePath)
        {
            _fileStream = File.OpenWrite(filePath);
        }

        protected virtual void Dispose(bool disposing)
        {
            // Check to see if Dispose has already been called.
            if (!_disposed)
            {
                // If disposing equals true, dispose all managed
                // and unmanaged resources.
                if (disposing)
                {
                    if (_fileStream != null)
                    {
                        if (_fileStream.CanWrite)
                        {
                            UpdateHeader();
                            _fileStream.Flush();
                        }
                        
                        _fileStream.Dispose();
                        _fileStream = null;
                    }
                }

                // Note disposing has been done.
                _disposed = true;
            }
        }

        public void FilterMessage(INetworkService networkService, ref MessageContainer messageContainer)
        {
            if (_fileStream == null || _fileStream.CanWrite == false) 
            {
                return;
            }

            if (_headerWritten == false)
            {
                if (WriteFileHeader(networkService))
                {
                    _headerWritten = true;
                }
                else
                {
                    return;
                }
            }

            // prepare data
            long timestamp = DateTimeOffset.Now.Ticks;

            if (_firstTimestamp == 0)
            {
                _firstTimestamp = timestamp;
            }

            _lastTimestamp = timestamp;

            // write message to file
            // ... header
            _fileStream.Write(BitConverter.GetBytes(timestamp), 0, LENGTH_TIMESTAMP);
            _fileStream.Write(BitConverter.GetBytes((int)messageContainer.Type), 0, LENGTH_LENGTH);
            _fileStream.Write(BitConverter.GetBytes(messageContainer.Payload.Length), 0, LENGTH_LENGTH);
            // ... data
            _fileStream.Write(messageContainer.Payload, 0, messageContainer.Payload.Length);
        }

        private void UpdateHeader()
        {
            if (_fileStream == null || _fileStream.CanWrite == false || _headerWritten == false)
            {
                return;
            }

            // update first timestamp in header
            _fileStream.Seek(_offsetFirstTimestamp, SeekOrigin.Begin);
            _fileStream.Write(BitConverter.GetBytes(_firstTimestamp), 0, LENGTH_TIMESTAMP);

            // update last timestamp in header
            _fileStream.Seek(_offsetLastTimestamp, SeekOrigin.Begin);
            _fileStream.Write(BitConverter.GetBytes(_lastTimestamp), 0, LENGTH_TIMESTAMP);
        }

        private bool WriteFileHeader(INetworkService networkService)
        {
            if (_fileStream == null || _fileStream.CanWrite == false || networkService == null)
            {
                return false;
            }

            // write magic string
            byte[] value = Encoding.UTF8.GetBytes(MAGIC_STRING);
            _fileStream.Write(value, 0, value.Length);

            // write version
            value = BitConverter.GetBytes(VERSION);
            _fileStream.Write(value, 0, value.Length);

            // write first timestamp placeholder
            _offsetFirstTimestamp = _fileStream.Position;
            _fileStream.Write(BitConverter.GetBytes(_firstTimestamp), 0, LENGTH_TIMESTAMP);

            // write last timestamp placeholder
            _offsetLastTimestamp = _fileStream.Position;
            _fileStream.Write(BitConverter.GetBytes(_lastTimestamp), 0, LENGTH_TIMESTAMP);

            // write session id
            value = Encoding.UTF8.GetBytes(networkService.ServiceDescription.SessionId.ToString());
            int length = value.Length;
            _fileStream.Write(BitConverter.GetBytes(length), 0, LENGTH_LENGTH);
            _fileStream.Write(value, 0, value.Length);

            // write service id
            value = Encoding.UTF8.GetBytes(networkService.ServiceDescription.ServiceId.ToString());
            length = value.Length;
            _fileStream.Write(BitConverter.GetBytes(length), 0, LENGTH_LENGTH);
            _fileStream.Write(value, 0, value.Length);

            // write service type
            value = BitConverter.GetBytes((int)networkService.ServiceDescription.Type);
            length = value.Length;
            _fileStream.Write(BitConverter.GetBytes(length), 0, LENGTH_LENGTH);
            _fileStream.Write(value, 0, value.Length);

            // write room id
            value = BitConverter.GetBytes(networkService.ServiceDescription.RoomId);
            length = value.Length;
            _fileStream.Write(BitConverter.GetBytes(length), 0, LENGTH_LENGTH);
            _fileStream.Write(value, 0, value.Length);

            // write service auxilliary data
            value = Encoding.UTF8.GetBytes(networkService.ServiceDescription.Data.ToString());
            length = value.Length;
            _fileStream.Write(BitConverter.GetBytes(length), 0, LENGTH_LENGTH);
            _fileStream.Write(value, 0, value.Length);

            return true;
        }
    }
}