using System.Collections;
using System.Collections.Generic;

using UnityEngine;

namespace Sail
{
    /// <summary>
    /// Holds a reference to all possible prefabs that can be spawned.
    /// </summary>
    public class NetworkObjectList : MonoBehaviour
    {
        //Properties
        public List<NetworkObjectDetails> ObjectDatabase = new List<NetworkObjectDetails>();

        //Singleton
        private static NetworkObjectList _instance;
        public static NetworkObjectList Instance
        {
            get => _instance;
            private set
            {
                if (_instance == null)
                    _instance = value;
                else if (_instance != value)
                {
                    Debug.Log($"{nameof(NetworkObjectList)} instance already exists, destroying object!");
                    Destroy(value);
                }
            }
        }

        private void Awake()
        {
            DontDestroyOnLoad(this);
            Instance = this;
        }

        public NetworkObjectDetails FindObject(int itemID)
        {
            NetworkObjectDetails retDetails = null;
            foreach(NetworkObjectDetails details in ObjectDatabase)
            {
                if(details.ItemID == itemID)
                {
                    retDetails = details;
                    break;
                } 
            }

            return retDetails;
        }
     
    }
}