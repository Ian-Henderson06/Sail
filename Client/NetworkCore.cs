using System;
using System.Collections;
using System.Collections.Generic;

using Riptide;
using Riptide.Utils;

using INet.Util;

using UnityEngine;
using Logger = INet.Util.Logger;

using UnityEngine.XR.Interaction.Toolkit;

namespace INet
{
    /// <summary>
    /// Server specific implementation of a network core. 
    /// Handles all server based connection events.
    /// </summary>
    public class NetworkCore : MonoBehaviour, INetworkCore
    {
        //Properties
        public NetworkPlayer LocalPlayer { get { return _localPlayer; } }
        public Dictionary<int, NetworkObject> ClientObjects = new Dictionary<int, NetworkObject>();

        //Private serialized fields
        [SerializeField] private GameObject _remotePlayerPrefab;
        [SerializeField] private GameObject _localPlayerPrefab;

        //Private fields
        private Client _client;
        private bool _hasStarted;
        private NetworkPlayer _localPlayer;


        //Properties
        public Client Client { get { return _client; } }

        public Peer GetPeer() => _client;

        public bool IsServer() => false;

        private void OnDestroy()
        {
            NetworkManager.Instance.OnTick -= OnTick;

            _client.Connected -= DidConnect;
        }

        public void InitialiseCore()
        {
            Logger.Log("Initialising Client Core.");
            _hasStarted = false;
            _client = new Client();

            _client.Connected += DidConnect;
            _client.ConnectionFailed += FailedToConnect;

            NetworkManager.Instance.OnTick += OnTick;

        }

        /// <summary>
        /// Try to connect the client to a server.
        /// </summary>
        /// <param name="address">IP and port of the server to connect to.</param>
        /// <param name="connectionAttempts">Maximum number of attempts to connect.</param>
        public void AttemptConnection(string address, int connectionAttempts = 5)
        {
            _hasStarted = true;
            Logger.Log("Starting client.");
            Logger.Log($"Connecting to {address}...");
            _client.Connect(address, connectionAttempts);


        }

        /// <summary>
        /// Spawn a player on the client at a specific position and rotation.
        /// </summary>
        /// <param name="username"></param>
        /// <param name="playerID"></param>
        public NetworkPlayer SpawnPlayer(string username, ushort playerID, int networkID, Vector3 position, Quaternion rotation)
        {
            Logger.Log($"Spawning {username}.");

            GameObject player;

            //Adjust prefab spawned based on if player is local or remote.
            if (playerID == _client.Id)
                player = Instantiate(_localPlayerPrefab, position, rotation);
            else
                player = Instantiate(_remotePlayerPrefab, position, rotation);

            NetworkPlayer playerNetwork = player.GetComponent<NetworkPlayer>();
            NetworkObject networkItem = player.GetComponent<NetworkObject>();

            if (playerNetwork == null)
            {
                Logger.LogError($"Could not fetch NetworkPlayer script from player prefab.");
                return null;
            }

            if (networkItem == null)
            {
                Logger.LogError($"Could not fetch NetworkObject script from player prefab.");
                return null;
            }

            playerNetwork.InitialisePlayer(username, playerID, networkID);
            networkItem.InitialiseObject(networkID, -1); //player has item id of -1
            NetworkManager.Instance.AddPlayer(playerNetwork);
            NetworkManager.Instance.AddNetworkObject(networkItem);

            //If local player
            if (playerID == _client.Id)
            {
                _localPlayer = playerNetwork;
            }
            return playerNetwork;
        }

        /// <summary>
        /// Destroy a player on the server.
        /// </summary>
        /// <param name="username"></param>
        /// <param name="playerID"></param>
        public void DestroyPlayer(ushort playerID)
        {
            NetworkPlayer playerNetwork;
            if (NetworkManager.Instance.Players.TryGetValue(playerID, out playerNetwork))
            {
                NetworkObject networkItem = playerNetwork.gameObject.GetComponent<NetworkObject>();
                NetworkManager.Instance.RemoveNetworkObject(networkItem);
                NetworkManager.Instance.RemovePlayer(playerNetwork);
                Destroy(playerNetwork.gameObject);
            }
            else
            {
                Logger.Log($"Could not destroy player {playerID}.");
            }
        }

        /// <summary>
        /// Spawn a networked object on the client.
        /// </summary>
        /// <param name="username"></param>
        /// <param name="playerID"></param>
        public NetworkObject SpawnNetworkObject(int networkID, int itemID, Vector3 position, Quaternion rotation)
        {
            Logger.Log($"Spawning item {itemID}.");
            GameObject item = Instantiate(NetworkObjectList.Instance.FindObject(itemID).Prefab, position, rotation);
            NetworkObject networkItem = item.GetComponent<NetworkObject>();

            if (networkItem == null)
            {
                Logger.LogError($"Could not fetch NetworkObject script from prefab.");
                return null;
            }

            Rigidbody rigidbody = networkItem.gameObject.GetComponent<Rigidbody>();
            if (rigidbody != null)
            {
                rigidbody.isKinematic = true;
            }

            AssignInteractableHandlers(networkItem); //Assigns parent handlers

            networkItem.InitialiseObject(networkID, itemID);
            NetworkManager.Instance.AddNetworkObject(networkItem);

            return networkItem;
        }

