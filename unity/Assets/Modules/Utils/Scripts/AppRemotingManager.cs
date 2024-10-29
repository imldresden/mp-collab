using Microsoft.MixedReality.OpenXR.Remoting;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using UnityEngine;

public class AppRemotingManager : MonoBehaviour
{
    [SerializeField]
    private string IP;

    private RemotingConnectConfiguration _connectConfiguration;

    // Start is called before the first frame update
    void Start()
    {
        var ip = GetArg("-ip");

        if (IPAddress.TryParse(ip, out _))
        {
            IP = ip;
        }

        _connectConfiguration = new RemotingConnectConfiguration();
        _connectConfiguration.EnableAudio = true;
        _connectConfiguration.MaxBitrateKbps = 20000;
        _connectConfiguration.RemoteHostName = IP;
        _connectConfiguration.RemotePort = 8265;
        _connectConfiguration.VideoCodec = RemotingVideoCodec.Auto;

        Microsoft.MixedReality.OpenXR.Remoting.AppRemoting.StartConnectingToPlayer(_connectConfiguration);
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private static string GetArg(string name)
    {
        var args = System.Environment.GetCommandLineArgs();
        for (int i = 0; i < args.Length; i++)
        {
            if (args[i] == name && args.Length > i + 1)
            {
                return args[i + 1];
            }
        }
        return null;
    }
}
