using UnityEngine;

namespace Vrsys
{
    public class YawTowardsLocalHead : MonoBehaviour
    {
        void Update()
        {
            if (NetworkUser.localHead)
            {
                transform.rotation = LookAtYawOnly(transform.position, NetworkUser.localHead.transform.position);
            }
        }

        public static Quaternion LookAtYawOnly(Vector3 fromPosition, Vector3 toPosition)
        {
            var lookDir = toPosition - fromPosition;
            lookDir.y = 0.0f;
            return Quaternion.LookRotation(lookDir);
        }
    }
}
