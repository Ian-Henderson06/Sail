﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Riptide;
using UnityEngine;
using Logger = Sail.Util.Logger;

using Sail.Data;


namespace Sail.Core.Client
{
    public static class ClientReceive
    {
        /// <summary>
        /// Received the new sync tick.
        /// </summary>
        [MessageHandler((ushort)PacketType.ServerPacket.Sync)]
        private static void SyncTick(Message message)
        {
            uint serverTick = message.GetUInt();
            if (Mathf.Abs(Manager.Instance.TimeManager.CurrentTick - serverTick) > 10)
            {
                Manager.Instance.TimeManager.CurrentTick = serverTick;
            }
        }

        /// <summary>
        /// Received the spawn player packet.
        /// Spawn a player once packet has been received.
        /// </summary>
        [MessageHandler((ushort)PacketType.ServerPacket.SpawnPlayer)]
        private static void SpawnPlayer(Message message)
        {
            ushort playerID = message.GetUShort();
            int networkID = message.GetInt();
            string username = message.GetString();
            Vector3 spawnPosition = message.GetVector3();
            Quaternion spawnRotation = message.GetQuaternion();

            Manager.Instance.Core.SpawnPlayer(username, playerID, networkID, spawnPosition, spawnRotation);
        }

        /// <summary>
        /// Destroy a players representation on the client.
        /// </summary>
        [MessageHandler((ushort)PacketType.ServerPacket.DestroyPlayer)]
        private static void DestroyPlayer(Message message)
        {
            ushort playerID = message.GetUShort();
            Manager.Instance.Core.DestroyPlayer(playerID);
        }

        /// <summary>
        /// Update a players position on this client.
        /// </summary>
        [MessageHandler((ushort)PacketType.ServerPacket.UpdatePlayer)]
        private static void UpdatePlayer(Message message)
        {
            ushort playerID = message.GetUShort();
            Vector3 newPosition = message.GetVector3();
            Quaternion newRotation = message.GetQuaternion();

            Manager.Instance.Players[playerID].transform.position = newPosition;
            Manager.Instance.Players[playerID].transform.rotation = newRotation;
        }


        /// <summary>
        /// Spawn a networked object on this client.
        /// </summary>
        [MessageHandler((ushort)PacketType.ServerPacket.SpawnObject)]
        private static void SpawnNetworkObject(Message message)
        {
            int itemID = message.GetInt();
            int networkID = message.GetInt();
            Vector3 spawnPosition = message.GetVector3();
            Quaternion spawnRotation = message.GetQuaternion();

            if (Manager.Instance.NetworkedObjects.ContainsKey(networkID))
            {
                Logger.Log("Object already exits. Won't spawn new version.");
            }

            Manager.Instance.Core.SpawnNetworkObject(networkID, itemID, spawnPosition, spawnRotation);
        }

        /// <summary>
        /// Destroy a networked object on this client.
        /// </summary>
        [MessageHandler((ushort)PacketType.ServerPacket.DestroyObject)]
        private static void DestroyNetworkObject(Message message)
        {
            int networkID = message.GetInt();
            Manager.Instance.Core.DestroyNetworkObject(networkID);
        }

        /// <summary>
        /// Update a networked objects position.
        /// </summary>
        [MessageHandler((ushort)PacketType.ServerPacket.UpdateObject)]
        private static void UpdateNetworkObject(Message message)
        {
            int networkID = message.GetInt();

            if (!Manager.Instance.NetworkedObjects.ContainsKey(networkID))
                return;

            //If we have authority over this object or it is owned by the client then discard the incoming packet
            if (Manager.Instance.NetworkedObjects[networkID].AuthorityID == Manager.Instance.Core.Client.Id || Manager.Instance.Core.ClientObjects.ContainsKey(networkID))
            {
                return;
            }

            Vector3 newPosition = message.GetVector3();
            Quaternion newRotation = message.GetQuaternion();

            Manager.Instance.NetworkedObjects[networkID].transform.position = newPosition;
            Manager.Instance.NetworkedObjects[networkID].transform.rotation = newRotation;
        }

