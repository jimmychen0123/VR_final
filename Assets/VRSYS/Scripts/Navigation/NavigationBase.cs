using UnityEngine;

namespace Vrsys
{
    [RequireComponent(typeof(ViewingSetupAnatomy))]
    public abstract class NavigationBase : MonoBehaviour
    {

        protected Vector3 startPos = Vector3.zero;
        protected Quaternion startRot = Quaternion.identity;
        protected float startScale = 1.0f;

        protected ViewingSetupAnatomy viewingSetup;


        // Start is called before the first frame update
        protected virtual void Start()
        {
            startPos = transform.position;
            startRot = transform.rotation;
            startScale = transform.localScale.x;

            viewingSetup = GetComponent<ViewingSetupAnatomy>();
        }


        public void ResetTransform()
        {
            transform.position = startPos;
            transform.rotation = startRot;
            transform.localScale = new Vector3(startScale, startScale, startScale);

            viewingSetup.mainCamera.GetComponent<Camera>().nearClipPlane = 0.2f * startScale; // adjust near plane to scale level (should be consistent in user space, e.g. 20cm)

            Debug.Log("Reset Navigation");
        }

    }
}
