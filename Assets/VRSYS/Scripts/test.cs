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
    public LineRenderer l;
    //private UnityEngine.XR.InputDevice rightXRInputDevice;
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

                l.enabled = true;
                l.positionCount = 2;
                l.SetPosition(0, rightHand.transform.position);
                l.SetPosition(1, endRay);

            }
            else
            {
                l.enabled = false;

            }
            


        }

    }


    // Start is called before the first frame update
    void Start()
    {

        

            
        press = 0;
        rightHand = GetComponent<Vrsys.AvatarHMDAnatomy>().handRight;
        l = rightHand.GetComponent<LineRenderer>();
        l.startWidth = 0.1f;
        l.positionCount = 0;
        l.enabled = false;

        if (photonView.IsMine)
        {

            rightXRController = transform.GetParentComponent<Vrsys.ViewingSetupHMDAnatomy>().rightController.GetComponent<XRController>();
            Debug.Log("xr controller: " + rightXRController);

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
                l.enabled = true;
                l.positionCount = 2;
                l.SetPosition(0, rightHand.transform.position);
                l.SetPosition(1, endRay);
                press = 1;

            }
            else
            {
                press = 0;
                l.enabled = false;


            }

        }


       
        
    }
}
