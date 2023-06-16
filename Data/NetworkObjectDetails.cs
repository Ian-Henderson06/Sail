using UnityEngine;

namespace Sail.Data
{
    /// <summary>
    /// Contains the internal database data about an object.
    /// </summary>
    [System.Serializable]
    public class NetworkObjectDetails
    {
        /// <summary>
        /// The ID the item is known as.
        /// </summary>
        public int ItemID;

        /// <summary>
        /// An items name.
        /// </summary>
        public string Name;

        /// <summary>
        /// Reference to an items prefab.
        /// </summary>
        public GameObject Prefab;

    }
}
