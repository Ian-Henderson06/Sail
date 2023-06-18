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
        [MessageHandler((ushort)PacketType.SailClientPacket.PlayerInformation)]
        private static void PlayerInformation(ushort clientID, Message message)
        {
            string username = message.GetString();
            NetworkPlayer player = Manager.Instance.Core.SpawnPlayer(username, clientID);
            ServerSend.SpawnPlayer(player);
            Manager.Instance.Core.UpdateNetworkObjectAuthority(player.NetworkID, clientID, ClientAuthorityType.Full); //Give clients full authority of their player objects.

            //If has sub objects then assign them
            if (Manager.Instance.NetworkedObjects[player.NetworkID].SubObjects.Length > 0)
            {
                Manager.Instance.Core.AssignSubObjects(player.NetworkID);

                foreach (SubNetworkObject child in Manager.Instance.NetworkedObjects[player.NetworkID].SubObjects)
                {
                    Manager.Instance.Core.UpdateNetworkObjectAuthority(child.NetworkID, clientID, ClientAuthorityType.Full); //Give clients full authority of their player sub objects.
                }
            }

            Manager.Instance.Core.UpdateJoinedPlayer(clientID);
        }

        /// <summary>
        /// Allow clients to request some form of authority over an object.
        /// </summary>
        [MessageHandler((ushort)PacketType.SailClientPacket.RequestAuthorityObject)]
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
                    ServerSend.UpdateAuthoritySpecific(Manager.Instance.NetworkedObjects[networkID], clientID, ClientAuthorityType.None);
                    Logger.LogWarning($"Refused authority change request. Object already owned by {Authority.GetAuthorityOwner(networkID)} already.");
                    return;
                }
            }


            Rigidbody rigidbody = Manager.Instance.NetworkedObjects[networkID].GetComponent<Rigidbody>();
            if ((ClientAuthorityType)authorityType == ClientAuthorityType.Full)
            {
                if (rigidbody != null)
                {
                    rigidbody.isKinematic = true;
                }
                Authority.RegisterClientAuthority(networkID, clientID, (ClientAuthorityType)authorityType);
                Manager.Instance.NetworkedObjects[networkID].SetAuthority(clientID);
            }
            else
            {
                if (rigidbody != null)
                {
                    rigidbody.isKinematic = false;
                }
                if (Authority.GetAuthorityOwner(networkID) != null)
                    Authority.UnregisterClientAuthority(networkID);

                Manager.Instance.NetworkedObjects[networkID].SetAuthority(int.MaxValue);
            }

            ServerSend.UpdateAuthority(Manager.Instance.NetworkedObjects[networkID], (ClientAuthorityType)authorityType);

            Logger.LogWarning($"Authority of object {networkID} has changed to {(ClientAuthorityType)authorityType} and now has authority id of {Manager.Instance.NetworkedObjects[networkID].AuthorityID}. This was requested by client {clientID}.");

        }

        /// <summary>
        /// Allow clients to update the position of an object they have authority over.
        /// </summary>
        [MessageHandler((ushort)PacketType.SailClientPacket.RequestUpdateObject)]
        private static void RequestUpdate(ushort clientID, Message message)
        {
            int networkID = message.GetInt();
            if (Authority.GetAuthorityOwner(networkID) == clientID)
            {
                if (Authority.GetAuthority(networkID) == ClientAuthorityType.Full)
                {
                    Vector3 position = message.GetVector3();
                    Quaternion rotation = message.GetQuaternion();
                    GameObject obj = Manager.Instance.NetworkedObjects[networkID].gameObject;
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
    }
}
