using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Riptide;
using UnityEngine;
using Sail.Data;

using Logger = Sail.Util.Logger;

namespace Sail.Core.Server
{
    public static class ServerSend
    {
        public static void SyncTicks()
        {
            Message message = Message.Create(MessageSendMode.Unreliable, (ushort)PacketType.SailServerPacket.Sync);
            message.Add(Manager.Instance.TimeManager.CurrentTick);

            Manager.Instance.Measure.AddToMeasure(message.WrittenLength, (ushort)PacketType.SailServerPacket.Sync);
            Manager.Instance.Core.Server.SendToAll(message);
        }

        /// <summary>
        /// Send a packet with appropriate information to spawn a player.
        /// </summary>
        /// <param name="player"></param>
        public static void SpawnPlayer(SailPlayer player, int clientID = -1)
        {
            Message message = Message.Create(MessageSendMode.Reliable, (ushort)PacketType.SailServerPacket.SpawnPlayer);
            message.Add(player.PlayerID);
            message.Add(player.NetworkID);
            message.Add(player.Username);
            message.Add(player.gameObject.transform.position);
            message.Add(player.gameObject.transform.rotation);


            if (clientID == -1)
                Manager.Instance.Core.Server.SendToAll(message);
            else
                Manager.Instance.Core.Server.Send(message, (ushort)clientID);
        }

        /// <summary>
        /// Send a packet with appropriate information to destroy a player.
        /// </summary>
        /// <param name="player"></param>
        public static void DestroyPlayer(SailPlayer player)
        {
            Message message = Message.Create(MessageSendMode.Reliable, (ushort)PacketType.SailServerPacket.DestroyPlayer);
            message.Add(player.PlayerID);
            Manager.Instance.Core.Server.SendToAll(message);
        }

        /// <summary>
        /// Send a packet with appropriate information to update a players position and rotation on the clients.
        /// </summary>
        /// <param name="player"></param>
        public static void UpdatePlayer(SailPlayer player, bool reliable = false)
        {
            MessageSendMode sendMode = reliable == false ? MessageSendMode.Unreliable : MessageSendMode.Reliable;

            Message message = Message.Create(sendMode, (ushort)PacketType.SailServerPacket.UpdatePlayer);
            message.Add(player.PlayerID);
            message.Add(player.gameObject.transform.position);
            message.Add(player.gameObject.transform.rotation);

            Manager.Instance.Measure.AddToMeasure(message.WrittenLength, (ushort)PacketType.SailServerPacket.UpdatePlayer);
            Manager.Instance.Core.Server.SendToAll(message);
        }


        /// <summary>
        /// Send a packet with appropriate information to spawn a network object.
        /// </summary>
        /// <param name="player"></param>
        public static void SpawnNetworkObject(NetworkObject obj, int clientID = -1)
        {
            Message message = Message.Create(MessageSendMode.Reliable, (ushort)PacketType.SailServerPacket.SpawnObject);
            message.Add(obj.ItemID);
            message.Add(obj.NetworkID);
            message.Add(obj.gameObject.transform.position);
            message.Add(obj.gameObject.transform.rotation);

            if (clientID == -1)
                Manager.Instance.Core.Server.SendToAll(message);
            else
                Manager.Instance.Core.Server.Send(message, (ushort)clientID);
        }

        /// <summary>
        /// Send a packet with appropriate information to destroy a network object.
        /// </summary>
        /// <param name="player"></param>
        public static void DestroyNetworkObject(NetworkObject obj)
        {
            Message message = Message.Create(MessageSendMode.Reliable, (ushort)PacketType.SailServerPacket.DestroyObject);
            message.Add(obj.NetworkID);
            Manager.Instance.Core.Server.SendToAll(message);
        }

        /// <summary>
        /// Send a packet with appropriate information to update a networked objects position and rotation on clients.
        /// </summary>
        /// <param name="player"></param>
        public static void UpdateNetworkObject(NetworkObject obj, bool reliable = false)
        {
            MessageSendMode sendMode = reliable == false ? MessageSendMode.Unreliable : MessageSendMode.Reliable;

            Message message = Message.Create(sendMode, (ushort)PacketType.SailServerPacket.UpdateObject);
            message.Add(obj.NetworkID);
            message.Add(obj.gameObject.transform.position);
            message.Add(obj.gameObject.transform.rotation);

            Manager.Instance.Measure.AddToMeasure(message.WrittenLength, (ushort)PacketType.SailServerPacket.UpdateObject);
            Manager.Instance.Core.Server.SendToAll(message);
        }

