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
    //public  XRController rightXRController;
    public Vector3 endRay;


    private GameObject mainCamera = null;
    //private GameObject platformCenter = null;
    private GameObject rightHandController = null;
    private XRController rightXRController = null;

    private Vector3 startPosition = Vector3.zero;
    private Quaternion startRotation = Quaternion.identity;
    private Quaternion rotTowardsHit = Quaternion.identity;

    public bool triggerPressed = false;
    public bool triggerReleased = false;
    private bool secondaryButtonLF = false;
    private Vector3 jumpingTargetPosition;
    private Vector3 centerOffset;

    private LineRenderer rightRayRenderer;
    private LineRenderer offsetRenderer;

    private bool rayOnFlag = false;

    public LayerMask myLayerMask;

    private GameObject rightRayIntersectionSphere = null;
    private GameObject jumpingPositionPreview = null;
    private GameObject jumpingPersonPreview = null;

    private RaycastHit hit;

    // YOUR CODE (IF NEEDED) - BEGIN 
    private float height = 1.0f;
    private Vector3 avatarDirection;

    // YOUR CODE - END    

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

            //rightXRController = transform.GetParentComponent<Vrsys.ViewingSetupHMDAnatomy>().rightController.GetComponent<XRController>();
            //Debug.Log("xr controller: " + rightXRController);

        }
        if (PhotonNetwork.IsMasterClient)
        {
            Debug.Log("I am the master");
            //gameObject.GetComponentInParent<Vrsys.TeleportNavigation>().enabled = true;

            startPosition = transform.position;
            startRotation = transform.rotation;

            mainCamera = GameObject.Find("Main Camera");
            //platformCenter = GameObject.Find("Center");
            rightHandController = GetComponent<Vrsys.AvatarHMDAnatomy>().handRight; 
            offsetRenderer = GetComponent<LineRenderer>();
            offsetRenderer.startWidth = 0.01f;
            offsetRenderer.positionCount = 2;

            if (rightHandController != null) // guard
            {
                rightXRController = transform.GetParentComponent<Vrsys.ViewingSetupHMDAnatomy>().rightController.GetComponent<XRController>();
                rightRayRenderer = rightHandController.AddComponent<LineRenderer>();
                rightRayRenderer.name = "Right Ray Renderer";
                rightRayRenderer.startWidth = 0.01f;
                rightRayRenderer.positionCount = 2;
                rayOnFlag = true;

                // geometry for intersection visualization
                rightRayIntersectionSphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                rightRayIntersectionSphere.name = "Right Ray Intersection Sphere";
                rightRayIntersectionSphere.transform.localScale = new Vector3(0.05f, 0.05f, 0.05f);
                rightRayIntersectionSphere.GetComponent<MeshRenderer>().material.color = Color.blue;
                rightRayIntersectionSphere.GetComponent<SphereCollider>().enabled = false; // disable for picking ?!
                rightRayIntersectionSphere.SetActive(false); // hide

                // geometry for Navidget visualization
                Material previewMaterial = new Material(Shader.Find("Standard"));
                previewMaterial.color = new Color(1.0f, 0.0f, 0.0f, 0.4f);
                previewMaterial.SetOverrideTag("RenderType", "Transparent");
                previewMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
                previewMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                previewMaterial.SetInt("_ZWrite", 0);
                previewMaterial.DisableKeyword("_ALPHATEST_ON");
                previewMaterial.DisableKeyword("_ALPHABLEND_ON");
                previewMaterial.EnableKeyword("_ALPHAPREMULTIPLY_ON");
                previewMaterial.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;

                jumpingPositionPreview = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                jumpingPositionPreview.transform.localScale = new Vector3(1f, 0.02f, 1f);
                jumpingPositionPreview.name = "Navidget Intersection Sphere";
                jumpingPositionPreview.layer = LayerMask.NameToLayer("Ignore Raycast");
                jumpingPositionPreview.GetComponent<MeshRenderer>().material = previewMaterial;
                jumpingPositionPreview.SetActive(false); // hide

                jumpingPersonPreview = Instantiate(Resources.Load("Resources/Prefabs/RealisticAvatar.prefab"), startPosition, startRotation) as GameObject;
                jumpingPersonPreview.layer = LayerMask.NameToLayer("Ignore Raycast");
                jumpingPersonPreview.SetActive(false);

                // YOUR CODE (IF NEEDED) - BEGIN 

                // YOUR CODE - END    

            }


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
            //rightXRController.inputDevice.TryGetFeatureValue(CommonUsages.trigger, out trigger);

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
