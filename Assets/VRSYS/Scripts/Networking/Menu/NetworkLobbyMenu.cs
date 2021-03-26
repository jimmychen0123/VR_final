using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;
using UnityEngine.UI;
using TMPro;


namespace Vrsys
{
    public class NetworkLobbyMenu : MonoBehaviour
    {
        [Header("Required Settings")]
        public NetworkLobbyLogic networkLobbyLogic;

        public List<string> supportedDeviceNames = new List<string> { "Desktop", "HMD" };
        public List<GameObject> availableViewingSetups = new List<GameObject>();
        public List<GameObject> availableAvatars = new List<GameObject>();

        [Header("Set Custom UI Elements (Optional)")]
        public TMP_InputField userNameInput;
        public TMP_InputField addRoomInput;
        public ToggleGroup colorToggleGroup;
        public TMP_Dropdown avatarDrop;
        public TMP_Dropdown deviceDrop;
        public TMP_Dropdown roomDrop;
        public Toggle networkSwitch;
        public TMP_Text statusText;

        public Button singleStartButton;
        public Button createJoinButton;
        public Button addRoomButton;

        public GameObject singleUserStartPanel;
        public GameObject multiUserStartPanel;

        private bool networkEnabled = true;
        private List<string> availableDevices = new List<string>();
        private Dictionary<string, GameObject> viewingSetups = new Dictionary<string, GameObject>();
        private Dictionary<string, GameObject> avatars = new Dictionary<string, GameObject>();
        private List<string> rooms;

        private NetworkLobbyLogic.ConnectionState connectionStatus;
        private Color notConnectedColor;
        private Color connectedColor;


        private void Awake()
        {
            if (networkLobbyLogic == null)
            {
                networkLobbyLogic = GameObject.Find("NetworkLobbyLogic").GetComponent<NetworkLobbyLogic>();
            }

            if (userNameInput == null)
            {
                userNameInput = GameObject.Find("UserNameInput").GetComponent<TMP_InputField>();
            }

            if (addRoomInput == null)
            {
                addRoomInput = GameObject.Find("AddRoomInput").GetComponent<TMP_InputField>();
            }

            if (colorToggleGroup == null)
            {
                colorToggleGroup = GameObject.Find("UserColorsPanel").GetComponent<ToggleGroup>();
            }

            if (avatarDrop == null)
            {
                avatarDrop = GameObject.Find("UserAvatarDrop").GetComponent<TMP_Dropdown>();
            }

            if (deviceDrop == null)
            {
                deviceDrop = GameObject.Find("DeviceDrop").GetComponent<TMP_Dropdown>();
            }

            if (roomDrop == null)
            {
                roomDrop = GameObject.Find("RoomDrop").GetComponent<TMP_Dropdown>();
            }

            if (networkSwitch == null)
            {
                networkSwitch = GameObject.Find("NetworkToggle").GetComponent<Toggle>();
            }

            if (statusText == null)
            {
                statusText = GameObject.Find("StatusText").GetComponent<TMP_Text>();
            }

            if (singleStartButton == null)
            {
                singleStartButton = GameObject.Find("SingleStartButton").GetComponent<Button>();
            }

            if (createJoinButton == null)
            {
                createJoinButton = GameObject.Find("CreateJoinButton").GetComponent<Button>();
            }

            if (addRoomButton == null)
            {
                addRoomButton = GameObject.Find("AddRoomButton").GetComponent<Button>();
            }

            if (singleUserStartPanel == null)
            {
                singleUserStartPanel = GameObject.Find("SingleUserPanel");
            }

            if (multiUserStartPanel == null)
            {
                multiUserStartPanel = GameObject.Find("MultiUserPanel");
            }

            // Setup Listeners
            singleStartButton.onClick.AddListener(delegate { StartSingleApplication(); });
            createJoinButton.onClick.AddListener(delegate { CreateOrJoinRoom(); });
            addRoomButton.onClick.AddListener(delegate { AddRoom(); });
            networkSwitch.onValueChanged.AddListener(delegate { ToggleNetwork(); });

            networkSwitch.enabled = networkLobbyLogic.networkEnabled;
            if (networkSwitch.isOn)
            {
                networkEnabled = true;
                singleUserStartPanel.SetActive(false);
            }
            else
            {
                networkEnabled = false;
                multiUserStartPanel.SetActive(false);
            }

            rooms = new List<string> { "Gropius Room", "Schiller Room", "Wieland Room" };
            notConnectedColor = new Color32(156, 0, 0, 255);
            connectedColor = new Color32(49, 255, 57, 255);
        }

        // Start is called before the first frame update
        void Start()
        {
            UpdateXRDeviceDropdown();
            UpdateRooms();
        }

        // Update is called once per frame
        void Update()
        {

        }

        private void OnEnable()
        {
            networkLobbyLogic.OnUpdatedRooms += UpdateRooms;
            networkLobbyLogic.OnConnectionStatusChanged += UpdateConnectionStatusText;
        }

        private void OnDisable()
        {
            networkLobbyLogic.OnUpdatedRooms -= UpdateRooms;
            networkLobbyLogic.OnConnectionStatusChanged -= UpdateConnectionStatusText;
        }

