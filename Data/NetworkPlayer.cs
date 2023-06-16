using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Sail.Data
{
    /// <summary>
    /// Component thats placed on a players gameobject to hold essential data on the player.
    /// It is the in world representation of a players connection.
    /// Should be overwritten for client or server implementation.
    /// </summary>
    [RequireComponent(typeof(NetworkObject))]
    public class NetworkPlayer : MonoBehaviour
    {
        //Public properties
        public string Username { get { return _username; } }
        public ushort PlayerID { get { return _playerID; } }
        public int NetworkID { get { return _networkID; } }

        //Private fields
        private string _username = "NULL";
        private ushort _playerID = 9999;
        private int _networkID = 0;

        public void InitialisePlayer(string username, ushort playerID, int networkID)
        {
            _username = username;
            _playerID = playerID;
            _networkID = networkID;
        }
    }
}