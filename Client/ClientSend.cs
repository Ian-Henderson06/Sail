using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Riptide;
using Sail.Data;
using UnityEngine;

using Logger = Sail.Util.Logger;

namespace Sail.Core.Server
{
    public static class ClientSend
    {
        /// <summary>
        /// Send the players information to the server.
        /// </summary>
        /// <param name="username"></param>
        public static void SendPlayerInformation(string username)
        {
            Message message = Message.Create(MessageSendMode.Reliable, (ushort)PacketType.SailClientPacket.PlayerInformation);
            message.Add(username);
            Manager.Instance.ClientCore.Send(message);
        }

        /// <summary>
        /// Request authority for an object at the specified authority level.
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="authLevel"></param>
        public static void RequestAuthority(NetworkObject obj, ClientAuthorityType authLevel)
        {
            Message message = Message.Create(MessageSendMode.Reliable, (ushort)PacketType.SailClientPacket.RequestAuthorityObject);
            message.Add(obj.NetworkID);
            message.Add((ushort)authLevel);
            Manager.Instance.ClientCore.Send(message);
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
            Message message = Message.Create(sendMode, (ushort)PacketType.SailClientPacket.RequestUpdateObject);
            message.Add(obj.NetworkID);
            message.Add(position);
            message.Add(rotation);
            Manager.Instance.ClientCore.Send(message);
        }

        /// <summary>
        /// Request the equipping or unequipping of an object on the server
        /// </summary>
        /// <param name="nwo"></param>
        public static void RequestEquip(NetworkObject nwo, NetworkObject hand, bool shouldEquip)
        {
            Message message = Message.Create(MessageSendMode.Reliable, (ushort)PacketType.SailClientPacket.RequestEquip);
            message.Add(nwo.NetworkID);
            message.Add(hand.NetworkID);

            if (shouldEquip)
                message.Add(true);
            else
                message.Add(false);

            Manager.Instance.ClientCore.Send(message);
        }

        /// <summary>
        /// Activate object in current hand.
        /// </summary>
        /// <param name="nwo"></param>
        public static void Activate(NetworkObject objectToActivate, NetworkObject hand)
        {
            Message message = Message.Create(MessageSendMode.Reliable, (ushort)PacketType.SailClientPacket.Activate);
            message.Add(objectToActivate.NetworkID);
            message.Add(hand.NetworkID);
            Manager.Instance.ClientCore.Send(message);
        }
    }
}
