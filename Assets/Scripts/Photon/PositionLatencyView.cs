using System.Collections;
using System.Collections.Generic;
using Photon.Pun;
using UnityEngine;

public class PositionLatencyView : MonoBehaviourPun, IPunObservable
{
    // Update is called once per frame
    void Update()
    {
        
    }
    
    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (stream.IsWriting)
        {
            stream.SendNext(transform.position);
        }
        else
        {
            Vector3 networkPosition = (Vector3)stream.ReceiveNext();

            double latency = PhotonNetwork.Time - info.SentServerTime;
            
            float latencyInMs = (float)latency * 1000;
            
            Log.Info($"Latency: {latencyInMs} ms");
        }
    }

}
