using UnityEngine;

namespace Vrsys 
{
    public class AvatarAnatomy : MonoBehaviour
    {
        public GameObject head;
        public GameObject nameTag;

        private void Awake()
        {
            ParseComponents();
        }

        protected virtual void ParseComponents()
        {
            if (head == null)
            {
                head = transform.Find("Head").gameObject;
            }
            if (nameTag == null)
            {
                nameTag = transform.Find("Head/NameTag").gameObject;
            }
        }

        public virtual void ConnectTransformFrom(ViewingSetupAnatomy viewingSetup)
        {
            transform.SetParent(viewingSetup.childAttachmentRoot.transform);
            TransformConnection.CreateOrUpdate(viewingSetup.mainCamera, head);
        }
    }
}