        /// <summary>
        /// Received authority update from the server.
        /// This is only authority changes relevant to this player
        /// As far as the local player is concerned, the server has authority over all other entities,
        /// even if another player has control.
        /// </summary>
        [MessageHandler((ushort)PacketType.ServerPacket.UpdateAuthorityObject)]
        private static void UpdateAuthority(Message message)
        {
            int networkID = message.GetInt();
            int clientID = message.GetInt();
            ushort newAuth = message.GetUShort();

            Debug.Log("Updating " + networkID + " to " + clientID);

            XRGrabInteractable interactable = Manager.Instance.NetworkedObjects[networkID].gameObject.GetComponent<XRGrabInteractable>();

            if (clientID == Manager.Instance.Core.Client.Id)
            {
                if ((ClientAuthorityType)newAuth == ClientAuthorityType.Full)
                {
                    Rigidbody rigidbody = Manager.Instance.NetworkedObjects[networkID].gameObject.GetComponent<Rigidbody>();
                    if (rigidbody != null)
                    {
                        rigidbody.isKinematic = false;
                    }

                    Debug.Log("Have authority over " + Manager.Instance.NetworkedObjects[networkID].name);
                }


                //Automatically remove an object from client objects if the authority is going back to None
                //IN FUTURE THIS SHOULD BE DONE IN THE MANAGER - NOT HERE :)
                if ((ClientAuthorityType)newAuth == ClientAuthorityType.None)
                {
                    if (Manager.Instance.Core.ClientObjects.ContainsKey(networkID))
                    {
                        Manager.Instance.Core.ClientObjects.Remove(networkID);
                    }
                }
            }
            else
            {
                if ((ClientAuthorityType)newAuth == ClientAuthorityType.Full)
                {
                    //Turn off your interactable if it belongs to someone else
                    if (interactable != null)
                    {
                        if (interactable.enabled)
                        {
                            interactable.enabled = false;
                        }
                    }
                }

                //Turn on your interactable if it belongs to someone else
                if ((ClientAuthorityType)newAuth == ClientAuthorityType.None)
                {
                    if (interactable != null)
                    {
                        if (!interactable.enabled)
                        {
                            interactable.enabled = true;
                        }
                    }
                }
            }

            Authority.RegisterClientAuthority(networkID, clientID, (ClientAuthorityType)newAuth);
            Manager.Instance.NetworkedObjects[networkID].SetAuthority(clientID);
        }

        /// <summary>
        ///Receive an update from the server to assign and setup some sub objects.
        /// </summary>
        [MessageHandler((ushort)PacketType.ServerPacket.AssignSubObject)]
        private static void AssignSubObjects(Message message)
        {
            int parentNetworkID = message.GetInt();
            ushort listIndex = message.GetUShort();
            int childNetworkID = message.GetInt();

            Manager.Instance.Core.AssignSubObject(parentNetworkID, listIndex, childNetworkID);
        }

        /// <summary>
        ///Receive an update from the server to assign and setup some sub objects.
        /// </summary>
        [MessageHandler((ushort)PacketType.ServerPacket.CallRPC)]
        private static void HandleRPC(Message message)
        {
            int networkID = message.GetInt();
            string methodName = message.GetString();

            Debug.Log("GOT RPC");

            Reflector.DeSerializeAndCallRPC(networkID, methodName, ref message);
        }

        /// <summary>
        ///Receive an update from the server set flags on this client.
        /// </summary>
        [MessageHandler((ushort)PacketType.ServerPacket.UpdateFlags)]
        private static void Flags(Message message)
        {
            int networkID = message.GetInt();
            byte flags = message.GetByte();

            Manager.Instance.NetworkedObjects[networkID].SetFlags(flags);
        }
    }
}
