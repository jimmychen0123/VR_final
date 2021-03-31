using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using System;

namespace Vrsys
{
    public class NetworkLogic : MonoBehaviourPunCallbacks
    {
        private string logTag;

        public bool verbose = false;

        void Start()
        {
            logTag = GetType().Name;

            if (verbose)
            {
                Debug.Log(logTag + ": Device Name: " + PlayerPrefs.GetString("DeviceName") + " Device Path: " + PlayerPrefs.GetString("DevicePath"));
            }

            if (!NetworkUser.localUserInstance)
            {
                InstantiateUser();
            }

            if (PhotonNetwork.IsMasterClient)
            {
                // Since the master client already called OnJoinedRoom() in the launcher scene, we have to call it again here; 
                // other clients will directly trigger the OnJoinedRoom() callback in this class
                OnJoinedRoom();
            }
        }

        private void InstantiateUser()
        {
            // instantiate avatar and viewing setup GameObjects
            string viewSetupResPath = PlayerPrefs.GetString("ViewingSetupPath") + "/" + PlayerPrefs.GetString("ViewingSetupName");
            var viewingSetupInstance = Instantiate(Resources.Load(viewSetupResPath) as GameObject, transform.position, Quaternion.identity);

            string avatarResPath = PlayerPrefs.GetString("AvatarPath") + "/" + PlayerPrefs.GetString("AvatarName");
            var avatarInstance = PhotonNetwork.Instantiate(avatarResPath, new Vector3(0.0f, 0, 0.0f), Quaternion.identity, 0);

            // connect viewing setup with avatar component transformations
            var avatarAnatomy = avatarInstance.GetComponent<AvatarAnatomy>();
            var viewingSetupAnatomy = viewingSetupInstance.GetComponent<ViewingSetupAnatomy>();
            if (avatarAnatomy != null && viewingSetupAnatomy != null)
            {
                avatarAnatomy.nameTag.SetActive(false);
                avatarAnatomy.ConnectTransformFrom(viewingSetupAnatomy);
            }
        }

        public override void OnJoinedRoom()
        {
            string connectedRoomName = PhotonNetwork.CurrentRoom.Name;
            if (verbose)
            {
                Debug.Log(logTag + ": Successfully connected to room " + connectedRoomName + ". Have fun!");
                Debug.Log(logTag + ": There are " + (PhotonNetwork.CurrentRoom.PlayerCount - 1) + " other participants in this room.");
            }
        }

        public override void OnLeftRoom()
        {
            Application.Quit();
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#endif
        }

        public override void OnPlayerEnteredRoom(Player other)
        {
            if (verbose)
            {
                Debug.Log(logTag + ": " + other.NickName + " has entered the room.");
            }
        }

        public override void OnPlayerLeftRoom(Player other)
        {
            if (verbose)
            {
                Debug.Log(logTag + ": " + other.NickName + " has left the room.");
            }
        }
    }
}
