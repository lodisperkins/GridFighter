using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityGGPO;
using SharedGame;
using Unity.Collections;

public struct GridGame : IGame
{

    public int Framenumber { get; private set; }

    public int Checksum => GetHashCode();

    public void FreeBytes(NativeArray<byte> data)
    {
        throw new System.NotImplementedException();
    }

    public void FromBytes(NativeArray<byte> data)
    {
        throw new System.NotImplementedException();
    }

    public void LogInfo(string filename)
    {
        throw new System.NotImplementedException();
    }

    public long ReadInputs(int controllerId)
    {
        throw new System.NotImplementedException();
    }

    public NativeArray<byte> ToBytes()
    {
        throw new System.NotImplementedException();
    }

    public void Update(long[] inputs, int disconnectFlags)
    {
        throw new System.NotImplementedException();
    }
}
