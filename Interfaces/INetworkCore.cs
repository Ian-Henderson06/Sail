using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Riptide;

namespace Sail
{
    public interface INetworkCore
    {
        /// <summary>
        /// Get a reference to the appropriate client or server object.
        /// </summary>
        /// <returns></returns>
        public Peer GetPeer();

        /// <summary>
        /// Initialise core, connecting any events and setting any variables that are required before startup.
        /// </summary>
        public void InitialiseCore();
    }

}
