using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Riptide;
using UnityEngine;

using Logger = INet.Util.Logger;

namespace INet
{
    public static class ClientSend
    {
        /// <summary>
        /// Send the players information to the server.
        /// </summary>
        /// <param name="username"></param>
        public static void SendPlayerInformation(string username)
        {
            Message message = Message.Create(MessageSendMode.Reliable, (ushort)PacketType.ClientPacket.PlayerInformation);
            message.Add(username);
            NetworkManager.Instance.Core.Client.Send(message);
        }

        /// <summary>
        /// Request authority for an object at the specified authority level.
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="authLevel"></param>
        public static void RequestAuthority(NetworkObject obj, ClientAuthorityType authLevel)
        {
            Message message = Message.Create(MessageSendMode.Reliable, (ushort)PacketType.ClientPacket.RequestAuthorityObject);
            message.Add(obj.NetworkID);
            message.Add((ushort)authLevel);
            NetworkManager.Instance.Core.Client.Send(message);
        }

        /// <summary>
        /// Request authority for an object at the specified authority level.
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="authLevel"></param>
        public static void RequestObjectUpdate(NetworkObject obj, Vector3 position, Quaternion rotation, bool reliable = false)
        {
            if (Authority.GetAuthorityOwner(obj.NetworkID) == null)
            {
                Logger.LogWarning("Attempting to update an object you might not own yet. This may be denied.");
            }

            MessageSendMode sendMode = reliable == false ? MessageSendMode.Unreliable : MessageSendMode.Reliable;
            Message message = Message.Create(sendMode, (ushort)PacketType.ClientPacket.RequestUpdateObject);
            message.Add(obj.NetworkID);
            message.Add(position);
            message.Add(rotation);
            NetworkManager.Instance.Core.Client.Send(message);
        }

        /// <summary>
        /// Request the equipping or unequipping of an object on the server
        /// </summary>
        /// <param name="nwo"></param>
        public static void RequestEquip(NetworkObject nwo, NetworkObject hand, bool shouldEquip)
        {
            Message message = Message.Create(MessageSendMode.Reliable, (ushort)PacketType.ClientPacket.RequestEquip);
            message.Add(nwo.NetworkID);
            message.Add(hand.NetworkID);

            if (shouldEquip)
                message.Add(true);
            else
                message.Add(false);

            NetworkManager.Instance.Core.Client.Send(message);
        }

        /// <summary>
        /// Activate object in current hand.
        /// </summary>
        /// <param name="nwo"></param>
        public static void Activate(NetworkObject objectToActivate, NetworkObject hand)
        {
            Message message = Message.Create(MessageSendMode.Reliable, (ushort)PacketType.ClientPacket.Activate);
            message.Add(objectToActivate.NetworkID);
            message.Add(hand.NetworkID);
            NetworkManager.Instance.Core.Client.Send(message);
        }
    }
}
