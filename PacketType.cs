using System.Collections;
using System.Collections.Generic;

using UnityEngine;

namespace Sail
{
    /// <summary>
    /// All packets used in the game.
    /// Server or Client denotes the origin of the packet, not the destination.
    /// </summary>
    public class PacketType
    {
        public enum ServerPacket
        {
            Sync = 1,
            SpawnPlayer,
            DestroyPlayer,
            UpdatePlayer,

            SpawnObject,
            DestroyObject,
            UpdateObject,

            UpdateAuthorityObject,

            AssignSubObject,

            CallRPC,
            UpdateFlags,
        }

        public enum ClientPacket
        {
            PlayerInformation = 1,
            RequestAuthorityObject,
            RequestUpdateObject,
            RequestEquip,
            Activate
        }
    }
}
