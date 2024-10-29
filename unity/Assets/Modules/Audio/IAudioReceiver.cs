using IMLD.MixedReality.Network;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

public interface IAudioReceiver
{
    public void ConnectToAudioService(NetworkServiceDescription service);
}
