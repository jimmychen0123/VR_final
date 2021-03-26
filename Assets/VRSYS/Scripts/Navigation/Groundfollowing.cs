using UnityEngine;

namespace Vrsys
{
    [RequireComponent(typeof(ViewingSetupAnatomy))]
    public class Groundfollowing : MonoBehaviour
    {
        private float rayStartHeight = 1.0f; // defines height from which ray is shot downwards (required to climb upstairs)
        private float targetHeight = 0.0f; // targeted height level above ground (required in Desktop viewing setups)
        private bool fallingFlag;
        private float fallStartTime;
        private RaycastHit rayHit;
        [Tooltip("LayerMask for Groundfollowing")]
        public LayerMask layerMask = -1; // -1 everything

        private ViewingSetupAnatomy viewingSetup;

        private void Start()
        {
            viewingSetup = GetComponent<ViewingSetupAnatomy>();
        }

        public void setTargetHeight(float height)
        {
            targetHeight = height;
        }

        private void OnDisable()
        {
            fallingFlag = false;
        }

        public void UpdateGroundfollowing()
        {
            Vector3 startPos = Vector3.zero;
            if (transform == viewingSetup.mainCamera.transform) // navigation node is camera node 
            {
                startPos = transform.position + new Vector3(0.0f, rayStartHeight * transform.localScale.x, 0.0f);
            }
            else
            {
                //startPos = transform.position + transform.TransformDirection(new Vector3(0.0f, startHeight, 0.0f)); // platform center
                startPos = transform.position + transform.TransformDirection(new Vector3(viewingSetup.mainCamera.transform.localPosition.x, rayStartHeight * transform.localScale.x, viewingSetup.mainCamera.transform.localPosition.z)); // user pos
            }


            if (Physics.Raycast(startPos, Vector3.down, out rayHit, Mathf.Infinity, layerMask))
            {
                float heightOffset = rayHit.distance - rayStartHeight * transform.localScale.x - targetHeight * transform.localScale.x;
                //Debug.Log("GF Hit: " + rayHit.distance  + " " + heightOffset + " " + rayHit.point);

                if (heightOffset > 1.0f * transform.localScale.x) // falling
                {
                    if (fallingFlag == false) // start falling
                    {
                        fallingFlag = true;
                        fallStartTime = Time.time;
                    }

                    float fallTime = Time.time - fallStartTime;
                    Vector3 fallVec = Vector3.down * Mathf.Min(9.81f / 2.0f * Mathf.Pow(fallTime, 2.0f), 100.0f); // Weg-Zeit Gesetz
                                                                                                                  //transform.Translate(fallVec * Time.deltaTime); // correction in in navigation coordinate system (e.g. orientation)
                    transform.position += fallVec * Time.deltaTime;
                }
                else // near surface
                {
                    fallingFlag = false;

                    float verticalInput = Mathf.Pow(heightOffset * 0.5f, 2.0f) * -40.0f;
                    verticalInput = Mathf.Min(verticalInput, Mathf.Abs(heightOffset)); // clamp actual height correction to max height offset
                    if (heightOffset < 0.0f) verticalInput *= -1.0f;
                    //transform.Translate(new Vector3(0.0f, verticalInput * Time.deltaTime, 0.0f)); // correction in navigation coordinate system (e.g. orientation)
                    transform.position += new Vector3(0.0f, verticalInput * Time.deltaTime, 0.0f);
                }
            }
        }
    }
}
