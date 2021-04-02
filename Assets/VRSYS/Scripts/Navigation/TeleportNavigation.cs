using Photon.Pun;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;

namespace Vrsys
{
    public class TeleportNavigation : NavigationBaseHMD, IPunObservable
    {
        public LayerMask myLayerMask = -1; // -1 everything

        public enum IntersectionMode { Line, Parabola };
        public IntersectionMode activeIntersectionMode;
        public float parabolaVel = 10; // defines curvature
        [Range(10, 100)]
        public int maxSegmentCount = 50;
        public float segmentLength = 0.3f;
        public enum TransitionMode { Instant, Dash };
        public TransitionMode activeTransitionMode;
        [Range(0.5f, 5.0f)] // in seconds
        public float transitionDuration = 1.0f;

        public bool considerTargetNormal = true;
        [Range(0.0f, 180.0f)] // in degress
        public float maxDeviationAngle = 65.0f;

        private GameObject intersectionSphere;
        private GameObject posePreview;
        private UnityEngine.XR.InputDevice rightXRInputDevice;

        private LineRenderer rayRenderer;
        private RaycastHit rayHit;

        private bool gripButton = false;
        private bool validTargetPoseFlag = false;
        private bool rotTargetPoseFlag = false;

        private bool animationFlag = false;
        private float animationStartTime = 0.0f;
        private Vector3 animationStartPos = Vector3.zero;
        private Vector3 animationTargetPos = Vector3.zero;
        private Quaternion animationStartRot = Quaternion.identity;
        private Quaternion animationTargetRot = Quaternion.identity;

        public GameObject posePreviewGeometry;

        public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
        {
            if (stream.IsWriting)
            {
                stream.SendNext(gripButton);
                //stream.SendNext(endRay);
            }
            else
            {
                this.gripButton = (bool)stream.ReceiveNext();
                //int p = (int)stream.ReceiveNext();
                //press = p;
                //if (press != 0)
                //{
                //    Debug.Log("receive: " + press);
                //}

                //Vector3 end = (Vector3)stream.ReceiveNext();
                //if (press == 1)
                //{

                //    lineRenderer.enabled = true;
                //    lineRenderer.positionCount = 2;
                //    lineRenderer.SetPosition(0, rightHand.transform.position);
                //    lineRenderer.SetPosition(1, endRay);

                //}
                //else
                //{
                //    lineRenderer.enabled = false;

                //}



            }

        }

        // Start is called before the first frame update
        protected override void Start()
        {
            base.Start();

            rayRenderer = viewingSetupHMD.rightController.AddComponent<LineRenderer>();
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
            posePreview = Instantiate(posePreviewGeometry);
            posePreview.name = "Teleport Pose Preview";
            posePreview.SetActive(false); // hide
        }

        // Update is called once per frame
        void Update()
        {
            CheckForXRInputDevices(); // rework this

            if (rightXRInputDevice.characteristics.ToString() != "None") // rework this
            {
                bool button = false;
                rightXRInputDevice.TryGetFeatureValue(CommonUsages.gripButton, out button);
                if (button != gripButton) // state has changed
                {
                    rayRenderer.enabled = button; // enable/disable ray/parabola visualization
                    if (button == false) // released
                    {
                        intersectionSphere.SetActive(false); // hide
                        if (posePreview.activeSelf == true) EnablePreviewPose(false); // start transition
                    }
                }
                else
                {
                    if (button == true)
                    {
                        // intersection
                        switch (activeIntersectionMode)
                        {
                            case IntersectionMode.Line:
                                LineIntersection();
                                break;
                            case IntersectionMode.Parabola:
                                ParabolaIntersection();
                                break;
                        }

                        if (posePreview.activeSelf == true) UpdatePreviewPoseOrientation();
                    }
                }
                gripButton = button;

                float grip = 0.0f;
                rightXRInputDevice.TryGetFeatureValue(CommonUsages.grip, out grip);

                if (grip > 0.8f)
                {
                    if (posePreview.activeSelf == false) EnablePreviewPose(true);
                }
            }

            AnimateTransform();
        }

