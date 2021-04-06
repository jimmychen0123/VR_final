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
    public XRController rightXRController;
    public Vector3 endRay;

    private bool gripButton;
    public GameObject posePreview;

    //public LayerMask myLayerMask = -1; // -1 everything

    //public enum IntersectionMode { Line, Parabola };
    //public IntersectionMode activeIntersectionMode;
    //public float parabolaVel = 10; // defines curvature
    //[Range(10, 100)]
    //public int maxSegmentCount = 50;
    //public float segmentLength = 0.3f;
    //public enum TransitionMode { Instant, Dash };
    //public TransitionMode activeTransitionMode;
    //[Range(0.5f, 5.0f)] // in seconds
    //public float transitionDuration = 1.0f;

    //public bool considerTargetNormal = true;
    //[Range(0.0f, 180.0f)] // in degress
    //public float maxDeviationAngle = 65.0f;

    private GameObject intersectionSphere;
    //private GameObject posePreview;

    private LineRenderer rayRenderer;
    //private RaycastHit rayHit;

    //private bool gripButton = false;
    //private bool validTargetPoseFlag = false;
    //private bool rotTargetPoseFlag = false;

    //private bool animationFlag = false;
    //private float animationStartTime = 0.0f;
    //private Vector3 animationStartPos = Vector3.zero;
    //private Vector3 animationTargetPos = Vector3.zero;
    //private Quaternion animationStartRot = Quaternion.identity;
    //private Quaternion animationTargetRot = Quaternion.identity;

    public GameObject posePreviewGeometry;


    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (stream.IsWriting)
        {
            if (PhotonNetwork.IsMasterClient)
            {
                stream.SendNext(gripButton);
                stream.SendNext(posePreview.transform.position);
            }
            

            //stream.SendNext(press);
            //stream.SendNext(endRay);

            
        }
        else
        {
            //int p = (int)stream.ReceiveNext();
            //press = p;

            bool b = (bool)stream.ReceiveNext();
            gripButton = b;

            if (press != 0)
            {
                Debug.Log("receive: " + press);
            }

            Vector3 end = (Vector3)stream.ReceiveNext();
            if (gripButton)
            {

                lineRenderer.enabled = true;
                lineRenderer.positionCount = 2;
                lineRenderer.SetPosition(0, rightHand.transform.position);
                lineRenderer.SetPosition(1, end);

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

        if (PhotonNetwork.IsMasterClient)
        {
            gripButton = gameObject.GetComponentInParent<Vrsys.TeleportNavigation>().gripButton;
            posePreview = gameObject.GetComponentInParent<Vrsys.TeleportNavigation>().posePreview;

        }
       
        press = 0;
        rightHand = GetComponent<Vrsys.AvatarHMDAnatomy>().handRight;
        lineRenderer = rightHand.GetComponent<LineRenderer>();
        lineRenderer.startWidth = 0.1f;
        lineRenderer.positionCount = 0;
        lineRenderer.enabled = false;

        rayRenderer = rightHand.GetComponent<LineRenderer>();
        rayRenderer.name = "Ray Renderer";
        rayRenderer.startWidth = 0.01f;
        rayRenderer.enabled = false;

        // geometry for intersection visualization
        intersectionSphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        //intersectionSphere.transform.parent = this.gameObject.transform;
        intersectionSphere.name = "Ray Intersection Sphere";
        intersectionSphere.transform.localScale = new Vector3(0.05f, 0.05f, 0.05f);
        intersectionSphere.GetComponent<MeshRenderer>().material.color = Color.blue;
        //intersectionSphere.layer = LayerMask.NameToLayer("Ignore Raycast");
        intersectionSphere.GetComponent<SphereCollider>().enabled = false; // disable for picking ?!
        intersectionSphere.SetActive(false); // hide

        // geometry for teleport pose preview
        // posePreview = Instantiate(posePreviewGeometry);
        // posePreview.name = "Teleport Pose Preview";
        // posePreview.SetActive(false); // hide

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

        CheckForXRInputDevices(); // rework this

        if (photonView.IsMine)
        {
            if (rightXRInputDevice.characteristics.ToString() != "None") // rework this
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

                    rayRenderer.enabled = true;
                    rayRenderer.positionCount = 2;
                    rayRenderer.SetPosition(0, rightHand.transform.position);
                    rayRenderer.SetPosition(1, endRay);
                    press = 1;

        

                }
                else
                {
                    press = 0;
                    lineRenderer.enabled = false;
                    rayRenderer.enabled = false;
                    intersectionSphere.SetActive(false); // hide
                   // if (posePreview.activeSelf == true) EnablePreviewPose(false); // start transition
                }

                // gripButton = button;

                float grip = 0.0f;
                rightXRInputDevice.TryGetFeatureValue(CommonUsages.grip, out grip);

                if (grip > 0.8f)
                {
                    // if (posePreview.activeSelf == false) EnablePreviewPose(true);
                }
            }

        }

    }

    private void CheckForXRInputDevices()
    {
        // this is shitty
        if (rightXRInputDevice.characteristics.ToString() == "None")
        {
            var rightHandDevices = new List<UnityEngine.XR.InputDevice>();
            UnityEngine.XR.InputDevices.GetDevicesAtXRNode(UnityEngine.XR.XRNode.RightHand, rightHandDevices);
            if (rightHandDevices.Count > 0)
            {
                rightXRInputDevice = rightHandDevices[0];
                Debug.Log(string.Format("Device name '{0}' with role '{1}'", rightXRInputDevice.name, rightXRInputDevice.characteristics.ToString()));
            }
        }
    }
}