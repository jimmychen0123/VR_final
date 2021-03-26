using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;

public class TutorialSerialization : MonoBehaviourPunCallbacks, IPunObservable
{
    private GameObject indicator;
    private Vector3 currentIndicatorLocation;

    // Start is called before the first frame update
    void Start()
    {
        currentIndicatorLocation = new Vector3();
        indicator = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        indicator.transform.localScale = new Vector3(0.3f, 0.3f, 0.3f);
        indicator.layer = LayerMask.NameToLayer("Ignore Raycast");
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetMouseButton(0))
        { // if left button pressed...
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;
            if (Physics.Raycast(ray, out hit, LayerMask.GetMask("InteractivePlane")))
            {
                currentIndicatorLocation = hit.point;
            }
            else
            {
                currentIndicatorLocation = this.transform.position;
            }
        }
        else
        {
            currentIndicatorLocation = this.transform.position;
        }

        if (photonView.IsMine)
        {
            indicator.transform.position = currentIndicatorLocation;
        }
    }

    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (stream.IsWriting)
        {
            stream.SendNext(currentIndicatorLocation);
        }
        else
        {
            currentIndicatorLocation = (Vector3)stream.ReceiveNext();
            indicator.transform.position = currentIndicatorLocation;
        }
    }
}
