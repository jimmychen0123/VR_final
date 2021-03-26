using UnityEngine;

namespace Vrsys
{
    public class AvatarDesktopAnatomy : AvatarAnatomy
    {
        public GameObject body;

        protected override void ParseComponents()
        {
            base.ParseComponents();
        
            if (body == null)
            {
                body = transform.Find("Head/Body").gameObject;
            }
        }
    }
}
