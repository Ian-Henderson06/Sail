using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using UnityEngine;
using Logger = Sail.Util.Logger;


namespace Sail.Core.Client
{
    public class ClientFunctions
    {
        public virtual void OnUpdatePlayer(ushort playerID, Vector3 newPosition, Quaternion newRotation)
        {
            Manager.Instance.Players[playerID].transform.position = newPosition;
            Manager.Instance.Players[playerID].transform.rotation = newRotation;
        }

        public virtual void OnUpdateNetworkObject(int networkID, Vector3 newPosition, Quaternion newRotation)
        {
            Manager.Instance.NetworkedObjects[networkID].transform.position = newPosition;
            Manager.Instance.NetworkedObjects[networkID].transform.rotation = newRotation;
        }
    }
}