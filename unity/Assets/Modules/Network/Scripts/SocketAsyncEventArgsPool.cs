// Source: https://learn.microsoft.com/en-us/dotnet/api/system.net.sockets.socketasynceventargs.-ctor?view=net-7.0
// Represents a collection of reusable SocketAsyncEventArgs objects.
using System.Collections.Generic;
using System.Net.Sockets;
using System;
using UnityEngine;
using System.Buffers;

namespace IMLD.MixedReality.Network
{
    class SocketAsyncEventArgsPool
    {
        Stack<SocketAsyncEventArgs> _pool;

        // Initializes the object pool to the specified size
        //
        // The "capacity" parameter is the maximum number of
        // SocketAsyncEventArgs objects the pool can hold
        public SocketAsyncEventArgsPool(int capacity)
        {
            _pool = new Stack<SocketAsyncEventArgs>(capacity);
        }

        // Add a SocketAsyncEventArg instance to the pool
        //
        //The "item" parameter is the SocketAsyncEventArgs instance
        // to add to the pool
        public void Return(SocketAsyncEventArgs item)
        {
            if (item == null) { throw new ArgumentNullException("Items added to a SocketAsyncEventArgsPool cannot be null"); }
            lock (_pool)
            {
                _pool.Push(item);
            }
        }

        // Removes a SocketAsyncEventArgs instance from the pool
        // and returns the object removed from the pool
        public SocketAsyncEventArgs Rent()
        {
            SocketAsyncEventArgs args;

            lock (_pool)
            {
                if (_pool.Count > 0)
                {
                    args = _pool.Pop();
                }
                else
                {
                    args = new SocketAsyncEventArgs();
                    Debug.Log("Pool size increased!");
                }
            }

            return args;
        }

        //public SocketAsyncEventArgs Rent(int bufferSize)
        //{
        //    SocketAsyncEventArgs args = Rent();

        //    if (bufferSize > 0)
        //    {
        //        ArrayPool<byte>.Shared.Return(args.Buffer);
        //        var buffer = ArrayPool<byte>.Shared.Rent(bufferSize);
        //        args.SetBuffer(buffer, 0, bufferSize);
        //    }

        //    return args;
        //}

        // The number of SocketAsyncEventArgs instances in the pool
        public int Count
        {
            get { return _pool.Count; }
        }
    }
}
