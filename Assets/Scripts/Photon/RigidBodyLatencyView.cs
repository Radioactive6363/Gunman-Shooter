using System;
using Photon.Pun;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class RigidBodyLatencyView : MonoBehaviourPun,IPunObservable
{
    private void FixedUpdate()
    {
        if (photonView.IsMine) return;
        
        //Vector3 nextPositon = Vector3.Lerp(rb.position, targetPosition, Time.deltaTime * interpolationTime);
        //rb.MovePosition(nextPositon);
    }

    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        throw new NotImplementedException();
    }
}