        /// <summary>
        /// Destroy a networked object on the server.
        /// </summary>
        /// <param name="username"></param>
        /// <param name="playerID"></param>
        public void DestroyNetworkObject(int networkID)
        {
            NetworkObject networkObject;
            if (NetworkManager.Instance.NetworkedObjects.TryGetValue(networkID, out networkObject))
            {
                //If has sub objects then remove them
                if (NetworkManager.Instance.NetworkedObjects[networkID].SubObjects.Length > 0)
                {
                    foreach (SubNetworkObject child in NetworkManager.Instance.NetworkedObjects[networkID].SubObjects)
                    {
                        if (ClientObjects.ContainsKey(child.NetworkID)) ClientObjects.Remove(child.NetworkID); //If client has control over entity then remove it from that list

                        NetworkManager.Instance.RemoveNetworkObject(child);
                    }
                }

                if (ClientObjects.ContainsKey(networkObject.NetworkID)) ClientObjects.Remove(networkObject.NetworkID); //If client has control over entity then remove it from that list
                NetworkManager.Instance.RemoveNetworkObject(networkObject);
                Destroy(networkObject.gameObject);
            }
            else
            {
                Logger.Log($"Could not destroy network object {networkID} as its not spawned on this client.");
            }
        }

        /// <summary>
        /// Assign a sub object its id and information.
        /// </summary>
        /// <param name="parentNetworkID"></param>
        /// <param name="listIndex"></param>
        /// <param name="childNetworkID"></param>
        public void AssignSubObject(int parentNetworkID, int listIndex, int childNetworkID)
        {
            Debug.Log($"Finding sub object {listIndex} of parent {parentNetworkID}");
            NetworkManager.Instance.NetworkedObjects[parentNetworkID].SubObjects[listIndex].InitialiseObject(childNetworkID, -1);
            NetworkManager.Instance.NetworkedObjects[parentNetworkID].SubObjects[listIndex].InitializeSubObject(parentNetworkID, listIndex);
            NetworkManager.Instance.AddNetworkObject(NetworkManager.Instance.NetworkedObjects[parentNetworkID].SubObjects[listIndex]);

            AssignInteractableHandlers(NetworkManager.Instance.NetworkedObjects[parentNetworkID].SubObjects[listIndex]);
        }

        private void AssignInteractableHandlers(NetworkObject networkItem)
        {
            XRGrabInteractable interactable = networkItem.gameObject.GetComponent<XRGrabInteractable>();
            if (interactable != null)
            {
                interactable.interactionManager = GameObject.FindObjectOfType<XRInteractionManager>();
                interactable.selectEntered.AddListener((args) =>
                 {
                     NetworkObject obj = args.interactableObject.transform.gameObject.GetComponent<NetworkObject>();
                     NetworkObject hand = args.interactorObject.transform.gameObject.GetComponent<NetworkObject>();
                     ClientSend.RequestAuthority(obj, ClientAuthorityType.Full);
                     ClientSend.RequestEquip(obj, hand, true);
                     NetworkManager.Instance.Core.ClientObjects.Add(obj.NetworkID, obj);
                 });

                interactable.selectExited.AddListener((args) =>
                {
                    Debug.Log("EXITED");
                    NetworkObject obj = args.interactableObject.transform.gameObject.GetComponent<NetworkObject>();
                    NetworkObject hand = args.interactorObject.transform.gameObject.GetComponent<NetworkObject>();
                    ClientSend.RequestAuthority(obj, ClientAuthorityType.None);
                    ClientSend.RequestEquip(obj, hand, false);
                    NetworkManager.Instance.Core.ClientObjects.Remove(obj.NetworkID);
                });

                interactable.activated.AddListener((args) =>
                {
                    NetworkObject obj = args.interactableObject.transform.gameObject.GetComponent<NetworkObject>();
                    NetworkObject hand = args.interactorObject.transform.gameObject.GetComponent<NetworkObject>();
                    IActivateable activateable = obj.gameObject.GetComponent<IActivateable>();
                    if (activateable == null)
                    {
                        Debug.Log("Object is not activatable.");
                        return;
                    }

                    activateable.LocalActivate();
                    ClientSend.Activate(obj, hand);
                });
            }
            else
            {
                Debug.LogWarning("Object does not have grab interactable.");
            }
        }

        //////////Private methods//////////
        private void OnTick()
        {
            if (!_hasStarted) return;
            _client.Update();

            SendObjectUpdates();
        }

        /// <summary>
        /// Send updates about all objects roughly every few ticks.
        /// </summary>
        private void SendObjectUpdates()
        {
            if (NetworkManager.Instance.TimeManager.CurrentTick % 4 == 0)
            {
                foreach (KeyValuePair<int, NetworkObject> obj in ClientObjects)
                {
                    if (!obj.Value.ShouldSync) continue; //Skip this object if set to not sync.

                    Debug.Log("Sending " + obj.Value.transform.position);
                    ClientSend.RequestObjectUpdate(obj.Value, obj.Value.transform.position, obj.Value.transform.rotation);
                }
            }
        }

        private void DidConnect(object sender, EventArgs e)
        {
            Logger.Log("Connected.");
            ClientSend.SendPlayerInformation("test");
        }

        private void FailedToConnect(object sender, EventArgs e)
        {
            Logger.Log("Failed to connect to server.");
        }
    }
}

