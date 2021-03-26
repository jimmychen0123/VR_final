using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;
//using UnityEngine.XR.Interaction.Toolkit;

namespace Vrsys
{
    public class FlystickNavigation : NavigationBaseHMD
    {
        [Tooltip("Translation Velocity [m/sec]")]
        [Range(0.1f, 10.0f)]
        public float transVel = 1.0f;

        [Tooltip("Rotation Velocity [°/sec]")]
        [Range(5.0f, 90.0f)]
        public float rotVel = 20.0f;

        [Tooltip("Scale Input Factor [%/sec]")]
        [Range(0.1f, 10.0f)]
        public float scaleInputFactor = 1.0f;

        [Tooltip("Enable/Disable pitch rotation")]
        public bool pitchEnabled;

        [Tooltip("Enable/Disable vertical translation")]
        public bool verticalTransEnabled;

        private UnityEngine.XR.InputDevice rightXRInputDevice;

        private float savTime = 0.0f;
        private float scaleLevelStopDuration = 1.0f; // in sec

        private Groundfollowing groundfollowing;

        // Start is called before the first frame update
        protected override void Start()
        {
            base.Start();

            // overrides BC
            viewingSetup = GetComponent<ViewingSetupHMDAnatomy>();

            groundfollowing = GetComponent<Groundfollowing>();

            transVel = 5.0f;
            rotVel = 45.0f;
        }


        // Update is called once per frame
        void Update()
        {
            CheckForXRInputDevices(); // rework this

            Vector3 transInput = Vector3.zero;
            Vector3 rotInput = Vector3.zero;
            float scaleInput = 0.0f;
            GetNavigationInput(out transInput, out rotInput, out scaleInput);

            MapInput(transInput, rotInput, scaleInput);

            if (groundfollowing.enabled) groundfollowing.UpdateGroundfollowing();

            bool primary2DAxisClick = false;
            rightXRInputDevice.TryGetFeatureValue(CommonUsages.primary2DAxisClick, out primary2DAxisClick);

            if (primary2DAxisClick) base.ResetTransform();
        }


        private void GetNavigationInput(out Vector3 transInput, out Vector3 rotInput, out float scaleInput)
        {
            transInput = Vector3.zero;
            rotInput = Vector3.zero;
            scaleInput = 0.0f;

            if (rightXRInputDevice.characteristics.ToString() != "None") // guard
            {
                // get translation input
                float trigger = 0.0f;
                rightXRInputDevice.TryGetFeatureValue(CommonUsages.trigger, out trigger);
                //Debug.Log("index finger rocker: " + trigger);
                transInput.z = trigger;

                // sprint option
                /*
                bool gripButton = false;
                rightXRInputDevice.TryGetFeatureValue(CommonUsages.gripButton, out gripButton);
                if (gripButton) transInput *= 2.0f; // huge inpact due to exponential transfer-function later
                */

                // get rotation input
                Vector2 primary2DAxis = Vector2.zero;
                rightXRInputDevice.TryGetFeatureValue(CommonUsages.primary2DAxis, out primary2DAxis);
                rotInput.y = primary2DAxis.x;
                rotInput.x = primary2DAxis.y;

                // get scale input
                bool primaryButton = false;
                rightXRInputDevice.TryGetFeatureValue(CommonUsages.primaryButton, out primaryButton);
                if (primaryButton) scaleInput += 1.0f;

                bool secondaryButton = false;
                rightXRInputDevice.TryGetFeatureValue(CommonUsages.secondaryButton, out secondaryButton);
                if (secondaryButton) scaleInput -= 1.0f;

            }
        }

        private void MapInput(Vector3 transInput, Vector3 rotInput, float scaleInput)
        {
            // map scale input
            if (scaleInput != 0.0f)
            {
                if ((Time.time - savTime) > scaleLevelStopDuration) // not in the 1:1 scale level stop duration phase
                {
                    scaleInput = 1.0f + scaleInput * scaleInputFactor * Time.deltaTime; // transfer function
                    float newScale = transform.localScale.x * scaleInput;
                    newScale = Mathf.Clamp(newScale, 0.1f, 10.0f);
                    //Debug.Log(newScale);

                    if ((transform.localScale.x > 1.0f && newScale < 1.0f) || (transform.localScale.x < 1.0f && newScale > 1.0f)) // passing 1:1 scale level
                    {
                        newScale = 1.0f; // snap exactely to 1:1 scale
                        savTime = Time.time;
                    }

                    transform.localScale = new Vector3(newScale, newScale, newScale); // apply new scale

                    viewingSetup.mainCamera.GetComponent<Camera>().nearClipPlane = 0.2f * newScale; // adjust near plane to scale level (should be consistent in user space, e.g. 20cm)
                }
            }

            // map translation input
            if (transInput.magnitude > 0.0f)
            {
                // forward movement in pointing direction
                Vector3 moveVec = viewingSetupHMD.rightController.transform.TransformDirection(Vector3.forward);

                if (verticalTransEnabled == false)
                {
                    moveVec.y = 0.0f; // restrict input to planar movement           
                    moveVec.Normalize();
                }

                moveVec = moveVec * Mathf.Pow(transInput.z, 3) * transVel * Time.deltaTime; // exponential transfer function
                moveVec *= transform.localScale.x; // translation velocity adjusted to scale level (should be consistent in user space)

                transform.Translate(moveVec, Space.World);
            }

            // map rotation input
            if (rotInput.magnitude > 0.0f)
            {
                // head rotation
                float RY = Mathf.Pow(rotInput.y, 3) * Time.deltaTime * rotVel; // exponential transfer function
                                                                               //transform.Rotate(0.0f, RY, 0.0f, Space.Self); // rotate around XR-Rig center
                transform.RotateAround(viewingSetup.mainCamera.transform.position, Vector3.up, RY); // rotate arround camera position (somewhere on platform)

                // pitch rotation
                if (pitchEnabled == true)
                {
                    float RX = Mathf.Pow(rotInput.x, 3) * Time.deltaTime * rotVel; // exponential transfer function
                                                                                   //transform.Rotate(RX, 0.0f, 0.0f, Space.Self); // rotate around XR-Rig center
                    Matrix4x4 rotMat = Matrix4x4.Rotate(Quaternion.Euler(0.0f, viewingSetup.mainCamera.transform.rotation.eulerAngles.y, 0.0f)); // only take "HEAD" rotation from users head transform
                    Vector3 rightGlobal = rotMat * Vector3.right; // define pitch axis in userspace "ONLY HEAD" orientation
                    transform.RotateAround(viewingSetup.mainCamera.transform.position, rightGlobal, RX); // rotate arround camera position (somewhere on platform)
                }

                // no roll rotation so far
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
}
