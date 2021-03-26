using Photon.Pun;
using TMPro;
using UnityEngine;

namespace Vrsys
{
    [RequireComponent(typeof(AvatarAnatomy))]
    public class NetworkAvatar : MonoBehaviourPunCallbacks
    {
        private AvatarAnatomy anatomy;

        private void Awake()
        {
            anatomy = GetComponent<AvatarAnatomy>();

            if (PhotonNetwork.IsConnected)
            {
                GetComponentInChildren<TMP_Text>().text = photonView.Owner.NickName;
            }

            if (photonView.IsMine)
            {
                NetworkUser.localUserInstance = gameObject;
                NetworkUser.localHead = anatomy.head;
            }
        }
    }
}