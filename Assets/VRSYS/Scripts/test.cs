using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine.XR;
using UnityEngine.XR.Interaction.Toolkit;

public class test : MonoBehaviourPunCallbacks, IPunObservable
{
    int press = 0;
    public GameObject rightHand;
    public LineRenderer lineRenderer;
    private UnityEngine.XR.InputDevice rightXRInputDevice;
    public  XRController rightXRController;
    public Vector3 endRay;
    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (stream.IsWriting)
        {
            stream.SendNext(press);
            stream.SendNext(endRay);
        }
        else
        {
            int p = (int)stream.ReceiveNext();
            press = p;
            if (press != 0)
            {
                Debug.Log("receive: " + press);
            }

            Vector3 end = (Vector3)stream.ReceiveNext();
            if(press == 1)
            {

                lineRenderer.enabled = true;
                lineRenderer.positionCount = 2;
                lineRenderer.SetPosition(0, rightHand.transform.position);
                lineRenderer.SetPosition(1, endRay);

            }
            else
            {
                lineRenderer.enabled = false;

            }
            


        }

    }


    // Start is called before the first frame update
    void Start()
    {

        

            
        press = 0;
        rightHand = GetComponent<Vrsys.AvatarHMDAnatomy>().handRight;
        lineRenderer = rightHand.GetComponent<LineRenderer>();
        lineRenderer.startWidth = 0.1f;
        lineRenderer.positionCount = 0;
        lineRenderer.enabled = false;

        if (photonView.IsMine)
        {

            rightXRController = transform.GetParentComponent<Vrsys.ViewingSetupHMDAnatomy>().rightController.GetComponent<XRController>();
            Debug.Log("xr controller: " + rightXRController);

        }
        if (PhotonNetwork.IsMasterClient)
        {
            Debug.Log("I am the master");
            gameObject.GetComponentInParent<Vrsys.TeleportNavigation>().enabled = true;


        }



    }

    

    // Update is called once per frame
    void Update()
    {
        

        if (photonView.IsMine)
        {

            
            //if (Input.GetKeyDown(KeyCode.Space))
            //{
            //    Debug.Log("keydown");
            //    press = 1;

            //}

            float trigger = 0.0f;
            rightXRController.inputDevice.TryGetFeatureValue(CommonUsages.trigger, out trigger);

            //bool primaryGB = false;
            //rightXRInputDevice.TryGetFeatureValue(CommonUsages.gripButton, out primaryGB);

            if (trigger > 0.1f)
            {
                endRay = rightHand.transform.position + rightHand.transform.forward * 2;
                lineRenderer.enabled = true;
                lineRenderer.positionCount = 2;
                lineRenderer.SetPosition(0, rightHand.transform.position);
                lineRenderer.SetPosition(1, endRay);
                press = 1;

            }
            else
            {
                press = 0;
                lineRenderer.enabled = false;


            }

        }


       
        
    }
}
