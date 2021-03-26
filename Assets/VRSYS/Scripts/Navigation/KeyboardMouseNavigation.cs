using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Vrsys
{
    public class KeyboardMouseNavigation : NavigationBase
    {
        [Tooltip("Translation Velocity [m/sec]")]
        [Range(0.1f, 10.0f)]
        public float transVel = 3.0f;

        [Tooltip("Rotation Velocity [°/sec]")]
        [Range(5.0f, 90.0f)]
        public float rotVel = 25.0f;

        [Tooltip("Scale Input Factor [%/sec]")]
        [Range(0.1f, 10.0f)]
        public float scaleInputFactor = 5.0f;

        private GameObject pivotPointGO;

        private RaycastHit hit;
        private bool pivotFlag = false;

        private Groundfollowing groundfollowing;

        // Start is called before the first frame update
        protected override void Start()
        {
            base.Start();

            groundfollowing = GetComponent<Groundfollowing>();
            groundfollowing.setTargetHeight(1.0f); // navigation is corrected to 1m height above ground

            // geometry for pivot point visualization
            pivotPointGO = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            //rightRayIntersectionSphere.transform.parent = this.gameObject.transform;
            pivotPointGO.name = "Pivot Point";
            pivotPointGO.transform.localScale = new Vector3(0.08f, 0.08f, 0.08f);
            pivotPointGO.GetComponent<MeshRenderer>().material.color = Color.red;
            pivotPointGO.GetComponent<SphereCollider>().enabled = false; // disable for picking ?!
            pivotPointGO.SetActive(false); // hide
        }


        // Update is called once per frame
        void Update()
        {
            CalcPivotPoint();

            Vector3 transInput = CalcTranslationInput();
            Vector3 rotInput = CalcRotationInput();
            float scaleInput = CalcScaleInput();

            MapInput(transInput, rotInput, scaleInput);

            if (groundfollowing.enabled) groundfollowing.UpdateGroundfollowing();

            if (Input.GetKeyDown(KeyCode.Space)) base.ResetTransform();
        }

        private Vector3 CalcTranslationInput()
        {
            Vector3 transInput = Vector3.zero;

            // foward input
            if (Input.GetKey(KeyCode.W)) transInput.z += 1.0f;
            if (Input.GetKey(KeyCode.S)) transInput.z -= 1.0f;

            // sideward input (strafing)
            if (Input.GetKey(KeyCode.A)) transInput.x -= 1.0f;
            if (Input.GetKey(KeyCode.D)) transInput.x += 1.0f;

            // vertical input
            if (Input.GetKey(KeyCode.KeypadPlus)) transInput.y += 1.0f;
            if (Input.GetKey(KeyCode.KeypadMinus)) transInput.y -= 1.0f;

            // sprint option
            if (Input.GetKey(KeyCode.LeftShift)) transInput *= 3.0f;

            return transInput;
        }



        private Vector3 CalcRotationInput()
        {
            Vector3 rotInput = Vector3.zero; // euler angles

            if (Time.frameCount < 10) return rotInput;

            // roll rot input
            if (Input.GetKey(KeyCode.Q)) rotInput.z += 1.0f;
            if (Input.GetKey(KeyCode.E)) rotInput.z -= 1.0f;

            // head rot input
            rotInput.y += Input.GetAxis("Mouse X") * 10.0f;

            // pitch rot input
            rotInput.x -= Input.GetAxis("Mouse Y") * 10.0f;

            return rotInput;
        }

        private float CalcScaleInput()
        {
            float scaleInput = 0.0f;
            scaleInput += Input.mouseScrollDelta.y;

            return scaleInput;
        }

        private void MapInput(Vector3 transInput, Vector3 rotInput, float scaleInput)
        {
            if (pivotFlag == true) // maneuvering
            {
                // orbit rotation
                Vector3 orbitInput = transInput * rotVel * Time.deltaTime * -1.0f; // transfer function
                transform.RotateAround(hit.point, Vector3.up, orbitInput.x);

                // move closer to or away from pivot point
                Vector3 pivotPointDir = hit.point - transform.position;
                //pivotPointDir.Normalize();
                Vector3 pivotApproachInput = pivotPointDir.normalized * transInput.z * transVel * Time.deltaTime; // transfer function
                transform.Translate(pivotApproachInput, Space.World);

                // ensure minimal distance to pivot point
                float minDist = 0.5f;
                if (pivotPointDir.magnitude < minDist && transInput.z > 0.0f)
                {
                    transform.position = hit.point + pivotPointDir.normalized * minDist * -1.0f;
                }
            }
            else // flying 
            {
                // map scale input
                if (scaleInput != 0.0f)
                {
                    scaleInput = 1.0f + scaleInput * scaleInputFactor * Time.deltaTime; // transfer function
                    float scaleLevel = transform.localScale.x * scaleInput;
                    scaleLevel = Mathf.Clamp(scaleLevel, 0.1f, 10.0f);
                    //Debug.Log(scaleLevel);
                    transform.localScale = new Vector3(scaleLevel, scaleLevel, scaleLevel);

                    base.viewingSetup.mainCamera.GetComponent<Camera>().nearClipPlane = 0.2f * scaleLevel; // adjust near plane to scale level (should be consistent in user space, e.g. 20cm)
                }

                // map translation input
                if (transInput.magnitude > 0.0f)
                {
                    transInput *= transVel * Time.deltaTime; // transfer function
                    transInput *= transform.localScale.x; // translation velocity adjusted to scale level (should be consistent in user space)
                    transform.Translate(transInput);
                }

                // map rotation input
                if (rotInput.magnitude > 0.0f)
                {
                    rotInput *= rotVel * Time.deltaTime; // transfer function
                                                         //transform.Rotate(rotInput.x, rotInput.y, rotInput.z); // leads to unwanted roll
                    //transform.rotation = Quaternion.Euler(Mathf.Clamp(transform.rotation.eulerAngles.x + rotInput.x, -85.0f, 85.0f), transform.rotation.eulerAngles.y + rotInput.y, transform.rotation.eulerAngles.z + rotInput.z);
                    // clamp pitch to prevent turn over
                    float newPitch = transform.rotation.eulerAngles.x + rotInput.x;
                    if (transform.forward.y < 0.0f) newPitch = Mathf.Min(newPitch, 85.0f);
                    else if (transform.forward.y > 0.0f) newPitch = Mathf.Max(newPitch, 275.0f);
                    transform.rotation = Quaternion.Euler(newPitch, transform.rotation.eulerAngles.y + rotInput.y, transform.rotation.eulerAngles.z + rotInput.z);
                }
            }
        }


        private void CalcPivotPoint()
        {
            if (base.viewingSetup.mainCamera == null) // guard
            {
                Debug.Log("Runtime Error: no main camera found");
                return;
            }

            if (Input.GetMouseButton(0) == true) // hold
            {
                Ray ray = base.viewingSetup.mainCamera.GetComponent<Camera>().ScreenPointToRay(Input.mousePosition);
                if (Physics.Raycast(ray, out hit, Mathf.Infinity)) // what about layer masks?
                {
                    if (Input.GetMouseButtonDown(0) == true) // pressed
                    {
                        pivotPointGO.SetActive(true); // show
                        pivotFlag = true;
                    }

                    pivotPointGO.transform.position = hit.point; // update pivot position
                }
            }

            if (Input.GetMouseButtonUp(0) == true) // released
            {
                pivotPointGO.SetActive(false); // hide
                pivotFlag = false;
            }
        }

    }
}