        private void EnablePreviewPose(bool flag)
        {
            posePreview.SetActive(flag); // show/hide visualization

            if (flag == true) // enable
            {
                rotTargetPoseFlag = false;

                if (rayHit.collider != null) // something hit
                {
                    posePreview.transform.position = rayHit.point; // set preview pose position

                    Quaternion newRot = Quaternion.LookRotation(viewingSetup.mainCamera.transform.forward, Vector3.up);
                    posePreview.transform.rotation = Quaternion.Euler(0f, newRot.eulerAngles.y, 0f); // set initial preview pose orientation (can be updated later)

                    if (considerTargetNormal)
                    {
                        float angle = Vector3.Angle(rayHit.normal, Vector3.up);
                        if (angle <= maxDeviationAngle)
                        {
                            validTargetPoseFlag = true;
                            posePreview.GetComponentInChildren<MeshRenderer>().material.color = new Color(0.0f, 1.0f, 0.0f, 0.4f);
                        }
                        else
                        {
                            validTargetPoseFlag = false;
                            posePreview.GetComponentInChildren<MeshRenderer>().material.color = new Color(1.0f, 0.0f, 0.0f, 0.4f);
                        }
                    }
                    else
                    {
                        validTargetPoseFlag = true;
                        posePreview.GetComponentInChildren<MeshRenderer>().material.color = new Color(0.0f, 1.0f, 0.0f, 0.4f);
                    }
                }
                else validTargetPoseFlag = false; // nothing hit
            }
            else // disable
            {
                if (validTargetPoseFlag == false) return;

                switch (activeTransitionMode)
                {
                    case TransitionMode.Instant:
                        {
                            // compensate rotation offset (between platform center and HMD/camera)
                            Quaternion offsetRot = transform.rotation * Quaternion.Inverse(viewingSetup.mainCamera.transform.rotation);
                            offsetRot = Quaternion.Euler(0.0f, offsetRot.eulerAngles.y, 0.0f); // offset compensation only for YAW rotation
                            transform.rotation = posePreview.transform.rotation * offsetRot;

                            // compensate position offset (between platform center and HMD/camera)
                            Vector3 offsetTrans = viewingSetup.mainCamera.transform.position - transform.position;
                            offsetTrans.y = 0.0f; // ignore height offset
                            transform.position = posePreview.transform.position - offsetTrans;
                        }
                        break;
                    case TransitionMode.Dash:
                        StartTransformAnimation(transform.position, transform.rotation, posePreview.transform.position, posePreview.transform.rotation);
                        break;
                }
            }
        }

        private void UpdatePreviewPoseOrientation()
        {
            if (rayHit.collider != null) // something hit
            {
                if (rotTargetPoseFlag == false) // check if target pose orientation adjustment can be enabled 
                {
                    Vector3 diffVec = rayHit.point - posePreview.transform.position;
                    diffVec.y = 0.0f; // ignore y component

                    if (diffVec.magnitude > 0.35f) rotTargetPoseFlag = true; // enable target pose orientation adjustment (if pointed outside the preview ring for the first time)
                }

                if (rotTargetPoseFlag == true)
                {
                    // update orientation - rotate towards direction of new hit point
                    Quaternion offsetRot = Quaternion.LookRotation((rayHit.point - posePreview.transform.position).normalized, Vector3.up);
                    posePreview.transform.rotation = Quaternion.Euler(0f, offsetRot.eulerAngles.y, 0f);
                }
            }
        }

        private void StartTransformAnimation(Vector3 startPos, Quaternion startRot, Vector3 targetPos, Quaternion targetRot)
        {
            //Debug.Log("Start Animation");
            animationFlag = true;
            animationStartTime = Time.time;
            animationStartPos = startPos;
            animationTargetPos = targetPos;
            animationStartRot = startRot;
            animationTargetRot = targetRot;
        }


