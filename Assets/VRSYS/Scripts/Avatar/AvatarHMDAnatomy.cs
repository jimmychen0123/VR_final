using UnityEngine;
using UnityEngine.XR;
using UnityEngine.XR.Interaction.Toolkit;

namespace Vrsys 
{
    public class AvatarHMDAnatomy : AvatarAnatomy
    {
        public GameObject body;
        public GameObject handLeft;
        public GameObject handRight;

        protected override void ParseComponents()
        {
            base.ParseComponents();

            if (body == null)
            {
                body = transform.Find("Head/Body").gameObject;
            }
            if (handLeft == null)
            {
                handLeft = transform.Find("HandLeft").gameObject;
                //handLeft.AddComponent<XRController>();
            }
            if (handRight == null)
            {
                handRight = transform.Find("HandRight").gameObject;
                //handRight.AddComponent<XRController>();
            }
        }

        public override void ConnectTransformFrom(ViewingSetupAnatomy viewingSetup)
        {
            base.ConnectTransformFrom(viewingSetup);
            if (viewingSetup is ViewingSetupHMDAnatomy viewingSetupHMD)
            {
                TransformConnection.CreateOrUpdate(viewingSetupHMD.leftController, handLeft);
                TransformConnection.CreateOrUpdate(viewingSetupHMD.rightController, handRight);
            }
        }
    }
}
