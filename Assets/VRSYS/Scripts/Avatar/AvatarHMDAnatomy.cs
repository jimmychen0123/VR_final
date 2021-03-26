using UnityEngine;

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
            }
            if (handRight == null)
            {
                handRight = transform.Find("HandRight").gameObject;
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
