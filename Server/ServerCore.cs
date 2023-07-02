using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;

using Riptide;
using Riptide.Utils;

using Sail.Util;
using Sail.Data;

using UnityEngine;
using Logger = Sail.Util.Logger;

namespace Sail.Core.Server
{
    /// <summary>
    /// Server specific implementation of a network core. 
    /// Handles all server based connection events.
    /// </summary>
    public class ServerCore : MonoBehaviour, INetworkCore
    {
        //Private serialized fields
        [SerializeField] private GameObject _playerPrefab;

        //Private fields
        private Riptide.Server _server;
        private bool _hasStarted;
        private Issuer _idIssuer;
        private int _clientsConnected = 0;

        /// <summary>
        /// Stores persistent data
        /// Keeps the objects they associate with for comparisons
        /// DICT: Associated object network ID, persistent data associated.
        /// </summary>
        private Dictionary<int, NetworkObjectPersistentData> _persistentData = new Dictionary<int, NetworkObjectPersistentData>();

        //Properties
        public Riptide.Server Server { get { return _server; } }

        public Peer GetPeer() => _server;

        private void OnDestroy()
        {
            Manager.Instance.OnTick -= OnTick;

            if (_server != null)
            {
                _server.ClientConnected -= ClientConnected;
                _server.ClientDisconnected -= ClientDisconnected;

                _server.Stop();
            }
        }

        public void InitialiseCore()
        {
            Logger.Log("Initialising Server Core.");
            _hasStarted = false;
            _server = new Riptide.Server();
            _idIssuer = new Issuer();

            Manager.Instance.OnTick += OnTick;
            _server.ClientConnected += ClientConnected;
            _server.ClientDisconnected += ClientDisconnected;
        }

        public void StartServer(ushort runningPort, ushort maxPlayers)
        {
            if (_server == null)
            {
                Logger.LogError("Server hasn't been initialized - can't start server.", this);
                return;
            }

            Logger.Log("Starting server.");

            if (Application.runInBackground == false)
            {
                Logger.LogWarning("Please make sure to run the server with RunInBackground checked. Otherwise this may cause no connection errors on client. Sail will automatically enable this setting incase.");
                Application.runInBackground = true;
            }

            _server.Start(runningPort, maxPlayers);

            _hasStarted = true;
        }

        /// <summary>
        /// Spawn a player on the server.
        /// </summary>
        /// <param name="username"></param>
        /// <param name="playerID"></param>
        public SailPlayer SpawnPlayer(string username, ushort playerID)
        {
            Logger.Log($"Spawning {username}.");
            GameObject player = Instantiate(_playerPrefab, Vector3.zero, Quaternion.identity);
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

            int id = _idIssuer.RequestIssue();
            playerNetwork.InitialisePlayer(username, playerID, id);
            networkItem.InitialiseObject(id, -1); //player has item id of -1
            Manager.Instance.AddPlayer(playerNetwork);
            Manager.Instance.AddNetworkObject(networkItem);

            //Give the player some persistent data
            _persistentData.Add(networkItem.NetworkID, new NetworkObjectPersistentData(networkItem.ItemID, 128));

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
                //If has sub objects then remove them
                if (Manager.Instance.NetworkedObjects[networkItem.NetworkID].SubObjects.Length > 0)
                {
                    foreach (SubNetworkObject child in Manager.Instance.NetworkedObjects[networkItem.NetworkID].SubObjects)
                    {
                        Manager.Instance.RemoveNetworkObject(child);
                        Destroy(child.gameObject);
                    }
                }
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
        /// Spawn a networked object on the server.
        /// </summary>
        /// <param name="username"></param>
        /// <param name="playerID"></param>
        public NetworkObject SpawnNetworkObject(int itemID, Vector3 position, Quaternion rotation)
        {
            Logger.Log($"Spawning item {itemID}.");
            GameObject item = Instantiate(NetworkObjectList.Instance.FindObject(itemID).Prefab, position, rotation);
            NetworkObject networkItem = item.GetComponent<NetworkObject>();

            if (networkItem == null)
            {
                Logger.LogError($"Could not fetch NetworkObject script from prefab.", this);
                return null;
            }

            networkItem.InitialiseObject(_idIssuer.RequestIssue(), itemID);
            Manager.Instance.AddNetworkObject(networkItem);

            _persistentData.Add(networkItem.NetworkID, new NetworkObjectPersistentData(networkItem.ItemID, 128));

            //If has sub objects then assign them
            if (Manager.Instance.NetworkedObjects[networkItem.NetworkID].SubObjects.Length > 0)
            {
                AssignSubObjects(networkItem.NetworkID);
            }

            //When flags change send a network update
            networkItem.OnFlagsChanged += () => { ServerSend.UpdateFlags(networkItem); };

            if (_clientsConnected > 0)
                ServerSend.SpawnNetworkObject(networkItem); //Spawn on currently connected clients.

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

                        if (_persistentData.ContainsKey(child.NetworkID))
                        {
                            //Release all stored messages in persistent data prior to removal
                            _persistentData[child.NetworkID].ClearMessages(100000);
                            _persistentData.Remove(child.NetworkID);
                        }
                        Manager.Instance.RemoveNetworkObject(child);
                    }
                }

