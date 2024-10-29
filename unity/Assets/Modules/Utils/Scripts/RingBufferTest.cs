using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RingBufferTest : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        RingBuffer<byte> buffer = new RingBuffer<byte>(100);
        Debug.Log("New buffer created. Capacity: " + buffer.Capacity + ", Count: " + buffer.Count);

        bool result = buffer.Write(new byte[10]);
        Debug.Log("Buffer updated, 10 bytes written. Capacity: " + buffer.Capacity + ", Count: " + buffer.Count + ", Read Head: " + buffer._readHead + ", Write Head: " + buffer._writeHead + ", success: " + result);

        result = buffer.Write(new byte[70]);
        Debug.Log("Buffer updated, 70 bytes written. Capacity: " + buffer.Capacity + ", Count: " + buffer.Count + ", Read Head: " + buffer._readHead + ", Write Head: " + buffer._writeHead + ", success: " + result);

        byte[] data;
        result = buffer.TryRead(20, out data);
        Debug.Log("Buffer updated, 20 bytes read. Capacity: " + buffer.Capacity + ", Count: " + buffer.Count + ", Read Head: " + buffer._readHead + ", Write Head: " + buffer._writeHead + ", success: " + result);

        result = buffer.Write(new byte[60]);
        Debug.Log("Buffer updated, 60 bytes written. Capacity: " + buffer.Capacity + ", Count: " + buffer.Count + ", Read Head: " + buffer._readHead + ", Write Head: " + buffer._writeHead + ", success: " + result);

        result = buffer.TryRead(100, out data);
        Debug.Log("Buffer updated, 100 bytes read. Capacity: " + buffer.Capacity + ", Count: " + buffer.Count + ", Read Head: " + buffer._readHead + ", Write Head: " + buffer._writeHead + ", success: " + result);

    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
