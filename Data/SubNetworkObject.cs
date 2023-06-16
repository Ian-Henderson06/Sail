using System.Collections;
using UnityEngine;

namespace Sail.Data
{
    /// <summary>
    /// Child network objects.
    /// These objects have reduced functionality,
    /// but allow child objects to be networked.
    /// </summary>
    public class SubNetworkObject : NetworkObject
    {
        //Public properties
        public int ParentNetworkID { get { return _parentNetworkID; } }
        public int ListIndex { get { return _listIndex; } }

        //Private fields
        private int _parentNetworkID;
        private int _listIndex;

        public void InitializeSubObject(int parentNetworkID, int listIndex)
        {
            _parentNetworkID = parentNetworkID;
            _listIndex = listIndex;
        }
    }
}