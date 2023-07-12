using System;
using System.Collections;
using System.Collections.Generic;

using Riptide;
using Riptide.Utils;

using Sail.Util;
using Sail.Data;

using UnityEngine;
using Logger = Sail.Util.Logger;


namespace Sail.Core.Client
{
    /// <summary>
    /// Server specific implementation of a network core. 
    /// Handles all server based connection events.
    /// </summary>
    public class ClientCore : MonoBehaviour, INetworkCore
    {
        //Properties
        public SailPlayer LocalPlayer { get { return _localPlayer; } }
        public Dictionary<int, NetworkObject> ClientObjects = new Dictionary<int, NetworkObject>();

        public ClientFunctions ClientFunctions { get { return _clientFunctions; } }

        //Private serialized fields
        [SerializeField] private GameObject _remotePlayerPrefab;
        [SerializeField] private GameObject _localPlayerPrefab;

        //Private fields
        private Riptide.Client _client;
        private bool _hasStarted;
        private SailPlayer _localPlayer;
        private ClientFunctions _clientFunctions;

        //Properties
        public Riptide.Client Client { get { return _client; } }

        public Peer GetPeer() => _client;

        private void OnDestroy()
        {
            Manager.Instance.OnTick -= OnTick;

            if (_client != null)
            {
                _client.Connected -= DidConnect;
            }
        }

        public void InitialiseCore()
        {
            Logger.Log("Initialising Client Core.");
            _hasStarted = false;
            _client = new Riptide.Client();

            _client.Connected += DidConnect;
            _client.ConnectionFailed += FailedToConnect;

            Manager.Instance.OnTick += OnTick;
            _clientFunctions = new ClientFunctions();
        }



        /// <summary>
        /// Try to connect the client to a server.
        /// </summary>
        /// <param name="address">IP and port of the server to connect to.</param>
        /// <param name="connectionAttempts">Maximum number of attempts to connect.</param>
        public bool AttemptConnection(string address, int connectionAttempts = 5)
        {
            if (_client == null)
            {
                Logger.LogError("Client hasn't been initialized - can't attempt connect to server.", this);
                return false;
            }

            _hasStarted = true;
            Logger.Log("Starting client.");
            Logger.Log($"Attempting connection to {address}...");
            bool result = _client.Connect(address, connectionAttempts);
            return result;
        }

        /// <summary>
        /// Spawn a player on the client at a specific position and rotation.
        /// </summary>
        /// <param name="username"></param>
        /// <param name="playerID"></param>
        public SailPlayer SpawnPlayer(string username, ushort playerID, int networkID, Vector3 position, Quaternion rotation)
        {
            Logger.Log($"Spawning {username}.");

            GameObject player;

            //Adjust prefab spawned based on if player is local or remote.
            if (playerID == _client.Id)
                player = Instantiate(_localPlayerPrefab, position, rotation);
            else
                player = Instantiate(_remotePlayerPrefab, position, rotation);

            SailPlayer playerNetwork = player.GetComponent<SailPlayer>();
            NetworkObject networkItem = player.GetComponent<NetworkObject>();

            if (playerNetwork == null)
            {
                Logger.LogError($"Could not fetch SailPlayer script from player prefab.", this);
                return null;
            }

            if (networkItem == null)
            {
                Logger.LogError($"Could not fetch NetworkObject script from player prefab.", this);
                return null;
            }


            //If local player
            if (playerID == _client.Id)
            {
                _localPlayer = playerNetwork;
            }

            playerNetwork.InitialisePlayer(username, playerID, networkID);
            networkItem.InitialiseObject(networkID, -1); //player has item id of -1
            Manager.Instance.AddNetworkObject(networkItem);
            Manager.Instance.AddPlayer(playerNetwork);

            return playerNetwork;
        }

        /// <summary>
        /// Destroy a player on the server.
        /// </summary>
        /// <param name="username"></param>
        /// <param name="playerID"></param>
        public void DestroyPlayer(ushort playerID)
        {
            SailPlayer playerNetwork;
            if (Manager.Instance.Players.TryGetValue(playerID, out playerNetwork))
            {
                NetworkObject networkItem = playerNetwork.gameObject.GetComponent<NetworkObject>();
                Manager.Instance.RemoveNetworkObject(networkItem);
                Manager.Instance.RemovePlayer(playerNetwork);
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
                Logger.LogError($"Could not fetch NetworkObject script from prefab.", this);
                return null;
            }

            Rigidbody rigidbody = networkItem.gameObject.GetComponent<Rigidbody>();
            if (rigidbody != null)
            {
                rigidbody.isKinematic = true;
            }

            networkItem.InitialiseObject(networkID, itemID);
            Manager.Instance.AddNetworkObject(networkItem);

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
            if (Manager.Instance.NetworkedObjects.TryGetValue(networkID, out networkObject))
            {
                //If has sub objects then remove them
                if (Manager.Instance.NetworkedObjects[networkID].SubObjects.Length > 0)
                {
                    foreach (SubNetworkObject child in Manager.Instance.NetworkedObjects[networkID].SubObjects)
                    {
                        if (ClientObjects.ContainsKey(child.NetworkID)) ClientObjects.Remove(child.NetworkID); //If client has control over entity then remove it from that list

                        Manager.Instance.RemoveNetworkObject(child);
                    }
                }

                if (ClientObjects.ContainsKey(networkObject.NetworkID)) ClientObjects.Remove(networkObject.NetworkID); //If client has control over entity then remove it from that list
                Manager.Instance.RemoveNetworkObject(networkObject);
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
            Manager.Instance.NetworkedObjects[parentNetworkID].SubObjects[listIndex].InitialiseObject(childNetworkID, -1);
            Manager.Instance.NetworkedObjects[parentNetworkID].SubObjects[listIndex].InitializeSubObject(parentNetworkID, listIndex);
            Manager.Instance.AddNetworkObject(Manager.Instance.NetworkedObjects[parentNetworkID].SubObjects[listIndex]);
        }

        /// <summary>
        /// Changes the internal client functions used. Allows users to specify custom behaviour for objects.
        /// </summary>
        /// <param name="functions"></param>
        public void AssignClientFunctions(ClientFunctions functions)
        {
            _clientFunctions = functions;
        }

        /// <summary>
        /// Send from the client to the server.
        /// Wrapper around Riptides method.
        /// </summary>
        public void Send(Message message, bool shouldRelease = true)
        {
            _client.Send(message, shouldRelease);
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
            if (Manager.Instance.TimeManager.CurrentTick % 4 == 0)
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

