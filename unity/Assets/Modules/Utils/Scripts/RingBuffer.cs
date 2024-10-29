using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RingBuffer<T>
{
    public int Count { get; private set; }
    public int Capacity { get; private set; }

    public IReadOnlyList<T> Buffer { get { return _buffer; } }

    private T[] _buffer;
    public int _readHead, _writeHead;

    public object _lock = new object();

    public RingBuffer(int capacity)
    {
        _buffer = new T[capacity];
        _readHead = 0;
        _writeHead = 0;
        Capacity = capacity;
        Count = 0;
    }

    public void Put(T item)
    {
        lock (_lock)
        {
            _buffer[_writeHead] = item;
            _writeHead = (_writeHead + 1) % Capacity;
            if (Count < Capacity)
            {
                Count++;
            }
            else
            {
                _readHead = _writeHead;
            }
        }
    }

    public bool Write(T[] data)
    {
        lock(_lock)
        {
            // return false if there is no data to write or if the capacity of the buffer is too small
            if (data == null || data.Length == 0 || data.Length > Capacity)
            {
                Debug.Log("Writing to buffer failed.");
                return false;
            }

            int firstSegmentLength, secondSegmentLength;

            // first segment goes from the write head to the end of the buffer
            firstSegmentLength = Math.Min(data.Length, Capacity - _writeHead);

            // second segment (if needed), goes from the start until we have written all data
            secondSegmentLength = data.Length - firstSegmentLength;

            if (firstSegmentLength > 0)
            {
                Array.Copy(data, 0, _buffer, _writeHead, firstSegmentLength); // write at end of buffer, from the write head
            }

            if (secondSegmentLength > 0)
            {
                Array.Copy(data, firstSegmentLength, _buffer, 0, secondSegmentLength); // write from the start of the buffer
            }

            // compute new write head position based on amount of data written and the capacity of the buffer
            _writeHead = (_writeHead + data.Length) % Capacity;

            // if we wrote more data than the remaining capacity, compute the new read head (we just lost some data!)
            if (data.Length > Capacity - Count)
            {
                _readHead = _writeHead;
            }

            // update the count
            Count = Math.Min(Count + data.Length, Capacity);
            //Debug.Log("Writing to Buffer: " + data.Length + ". New count: " + Count);
            return true;
        }  
    }

    public bool TryRead(int count, out T[] data)
    {
        lock(_lock)
        {
            // not enough data, return false
            if (Count < count || count <= 0)
            {
                data = null;
                //Debug.Log("Reading from buffer failed, count too low.");
                return false;
            }

            // initialize out variable
            data = new T[count];

            if (_readHead < _writeHead)
            {
                Array.Copy(_buffer, _readHead, data, 0, count); // just copy starting from the read head
            }
            else
            {
                int firstSegmentLength, secondSegmentLength;

                firstSegmentLength = Math.Min(count, Capacity - _readHead);
                secondSegmentLength = count - firstSegmentLength;

                Array.Copy(_buffer, _readHead, data, 0, firstSegmentLength); // copy from read head to the end of the buffer

                if (secondSegmentLength > 0)
                {
                    Array.Copy(_buffer, 0, data, firstSegmentLength - 1, secondSegmentLength); // copy from the start of the buffer until we are done
                }
            }

            // update first index and count
            _readHead = (_readHead + count) % Capacity;
            Count -= count;

            //Debug.Log("Reading from Buffer: " + count + ". New count: " + Count);
            return true;
        }        
    }

    public T Get()
    {
        lock (_lock)
        {
            // not enough data, return false
            if (Count <= 0)
            {
                throw new InvalidOperationException("RingBuffer is empty!");
            }

            // read from the read head
            var data = _buffer[_readHead];

            // compute new read head
            _readHead = (_readHead + 1) % Capacity;
            Count--;

            return data;
        }
    }

    public bool TryPeek(int position, out T data)
    {
        lock (_lock)
        {
            // not enough data, return false
            if (Count <= position)
            {
                data = default(T);
                //Debug.Log("Reading from buffer failed, count too low.");
                return false;
            }

            // compute temp read head
            int tempReadHead = (_readHead + position) % Capacity;

            // read from the temp read head
            data = _buffer[tempReadHead];
            return true;
        }
    }

    public IEnumerable<T> GetIterator()
    {
        for (int i = 0; i < Count; i++)
        {
            if (TryPeek(i, out T item))
            {
                yield return item;
            }            
        }
    }
}