        private void AnimateTransform()
        {
            if (animationFlag == false) return;

            float factor = (Time.time - animationStartTime) / transitionDuration;
            //Debug.Log("animate: " + factor);
            if (factor < 1.0)
            {
                // animate rotation -> slerp
                Quaternion offsetRot = transform.rotation * Quaternion.Inverse(viewingSetup.mainCamera.transform.rotation); // compensate for Head-XRRig rotation offset
                offsetRot = Quaternion.Euler(0.0f, offsetRot.eulerAngles.y, 0.0f); // offset compensation only for YAW rotation
                Quaternion newRot = Quaternion.Slerp(animationStartRot, animationTargetRot * offsetRot, factor);
                transform.rotation = newRot;

                // animate position -> lerp
                Vector3 offsetTrans = viewingSetup.mainCamera.transform.position - transform.position; // compensate for Head-XRRig position offset
                offsetTrans.y = 0.0f; // ignore height offset
                Vector3 newPos = Vector3.Lerp(animationStartPos, animationTargetPos - offsetTrans, factor);
                transform.position = newPos;
            }
            else // animation finished -> snap to exact target pose
            {
                // Debug.Log("Stop Animation");
                animationFlag = false;

                // compensate rotation offset (between platform center and HMD/camera)
                Quaternion offsetRot = transform.rotation * Quaternion.Inverse(viewingSetup.mainCamera.transform.rotation);
                offsetRot = Quaternion.Euler(0.0f, offsetRot.eulerAngles.y, 0.0f); // offset compensation only for YAW rotation
                transform.rotation = animationTargetRot * offsetRot;

                // compensate position offset (between platform center and HMD/camera)
                Vector3 offsetTrans = viewingSetup.mainCamera.transform.position - transform.position;
                offsetTrans.y = 0.0f; // ignore height offset
                transform.position = animationTargetPos - offsetTrans;
            }
        }

        private void LineIntersection()
        {
            rayRenderer.positionCount = 2; // only one line segment
                                           // Does the ray intersect any objects
            if (Physics.Raycast(viewingSetupHMD.rightController.transform.position, viewingSetupHMD.rightController.transform.forward, out rayHit, Mathf.Infinity, myLayerMask))
            {
                //Debug.Log("Did Hit");
                // update ray visualization
                rayRenderer.SetPosition(0, viewingSetupHMD.rightController.transform.position);
                rayRenderer.SetPosition(1, rayHit.point);

                // update intersection sphere visualization
                intersectionSphere.SetActive(true); // show
                intersectionSphere.transform.position = rayHit.point;
            }
            else
            {
                //Debug.Log("Did not Hit");
                // update ray visualization
                rayRenderer.SetPosition(0, viewingSetupHMD.rightController.transform.position);
                rayRenderer.SetPosition(1, viewingSetupHMD.rightController.transform.position + viewingSetupHMD.rightController.transform.TransformDirection(Vector3.forward) * 1000);

                // update intersection sphere visualization
                intersectionSphere.SetActive(false); // hide
            }
        }

        private void ParabolaIntersection()
        {
            Vector3[] segments = new Vector3[maxSegmentCount];
            int hitIDX = 0;
            bool hitFlag = false;

            //start of the jumping ray at the position of the object this script is attached to
            segments[0] = viewingSetupHMD.rightController.transform.position;

            // initial velocity
            Vector3 segVel = viewingSetupHMD.rightController.transform.forward * parabolaVel;// Time.deltaTime;

            // calculate Raycast
            for (int i = 1; i < maxSegmentCount; i++)
            {
                if (Physics.Raycast(segments[i - 1], segVel, out rayHit, segmentLength, myLayerMask))
                {
                    hitFlag = true;
                    hitIDX = i;
                    segments[i] = rayHit.point; // final segment

                    // update intersection sphere visualization
                    intersectionSphere.SetActive(true); // show
                    intersectionSphere.transform.position = rayHit.point;

                    break;
                }
                else
                {
                    // Time to traverse one segment of segScale; scale/length if length not 0; 0 else
                    float segTime = (segVel.sqrMagnitude != 0) ? segmentLength / segVel.magnitude : 0;

                    //add velocity for current segments timestep
                    segVel = segVel + Physics.gravity * segTime;

                    segments[i] = segments[i - 1] + segVel * segTime;
                }
            }

            // update parabola visualization
            if (hitFlag)
            {
                rayRenderer.positionCount = hitIDX;
            }
            else
            {
                rayRenderer.positionCount = maxSegmentCount;
                intersectionSphere.SetActive(false); // hide
            }

            for (int i = 0; i < rayRenderer.positionCount; i++) rayRenderer.SetPosition(i, segments[i]);
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
}