        /// <summary>
        /// Send a packet with appropriate information to update a networked objects position and rotation on clients.
        /// </summary>
        /// <param name="player"></param>
        public static void UpdateAuthority(NetworkObject obj, ClientAuthorityType auth)
        {
            Message message = Message.Create(MessageSendMode.Reliable, (ushort)PacketType.SailServerPacket.UpdateAuthorityObject);
            message.Add(obj.NetworkID);
            message.Add(obj.AuthorityID);
            message.Add((ushort)auth);
            Manager.Instance.Core.Server.SendToAll(message);
        }

        /// <summary>
        /// Send a packet with appropriate information to update a networked objects position and rotation on clients.
        /// </summary>
        /// <param name="player"></param>
        public static void UpdateAuthoritySpecific(NetworkObject obj, ushort clientID, ClientAuthorityType auth)
        {
            Message message = Message.Create(MessageSendMode.Reliable, (ushort)PacketType.SailServerPacket.UpdateAuthorityObject);
            message.Add(obj.NetworkID);
            message.Add(obj.AuthorityID);
            message.Add((ushort)auth);
            Manager.Instance.Core.Server.Send(message, clientID);
        }

        /// <summary>
        /// Setup sub objects on official networking systems.
        /// </summary>
        public static void AssignSubObject(NetworkObject parent, SubNetworkObject child, int clientID = -1)
        {
            Message message = Message.Create(MessageSendMode.Reliable, (ushort)PacketType.SailServerPacket.AssignSubObject);
            message.Add(parent.NetworkID);
            message.Add((ushort)child.ListIndex);
            message.Add(child.NetworkID);

            if (clientID == -1)
                Manager.Instance.Core.Server.SendToAll(message);
            else
                Manager.Instance.Core.Server.Send(message, (ushort)clientID);
        }

        /// <summary>
        /// Call an RPC method on a single client.
        /// </summary>
        /// <param name="nwo">Networked object we want to invoke an RPC on remotely.</param>
        /// <param name="methodName">Name of the method to be called on clients machine.</param>
        /// <param name="clientID">Client's ID to send this RPC method.</param>
        /// <param name="parameters">List of parameters to forward to the RPC method.</param>
        public static void CallRPC(NetworkObject nwo, string methodName, ushort clientID, params object[] parameters)
        {
            Message message = Message.Create(MessageSendMode.Reliable, (ushort)PacketType.SailServerPacket.CallRPC);
            message.Add(nwo.NetworkID);
            message.Add(methodName);
            Reflector.SerializeRPC(ref message, in parameters);
            Manager.Instance.Core.Server.Send(message, (ushort)clientID);
        }

        /// <summary>
        /// Call an RPC method on all clients.
        /// </summary>
        /// <param name="nwo">Networked object we want to invoke an RPC on remotely.</param>
        /// <param name="methodName">Name of the method to be called on clients machine.</param>
        /// <param name="persistent">Should this RPC be sent to newly connected clients?</param>
        /// <param name="parameters">List of parameters to forward to the RPC method.</param>
        public static void CallRPCGlobal(NetworkObject nwo, string methodName, bool persistent, params object[] parameters)
        {
            Message message = Message.Create(MessageSendMode.Reliable, (ushort)PacketType.SailServerPacket.CallRPC);
            message.Add(nwo.NetworkID);
            message.Add(methodName);

            Logger.Log("Sending RPC Invoke ID" + nwo.NetworkID + " RPC METHOD: " + methodName);

            Reflector.SerializeRPC(ref message, in parameters);

            Manager.Instance.Measure.AddToMeasure(message.WrittenLength, (ushort)PacketType.SailServerPacket.CallRPC);

            //Logic depending on if message should be sent to new connecting clients or not
            if (persistent)
            {
                Manager.Instance.Core.AddPersistentMessage(nwo.NetworkID, (ushort)PacketType.SailServerPacket.CallRPC, message);
                Manager.Instance.Core.Server.SendToAll(message, false);
            }
            else
            {
                Manager.Instance.Core.Server.SendToAll(message);
            }
        }

        /// <summary>
        /// Update flags on all remote clients.
        /// </summary>
        /// <param name="nwo"></param>
        public static void UpdateFlags(NetworkObject nwo, int clientID = -1)
        {
            Message message = Message.Create(MessageSendMode.Reliable, (ushort)PacketType.SailServerPacket.UpdateFlags);
            message.Add(nwo.NetworkID);
            message.Add(nwo.Flags);

            if (clientID == -1)
                Manager.Instance.Core.Server.SendToAll(message);
            else
                Manager.Instance.Core.Server.Send(message, (ushort)clientID);
        }

    }
}