        private void UpdateXRDeviceDropdown()
        {
            if (SystemInfo.deviceType == DeviceType.Desktop)
            {
                availableDevices.Add("Desktop");
            }

            var xrDisplaySubsystems = new List<XRDisplaySubsystem>();
            SubsystemManager.GetInstances<XRDisplaySubsystem>(xrDisplaySubsystems);
            foreach (var xrDisplay in xrDisplaySubsystems)
            {
                if (xrDisplay.running)
                {
                    if (xrDisplay.SubsystemDescriptor.id.Equals("oculus display"))
                    {
                        availableDevices.Add("HMD");
                    }
                }
            }

            foreach (string device in availableDevices)
            {
                if (supportedDeviceNames.Contains(device))
                {
                    int deviceIdx = supportedDeviceNames.IndexOf(device);
                    try
                    {
                        viewingSetups.Add(supportedDeviceNames[deviceIdx], availableViewingSetups[deviceIdx]);
                        avatars.Add(supportedDeviceNames[deviceIdx], availableAvatars[deviceIdx]);
                    }
                    catch (Exception e)
                    {
                        throw e;
                    }
                }
            }

            deviceDrop.ClearOptions();
            deviceDrop.AddOptions(viewingSetups.Keys.ToList());
            deviceDrop.value = viewingSetups.Count;
        }


        public void ToggleNetwork()
        {
            networkEnabled = !networkEnabled;
            if (networkEnabled)
            {
                singleUserStartPanel.SetActive(false);
                multiUserStartPanel.SetActive(true);
                networkLobbyLogic.EnableNetworking();
            }
            else
            {
                singleUserStartPanel.SetActive(true);
                multiUserStartPanel.SetActive(false);
                networkLobbyLogic.DisableNetworking();
            }
        }



        public void AddRoom()
        {
            if (addRoomInput.text != "")
            {
                string newRoomName = addRoomInput.text;
                if (!rooms.Contains(newRoomName)) rooms.Add(newRoomName);

                addRoomInput.text = "";
            }
            UpdateRooms();
            roomDrop.value = rooms.Count;
        }

        public void UpdateConnectionStatusText()
        {
            connectionStatus = networkLobbyLogic.GetConnectionStatus();
            statusText.text = ConnectionStateToString(connectionStatus);
            if (connectionStatus == NetworkLobbyLogic.ConnectionState.Disabled || connectionStatus == NetworkLobbyLogic.ConnectionState.JoinedLobby)
            {
                createJoinButton.interactable = true;
                networkSwitch.interactable = true;
            }
            else
            {
                createJoinButton.interactable = false;
                networkSwitch.interactable = false;
            }

            if (networkLobbyLogic.IsNetworkEnabled())
            {
                statusText.color = connectedColor;
            }
            else
            {
                statusText.color = notConnectedColor;
            }
        }

        public void UpdateRooms()
        {
            roomDrop.ClearOptions();
            Dictionary<string, int> usersPerRoom = networkLobbyLogic.GetUsersPerRoom();
            int maxUsersPerRoom = networkLobbyLogic.maxUsers;

            // Add available online rooms to the default rooms
            foreach (KeyValuePair<string, int> room in usersPerRoom)
            {
                if (!rooms.Contains(room.Key)) rooms.Add(room.Key);
            }

            // Create room options from rooms list and add the user numbers
            List<string> roomDropOptions = new List<string>();
            foreach (string room in rooms)
            {
                if (!usersPerRoom.ContainsKey(room))
                {
                    roomDropOptions.Add(room + " (0/" + maxUsersPerRoom.ToString() + ")");

                }
                else
                {
                    roomDropOptions.Add(room + " (" + usersPerRoom[room].ToString() + "/" + maxUsersPerRoom.ToString() + ")");
                }
            }

            // Add the options created in the List above
            roomDrop.AddOptions(roomDropOptions);
        }

        private void PrepareStart()
        {

            // Assign default user name 
            if (userNameInput.text == "")
            {
                networkLobbyLogic.userName = "DefaultUser" + UnityEngine.Random.Range(0, 10000).ToString();
            }
            else
            {
                networkLobbyLogic.userName = userNameInput.text;
            }

            // Set settings
            networkLobbyLogic.roomName = FormatRoomName(roomDrop.options[roomDrop.value].text);
            networkLobbyLogic.userViewingSetup = viewingSetups[deviceDrop.options[deviceDrop.value].text];
            networkLobbyLogic.userAvatar = avatars[deviceDrop.options[deviceDrop.value].text];
            IEnumerator<Toggle> toggleEnum = colorToggleGroup.ActiveToggles().GetEnumerator();
            toggleEnum.MoveNext();
            PlayerPrefs.SetString("UserColor", toggleEnum.Current.name);

            //PlayerPrefs.SetString("UserDevice", deviceDrop.options[deviceDrop.value].text);
            PlayerPrefs.SetString("UserAvatar", avatarDrop.options[avatarDrop.value].text);
        }

        private string FormatRoomName(string originalRoomName)
        {
            List<string> roomNameParts = originalRoomName.Split(new string[] { "(" }, StringSplitOptions.RemoveEmptyEntries).ToList();
            roomNameParts.RemoveAt(roomNameParts.Count - 1);
            string formattedRoomName = string.Join("(", roomNameParts);
            formattedRoomName = formattedRoomName.Remove(formattedRoomName.Length - 1);
            //formattedRoomName.Remove(formattedRoomName.Length - 1);
            return formattedRoomName;
        }

        public string ConnectionStateToString(NetworkLobbyLogic.ConnectionState state)
        {
            return state.ToString();
        }

        public void StartSingleApplication()
        {
            PrepareStart();
            networkLobbyLogic.JoinOrCreateRoom();
        }

        public void CreateOrJoinRoom()
        {
            PrepareStart();
            networkLobbyLogic.JoinOrCreateRoom();
        }

    }
}
