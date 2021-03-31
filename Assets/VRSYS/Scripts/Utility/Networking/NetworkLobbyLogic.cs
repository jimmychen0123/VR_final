using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using TMPro;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine.UI;
using UnityEditor;
using UnityEngine.SceneManagement;

namespace Vrsys 
{
    public class NetworkLobbyLogic : MonoBehaviourPunCallbacks
    {
        [Header("Photon App Settings")]
        [Tooltip("Configure version and update rate.")]
        public string appVersion = "1";

        public bool autoSyncScene = true;
        public int networkSendRate = 20;
        public int networkSerializationRate = 15;
        public byte maxUsers = 10;

        [Header("User Settings")]
        [Tooltip("Configure user name, color and avatar.")]
        public string userName = "";
        public GameObject userViewingSetup;
        public GameObject userAvatar;
        private string selectedDeviceName = "";
        private string selectedDevicePath = "";
        private string userAvatarName = "";
        private string userAvatarPath = "";

        [Header("Start Settings")]
        public bool autoStart = true;
        public bool networkEnabled = true;
        public string roomName = "";


        [Header("Optional Settings")]
        public string startScene = "";
        public bool verbose = false;

        private Dictionary<string, int> usersPerRoom;
        private ConnectionState connectionStatus = ConnectionState.Disconnected;
        private bool createdRoom = false;
        private string logTag = "";

        // Event that is called when the room list was updated
        public event UpdatedRoomsEvent OnUpdatedRooms;
        public delegate void UpdatedRoomsEvent();

        public event ConnectionStatusChangedEvent OnConnectionStatusChanged;
        public delegate void ConnectionStatusChangedEvent();

        public enum ConnectionState
        {
            Disabled,
            Disconnected,
            Disconnecting,
            JoiningLobby,
            JoinedLobby,
            Connecting,
            Connected
        }



        void Awake()
        {
            logTag = GetType().Name;

            // Connect to server and setup photon auto-sync, send rate etc.
            Connect();

            usersPerRoom = new Dictionary<string, int>();
        }

        void Start()
        {
            if (autoStart)
            {
                if (userViewingSetup == null)
                {
                    //Debug.LogError(logTag + ": Error - user viewing setup not set.");
                }
            }

        }

        public override void OnConnectedToMaster()
        {
            if (verbose)
            {
                Debug.Log(logTag + ": Connected to master!");
            }

            if (networkEnabled)
            {
                PhotonNetwork.JoinLobby();
                SetConnectionStatus(ConnectionState.JoiningLobby);
            }
            base.OnConnectedToMaster();
        }

        public override void OnConnected()
        {
            SetConnectionStatus(ConnectionState.Connected);
            base.OnConnected();
        }

        public override void OnJoinedLobby()
        {
            SetConnectionStatus(ConnectionState.JoinedLobby);
            if (autoStart)
            {
                JoinOrCreateRoom();
            }
        }

        public override void OnLeftLobby()
        {
            // If network is not enabled disconnect from photon
            if (!networkEnabled && PhotonNetwork.IsConnected)
            {
                PhotonNetwork.Disconnect();
            }
            SetConnectionStatus(ConnectionState.Disconnecting);
        }

        public override void OnDisconnected(DisconnectCause cause)
        {
            if (cause == DisconnectCause.DisconnectByClientLogic)
            {
                PhotonNetwork.OfflineMode = true;
                SetConnectionStatus(ConnectionState.Disabled);
            }
            base.OnDisconnected(cause);
        }

        public override void OnCreatedRoom()
        {
            if (verbose)
            {
                Debug.Log(logTag + ": Room created");
            }

            createdRoom = true;
            LoadStartScene();
        }

        public override void OnJoinedRoom()
        {
            if (verbose)
            {
                Debug.Log(logTag + ": Room joined");
            }

            if (!createdRoom && !autoSyncScene)
            {
                LoadStartScene();
            }
        }

        public override void OnRoomListUpdate(List<RoomInfo> roomList)
        {
            usersPerRoom = new Dictionary<string, int>();
            foreach (RoomInfo info in roomList)
            {
                usersPerRoom.Add(info.Name, info.PlayerCount);
            }

            // Call event that rooms updated
            if (OnUpdatedRooms != null)
            {
                OnUpdatedRooms();
            }
        }