                if (_persistentData.ContainsKey(networkID))
                {
                    //Release all stored messages in persistent data prior to removal
                    _persistentData[networkID].ClearMessages(100000);
                    _persistentData.Remove(networkID);
                }

                if (_clientsConnected > 0)
                    ServerSend.DestroyNetworkObject(networkObject); //Get current clients to remove object

                Manager.Instance.RemoveNetworkObject(networkObject);
                Destroy(networkObject.gameObject);
            }
            else
            {
                Logger.Log($"Could not destroy network object {networkID}.");
            }
        }

        public void UpdateNetworkObjectAuthority(int networkID, ushort clientID, ClientAuthorityType auth)
        {
            Authority.RegisterClientAuthority(networkID, clientID, auth);
            Manager.Instance.NetworkedObjects[networkID].SetAuthority(clientID);
            ServerSend.UpdateAuthority(Manager.Instance.NetworkedObjects[networkID], auth);
        }

        /// <summary>
        /// Properly initialise a childs sub objects.
        /// </summary>
        /// <param name="parentID"></param>
        public void AssignSubObjects(int parentID)
        {
            NetworkObject parent = Manager.Instance.NetworkedObjects[parentID];
            for (int i = 0; i < parent.SubObjects.Length; i++)
            {
                SubNetworkObject child = parent.SubObjects[i];

                child.InitialiseObject(_idIssuer.RequestIssue(), -1);
                child.InitializeSubObject(parentID, i);
                Manager.Instance.AddNetworkObject(child);
                ServerSend.AssignSubObject(parent, child);
            }
        }

        /// <summary>
        /// Send over all existing network objects to newly joined clients
        /// </summary>
        /// <param name="clientID"></param>
        public void UpdateJoinedPlayer(ushort newPlayerID)
        {
            foreach (KeyValuePair<int, NetworkObject> existingObject in Manager.Instance.NetworkedObjects)
            {
                // Sub objects need to be handled by a parent - we don't want to spawn this sub object as an independent object.
                if (existingObject.Value.GetType() == typeof(SubNetworkObject))
                {
                    continue;
                }

                //Ensures we dont send 'spawn other players' packets for this connections player as its already been spawned
                if (existingObject.Value.gameObject.GetComponent<SailPlayer>() != null && existingObject.Value.gameObject.GetComponent<SailPlayer>().PlayerID != newPlayerID)
                {
                    ServerSend.SpawnPlayer(existingObject.Value.gameObject.GetComponent<SailPlayer>(), newPlayerID);
                    //Send IDs if new player or network object contains sub objects.
                    for (int i = 0; i < existingObject.Value.SubObjects.Length; i++)
                    {
                        ServerSend.AssignSubObject(existingObject.Value, existingObject.Value.SubObjects[i], newPlayerID);
                    }
                }

                //If just a regular object
                if (existingObject.Value.gameObject.GetComponent<SailPlayer>() == null)
                {
                    ServerSend.SpawnNetworkObject(existingObject.Value, newPlayerID);
                    ServerSend.UpdateFlags(existingObject.Value, newPlayerID); //Update new joining player on the state of this objects flags.
                }

                //Send persistent data for this object if it contains any persistent data
                //Objects such as sub objects do not contain any persistent data
                if (_persistentData.ContainsKey(existingObject.Key))
                {
                    // foreach (Message message in _persistentData[existingObject.Key].Messages)
                    // {
                    //     _server.Send(message, newPlayerID, false);
                    // }

                    //Only send the latest of each packet type
                    //TODO: 
                    foreach (KeyValuePair<ushort, Message> pair in _persistentData[existingObject.Key].LatestOfUniqueType)
                    {
                        _server.Send(pair.Value, newPlayerID, false);
                    }
                }

            }
        }

        public void AddPersistentMessage(int networkID, ushort packetType, Message message)
        {
            //If error here then object has no persistent data - ensure _persistentData.Add is called when an object is created that needs persistent data
            if (_persistentData[networkID] == null)
            {
                Logger.LogError("Cant add persistent data because network object doesnt have space in dictionary. Please ensure network object was setup correctly", this);
            }

            _persistentData[networkID].AddMessage(packetType, message);
        }

        /// <summary>
        /// Send from the server to a client.
        /// Wrapper around Riptides method.
        /// </summary>
        public void Send(Message message, ushort toClient, bool shouldRelease = true)
        {
            _server.Send(message, toClient, shouldRelease);
        }

        /// <summary>
        /// Send from the server to all clients except one.
        /// Wrapper around Riptides method.
        /// </summary>
        public void SendToAll(Message message, ushort exceptToClient, bool shouldRelease = true)
        {
            _server.SendToAll(message, exceptToClient, shouldRelease);
        }

        /// <summary>
        /// Send from the server to all clients.
        /// Wrapper around Riptides method.
        /// </summary>
        public void SendToAll(Message message, bool shouldRelease = true)
        {
            _server.SendToAll(message, shouldRelease);
        }




        //////////Private methods//////////
        private void OnTick()
        {
            if (!_hasStarted) return;
            _server.Update();

            HandleTickSync();
            SendObjectUpdates();
        }


        /// <summary>
        /// Send sync message every 5 seconds roughly.
        /// </summary>
        private void HandleTickSync()
        {
            if (Manager.Instance.TimeManager.CurrentTick % (Manager.Instance.TimeManager.TickRate * 5) == 0)
            {
                ServerSend.SyncTicks();
            }
        }

        /// <summary>
        /// Send updates about all objects roughly every few ticks.
        /// </summary>
        private void SendObjectUpdates()
        {
            if (Manager.Instance.TimeManager.CurrentTick % 4 == 0)
            {
                foreach (KeyValuePair<int, NetworkObject> obj in Manager.Instance.NetworkedObjects)
                {
                    if (!obj.Value.ShouldSync) continue;

                    ServerSend.UpdateNetworkObject(obj.Value);
                }
            }
        }


        private void ClientConnected(object sender, ServerConnectedEventArgs e)
        {
            Logger.Log($"Client {e.Client.Id} has connected.");
            _clientsConnected++;
        }

        /// <summary>
        /// Destroy players representation on remote machines when they disconnect from server.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ClientDisconnected(object sender, ServerDisconnectedEventArgs e)
        {
            Logger.Log($"Client {e.Client.Id} has disconnected.");
            ServerSend.DestroyPlayer(Manager.Instance.Players[e.Client.Id]);
            DestroyPlayer(e.Client.Id);
            _clientsConnected--;
        }


    }
}

