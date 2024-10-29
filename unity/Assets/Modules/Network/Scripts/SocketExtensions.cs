using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;

namespace IMLD.MixedReality.Network
{
    public static class SocketExtensions
    {
        public static void Kill(this Socket socket)
        {
#if NETFX_CORE
            socket.Dispose();
#else
            socket.Close();
#endif
        }
    }
}