        private void Connect()
        {
            PhotonNetwork.AutomaticallySyncScene = autoSyncScene;
            if (!PhotonNetwork.IsConnected)
            {
                // Connect to Photon Master Server
                PhotonNetwork.ConnectUsingSettings();
                PhotonNetwork.GameVersion = appVersion;
                SetConnectionStatus(ConnectionState.Connecting);
            }
            PhotonNetwork.SendRate = networkSendRate;
            PhotonNetwork.SerializationRate = networkSerializationRate;
        }

        private void SetConnectionStatus(ConnectionState status)
        {
            connectionStatus = status;
            if (OnConnectionStatusChanged != null)
            {
                OnConnectionStatusChanged();
            }
        }

        public void EnableNetworking()
        {
            if (connectionStatus == ConnectionState.Disconnected || connectionStatus == ConnectionState.Disabled)
            {
                networkEnabled = true;
                PhotonNetwork.OfflineMode = false;
                Connect();
            }
        }

        public void DisableNetworking()
        {
            if (connectionStatus == ConnectionState.Connected || connectionStatus == ConnectionState.JoinedLobby)
            {
                networkEnabled = false;
                if (PhotonNetwork.InLobby)
                {
                    PhotonNetwork.LeaveLobby(); // leave lobby before disconnect can be called
                }
                else if (PhotonNetwork.IsConnected)
                {
                    PhotonNetwork.Disconnect();
                }
            }
        }

        public void Disconnect()
        {
            PhotonNetwork.Disconnect();
        }

        public bool IsNetworkEnabled()
        {
            return networkEnabled;
        }

        public bool IsConnected()
        {
            return PhotonNetwork.IsConnected;
        }
        public ConnectionState GetConnectionStatus()
        {
            return connectionStatus;
        }

        public Dictionary<string, int> GetUsersPerRoom()
        {
            return usersPerRoom;
        }

        public void JoinOrCreateRoom()
        {

            if (userName == "")
            {
                userName = "DefaultUser" + UnityEngine.Random.Range(0, 10000).ToString();
            }

            if (roomName == "")
            {
                roomName = "Room" + UnityEngine.Random.Range(0, 10000).ToString();
            }

            PhotonNetwork.NickName = userName;
            PlayerPrefs.SetString("UserName", userName);
            PlayerPrefs.SetString("RoomName", roomName);

            if (userViewingSetup != null)
            {
                selectedDevicePath = "ViewingSetups";
                selectedDeviceName = userViewingSetup.name;
            }
            else
            {
                //Debug.LogError(logTag + ": Error - user viewing setup not set.");
            }

            PlayerPrefs.SetString("ViewingSetupName", selectedDeviceName);
            PlayerPrefs.SetString("ViewingSetupPath", selectedDevicePath);

            if (userAvatar != null)
            {
                userAvatarPath = "Avatars";
                userAvatarName = userAvatar.name;
            }
            else
            {
                //Debug.LogError(logTag + ": Error - user avatar not set.");
            }

            PlayerPrefs.SetString("AvatarName", userAvatarName);
            PlayerPrefs.SetString("AvatarPath", userAvatarPath);

            if (verbose)
            {
                Debug.Log(logTag + ": Joining Room '" + roomName + "' as User '" + userName + "':");
                Debug.Log(logTag + ": Device '" + selectedDeviceName);
            }

            PhotonNetwork.JoinOrCreateRoom(roomName, new RoomOptions { MaxPlayers = maxUsers }, TypedLobby.Default);
        }

        private void LoadStartScene()
        {
            if (startScene.Length > 0)
            {
                PhotonNetwork.LoadLevel(startScene);
                if (verbose)
                {
                    Debug.Log(logTag + ": Loading Scene '" + startScene + "'");
                }
            }
            else
            {
                PhotonNetwork.LoadLevel(1);
                if (verbose)
                {
                    Debug.Log(logTag + ": Loading Scene '" + SceneManager.GetSceneAt(1).name + "'");
                }
            }
        }
    }
}

