using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace Sail.Data
{
    /// <summary>
    /// Data class to store all information about a networked object
    /// </summary>
    public class NetworkObject : MonoBehaviour
    {
        //Public properties
        public int NetworkID { get { return _networkID; } }
        public int ItemID { get { return _itemID; } }
        public int AuthorityID { get { return _authorityID; } }
        public byte Flags { get { return _flags; } }
        public SubNetworkObject[] SubObjects { get { return _subObjects.ToArray(); } }
        public bool ShouldSync { get { return _shouldSync; } }
        public bool IgnoreIncomingSync { get { return _ignoreIncomingSync; } }

        public delegate void NetworkObjectEvent(NetworkObject nwo);

        //Public events
        /// <summary>
        /// Called when this object is initialised and its NetworkID is set.
        /// </summary>
        public event Action OnNetworkIDChanged;

        /// <summary>
        /// Called when this objects authority has been changed.
        /// </summary>
        public event Action OnAuthorityChanged;

        /// <summary>
        /// Called when any flags are changed on the object.
        /// </summary>
        public event Action OnFlagsChanged;

        //Private serializable fields
        [SerializeField] private List<SubNetworkObject> _subObjects = new List<SubNetworkObject>();
        [SerializeField] private bool _shouldSync; //Client/Server will check this boolean before sending over position/rotation updates even if they have authority

        [SerializeField] private bool _ignoreIncomingSync = false; //Make local object ignore any updates coming in from the server/client.

        //Private fields
        private int _networkID;
        private int _itemID;
        private int _authorityID;
        private byte _flags;

        public void InitialiseObject(int networkID, int itemID)
        {
            _networkID = networkID;
            _itemID = itemID;
            _authorityID = int.MaxValue;
            _flags = 0b_0000_0000;

            OnNetworkIDChanged?.Invoke();
        }

        /// <summary>
        //Set a new owner on the object.
        /// </summary>
        public void SetAuthority(int ownerID)
        {
            _authorityID = ownerID;
            OnAuthorityChanged?.Invoke();
        }

        /// <summary>
        /// Set the flags on this object.
        /// </summary>
        /// <param name="flags"></param>
        public void SetFlags(byte flags)
        {
            _flags = flags;
            OnFlagsChanged?.Invoke();
        }

        /// <summary>
        /// Should this object sync its position/rotation if local has authority?
        /// </summary>
        /// <param name="shouldSync"></param>
        public void SetShouldSync(bool shouldSync)
        {
            _shouldSync = shouldSync;
        }

        /// <summary>
        /// Should this object ignore incoming updates?
        /// </summary>
        /// <param name="shouldSync"></param>
        public void SetShouldIgnoreSync(bool shouldIgnoreSync)
        {
            _ignoreIncomingSync = shouldIgnoreSync;
        }
    }
}