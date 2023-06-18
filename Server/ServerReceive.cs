using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Riptide;
using UnityEngine;
using Logger = Sail.Util.Logger;

namespace Sail.Core.Server
{
    public static class ServerReceive
    {
        /// <summary>
        /// Received the players information.
        /// Spawn player on server and tell clients to duplicate.
        /// </summary>
        [MessageHandler((ushort)PacketType.ClientPacket.PlayerInformation)]
        private static void PlayerInformation(ushort clientID, Message message)
        {
            string username = message.GetString();
            NetworkPlayer player = NetworkManager.Instance.Core.SpawnPlayer(username, clientID);
            ServerSend.SpawnPlayer(player);
            NetworkManager.Instance.Core.UpdateNetworkObjectAuthority(player.NetworkID, clientID, ClientAuthorityType.Full); //Give clients full authority of their player objects.

            //If has sub objects then assign them
            if (NetworkManager.Instance.NetworkedObjects[player.NetworkID].SubObjects.Length > 0)
            {
                NetworkManager.Instance.Core.AssignSubObjects(player.NetworkID);

                foreach (SubNetworkObject child in NetworkManager.Instance.NetworkedObjects[player.NetworkID].SubObjects)
                {
                    NetworkManager.Instance.Core.UpdateNetworkObjectAuthority(child.NetworkID, clientID, ClientAuthorityType.Full); //Give clients full authority of their player sub objects.
                }
            }

            NetworkManager.Instance.Core.UpdateJoinedPlayer(clientID);
        }

        /// <summary>
        /// Allow clients to request some form of authority over an object.
        /// </summary>
        [MessageHandler((ushort)PacketType.ClientPacket.RequestAuthorityObject)]
        private static void RequestAuthority(ushort clientID, Message message)
        {
            int networkID = message.GetInt();
            ushort authorityType = message.GetUShort();

            ///Refused authority as stranger is requesting authority
            if (Authority.GetAuthorityOwner(networkID) != null)
            {
                //Not server owned but owned by another client
                if (Authority.GetAuthorityOwner(networkID) != clientID && Authority.GetAuthorityOwner(networkID) != int.MaxValue)
                {
                    //Not server owned but owned by another client
                    ServerSend.UpdateAuthoritySpecific(NetworkManager.Instance.NetworkedObjects[networkID], clientID, ClientAuthorityType.None);
                    Logger.LogWarning($"Refused authority change request. Object already owned by {Authority.GetAuthorityOwner(networkID)} already.");
                    return;
                }
            }


            Rigidbody rigidbody = NetworkManager.Instance.NetworkedObjects[networkID].GetComponent<Rigidbody>();
            if ((ClientAuthorityType)authorityType == ClientAuthorityType.Full)
            {
                if (rigidbody != null)
                {
                    rigidbody.isKinematic = true;
                }
                Authority.RegisterClientAuthority(networkID, clientID, (ClientAuthorityType)authorityType);
                NetworkManager.Instance.NetworkedObjects[networkID].SetAuthority(clientID);
            }
            else
            {
                if (rigidbody != null)
                {
                    rigidbody.isKinematic = false;
                }
                if (Authority.GetAuthorityOwner(networkID) != null)
                    Authority.UnregisterClientAuthority(networkID);

                NetworkManager.Instance.NetworkedObjects[networkID].SetAuthority(int.MaxValue);
            }

            ServerSend.UpdateAuthority(NetworkManager.Instance.NetworkedObjects[networkID], (ClientAuthorityType)authorityType);

            Logger.LogWarning($"Authority of object {networkID} has changed to {(ClientAuthorityType)authorityType} and now has authority id of {NetworkManager.Instance.NetworkedObjects[networkID].AuthorityID}. This was requested by client {clientID}.");

        }

        /// <summary>
        /// Allow clients to update the position of an object they have authority over.
        /// </summary>
        [MessageHandler((ushort)PacketType.ClientPacket.RequestUpdateObject)]
        private static void RequestUpdate(ushort clientID, Message message)
        {
            int networkID = message.GetInt();
            if (Authority.GetAuthorityOwner(networkID) == clientID)
            {
                if (Authority.GetAuthority(networkID) == ClientAuthorityType.Full)
                {
                    Vector3 position = message.GetVector3();
                    Quaternion rotation = message.GetQuaternion();
                    GameObject obj = NetworkManager.Instance.NetworkedObjects[networkID].gameObject;
                    obj.transform.position = position;
                    obj.transform.rotation = rotation;
                }
                else
                {
                    Logger.LogWarning($"{clientID} does not have full authority over {networkID}.");
                }
            }
            else
            {
                Logger.LogWarning($"{clientID} does not have authority over {networkID}.");
            }
        }

        [MessageHandler((ushort)PacketType.ClientPacket.RequestEquip)]
        private static void RequestEquip(ushort clientID, Message message)
        {
            int equippedObjectsID = message.GetInt();
            int handID = message.GetInt();
            bool shouldEquip = message.GetBool();

            ServerPlayer player = NetworkManager.Instance.Players[clientID].gameObject.GetComponent<ServerPlayer>();

            if (shouldEquip)
                player.SetEquipped(handID, equippedObjectsID);
            else
                player.UnEquip(handID);
        }

        [MessageHandler((ushort)PacketType.ClientPacket.Activate)]
        private static void Activate(ushort clientID, Message message)
        {
            int objectID = message.GetInt();
            int handID = message.GetInt();
            ServerPlayer player = NetworkManager.Instance.Players[clientID].gameObject.GetComponent<ServerPlayer>();
            player.Activate(objectID, handID);
        }
    }
}
