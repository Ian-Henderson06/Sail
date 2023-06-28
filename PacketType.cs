using System.Collections;
using System.Collections.Generic;

using UnityEngine;

namespace Sail
{
    /// <summary>
    /// All internal packets used in Sail.
    /// Server or Client denotes the origin of the packet, not the destination.
    /// </summary>
    sealed internal class PacketType
    {
        public enum SailServerPacket
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
            CallStaticRPC,
            UpdateFlags,
        }

        public enum SailClientPacket
        {
            PlayerInformation = 1,
            RequestAuthorityObject,
            RequestUpdateObject,
            RequestEquip,
            Activate
        }
    }
}
