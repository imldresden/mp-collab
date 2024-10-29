using System;
using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using ZstdSharp;

public class TestZStd
{
    private static Compressor Compressor = new Compressor(3);
    private static Decompressor Decompressor = new Decompressor();
    [Test]
    public void TestZStdSmallIncompressible()
    {
        var rnd = new System.Random();
        byte[] uncompressedData;
        Span<byte> compressedData;
        Span<byte> decompressedData;
        for (int i = 0; i < 100; i++)
        {
            int size = rnd.Next(100, 1000);
            rnd.NextBytes(uncompressedData = new byte[size]);
            compressedData = Compressor.Wrap(uncompressedData);
            decompressedData = Decompressor.Unwrap(compressedData);
            Debug.Log("Uncompressed size: " + uncompressedData.Length + ", compressed size: " + compressedData.Length + ", decompressed size: " + decompressedData.Length);
            Assert.That(uncompressedData.Length, Is.EqualTo(decompressedData.Length));
            for (int j = 0; j < decompressedData.Length; j++)
            {
                if (uncompressedData[j] != decompressedData[j])
                {
                    Assert.Fail();
                }
            }
        }
    }

    [Test]
    public void TestZStdSmallCompressible()
    {
        var rnd = new System.Random();
        byte[] uncompressedData;
        Span<byte> compressedData;
        Span<byte> decompressedData;
        for (int i = 0; i < 100; i++)
        {
            int size = rnd.Next(100, 1000);
            uncompressedData = new byte[size];

            for (int j = 0;j < size; j++)
            {
                uncompressedData[j] = (byte)rnd.Next(0, 10);
            }

            compressedData = Compressor.Wrap(uncompressedData);
            decompressedData = Decompressor.Unwrap(compressedData);
            Debug.Log("Uncompressed size: " + uncompressedData.Length + ", compressed size: " + compressedData.Length + ", decompressed size: " + decompressedData.Length);
            Assert.That(uncompressedData.Length, Is.EqualTo(decompressedData.Length));
            for (int j = 0; j < decompressedData.Length; j++)
            {
                if (uncompressedData[j] != decompressedData[j])
                {
                    Assert.Fail();
                }
            }
        }
    }

    [Test]
    public void TestZStdLargeIncompressible()
    {
        var rnd = new System.Random();
        byte[] uncompressedData;
        Span<byte> compressedData;
        Span<byte> decompressedData;
        for (int i = 0; i < 10; i++)
        {
            int size = rnd.Next(100000, 1000000);
            rnd.NextBytes(uncompressedData = new byte[size]);
            compressedData = Compressor.Wrap(uncompressedData);
            decompressedData = Decompressor.Unwrap(compressedData);
            Debug.Log("Uncompressed size: " + uncompressedData.Length + ", compressed size: " + compressedData.Length + ", decompressed size: " + decompressedData.Length);
            Assert.That(uncompressedData.Length, Is.EqualTo(decompressedData.Length));
            for (int j = 0; j < decompressedData.Length; j++)
            {
                if (uncompressedData[j] != decompressedData[j])
                {
                    Assert.Fail();
                }
            }
        }
    }

    [Test]
    public void TestZStdLargeCompressible()
    {
        var rnd = new System.Random();
        byte[] uncompressedData;
        Span<byte> compressedData;
        Span<byte> decompressedData;
        for (int i = 0; i < 10; i++)
        {
            int size = rnd.Next(100000, 1000000);
            uncompressedData = new byte[size];

            for (int j = 0; j < size; j++)
            {
                uncompressedData[j] = (byte)rnd.Next(0, 10);
            }

            compressedData = Compressor.Wrap(uncompressedData);
            decompressedData = Decompressor.Unwrap(compressedData);
            Debug.Log("Uncompressed size: " + uncompressedData.Length + ", compressed size: " + compressedData.Length + ", decompressed size: " + decompressedData.Length);
            Assert.That(uncompressedData.Length, Is.EqualTo(decompressedData.Length));
            for (int j = 0; j < decompressedData.Length; j++)
            {
                if (uncompressedData[j] != decompressedData[j])
                {
                    Assert.Fail();
                }
            }
        }
    }

    // A UnityTest behaves like a coroutine in Play Mode. In Edit Mode you can use
    // `yield return null;` to skip a frame.
    [UnityTest]
    public IEnumerator TestZStdWithEnumeratorPasses()
    {
        // Use the Assert class to test conditions.
        // Use yield to skip a frame.
        yield return null;
    }
}
