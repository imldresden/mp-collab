using System;
using System.Net;
using System.Net.Sockets;
using UnityEngine;

namespace IMLD.MixedReality.Network
{
    public class ClientUdp : Client
    {
        private Socket _client;
        private IPEndPoint _endPoint;
        private bool _isOpen;

        public override bool IsOpen
        {
            get { return _client != null; }
        }

        public ClientUdp(string ipAdress, int port)
            : base (ipAdress, port)
        {
            _isOpen = false;
            _client = null;
            _endPoint = new IPEndPoint(IPAddress.Parse(_ipAddress), _port);
        }

        public override bool Open()
        {
            var args = new SocketAsyncEventArgs();
            try
            {
                _client = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
                _isOpen = true;
            }
            catch (Exception e)
            {
                Debug.LogError("SenderUdp - ERROR, could not open socket:\n" + e.Message);
                return false;
            }
            return true;
        }

        public override bool Send(byte[] data)
        {
            if (!_isOpen)
                return false;

            var args = new SocketAsyncEventArgs();
            args.SetBuffer(data, 0, data.Length);
            //args.SetBuffer(data);
            args.RemoteEndPoint = _endPoint;
            try
            {
                _client.SendToAsync(args);
            }
            catch (Exception e)
            {
                Debug.LogError("SenderUdp - ERROR while sending data:\n" + e.Message);
                return false;
            }

            return true;
        }

        public override void Close()
        {
            _isOpen = false;
            if (_client != null)
            {
                _client.Kill();
                _client = null;
            }
        }
    }
}
