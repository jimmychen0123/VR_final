using UnityEngine;

namespace Vrsys
{
    public class TransformConnection : MonoBehaviour
    {
        public Transform input;

        void Update()
        {
            if (input != null)
            {
                CopyTransform(input.transform, transform);
            }
        }

        public static void CopyTransform(Transform input, Transform target)
        {
            target.position = input.position;
            target.rotation = input.rotation;
            target.localScale = input.localScale;
        }

        public static void CreateOrUpdate(GameObject input, GameObject target)
        {
            var connection = target.GetComponent<TransformConnection>();
            if (connection == null)
            {
                connection = target.AddComponent<TransformConnection>();
            }
            connection.input = input.transform;
        }
    }
}
