using Photon.Pun;
using System.Collections.Generic;
using UnityEngine;

namespace Vrsys
{
    public class LocalShadowCastingUpdate : MonoBehaviourPunCallbacks
    {
        [SerializeField]
        protected List<GameObject> meshRendererGameObjects;

        [SerializeField]
        protected UnityEngine.Rendering.ShadowCastingMode localShadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.ShadowsOnly;

        void Start()
        {
            if (photonView.IsMine || !PhotonNetwork.IsConnected)
            {
                foreach (var go in meshRendererGameObjects)
                {
                    var renderer = go.GetComponent<MeshRenderer>();
                    if (renderer != null)
                    {
                        renderer.shadowCastingMode = localShadowCastingMode;
                    }
                }
            }
        }
    }
}