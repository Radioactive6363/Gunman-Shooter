using Photon.Pun;
using UnityEngine;

[RequireComponent(typeof(PhotonView))]
public class Target : MonoBehaviourPun, IShootable
{
    [SerializeField] private bool useInterpolation = false;
    [SerializeField] private bool useExtrapolation = false;
    [SerializeField] private float velocidad = 3f;
    [SerializeField] private float limiteIzquierda = -5f;
    [SerializeField] private float limiteDerecha = 5f;

    private Extrapolation extrapolation;
    private NetworkRigidbodyInterpolation interpolation;
    private int direccion = 1;

    private void Awake()
    {
        extrapolation = GetComponent<Extrapolation>();
        interpolation = GetComponent<NetworkRigidbodyInterpolation>();
        SetPolation();
    }

    [ContextMenu("SetPolation")]
    public void SetPolation()
    {
        extrapolation.enabled = useExtrapolation;
        interpolation.enabled = useInterpolation;
    }
    
    void Update()
    {
        if (PhotonNetwork.IsMasterClient)
        {
            transform.Translate(Vector3.right * velocidad * direccion * Time.deltaTime);

            if (transform.position.x >= limiteDerecha)
            {
                direccion = -1;
            }
            else if (transform.position.x <= limiteIzquierda)
            {
                direccion = 1;
            }
        }
    }

    public void OnHit()
    {
        
    }
}
