using Photon.Pun;
using UnityEngine;

public class PositionExtrapolationView : MonoBehaviourPun,IPunObservable
{
    private Rigidbody rb;
    private void Awake()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (!photonView.IsMine)
        {
            /*float lag = (float)(PhotonNetwork.Time - lastPacketTime);

            Vector3 extrapolatedPosition = newtworkPosition + (networkVelocity * lag);*/
            
            
        }
    }
    
    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (stream.IsWriting)
        {
            stream.SendNext(transform.position);
            stream.SendNext(rb.velocity);
        }
    }
}
