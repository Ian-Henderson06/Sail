using Riptide;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Logger = INet.Util.Logger;

namespace Sail.Data
{
    /// <summary>
    /// Data class to tie a list of persistent messages to a particular network object.
    /// Used when updating new clients on the state of an existing object.
    /// </summary>
    public class NetworkObjectPersistentData
    {
        //Properties
        public List<Message> Messages { get { return _messages; } }
        public Dictionary<ushort, Message> LatestOfUniqueType { get { return _latestOfUniqueType; } }
        public int NetworkID { get { return _networkID; } }

        //Private variables
        private List<Message> _messages;
        private Dictionary<ushort, Message> _latestOfUniqueType; //List of the latest messages of each type.
        private int _maxMessages;
        private int _networkID;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="maxMessages">Maximum amount of messages to be stored for this object.</param>
        public NetworkObjectPersistentData(int networkID, int maxMessages)
        {
            _messages = new List<Message>();
            _maxMessages = maxMessages;
            _networkID = networkID;
            _latestOfUniqueType = new Dictionary<ushort, Message>();
        }

        /// <summary>
        /// Add a persistent message to the internal register.
        /// </summary>
        /// <param name="message"></param>
        public void AddMessage(ushort packetType, Message message)
        {
            //Overall persistent data count is greater than max we want to store
            if (_messages.Count + 1 > _maxMessages)
            {
                Logger.Log("Max persistent messages reached for object " + _networkID + ". Attempting to clear space.");
                int desiredSize = _messages.Count / 2;
                ClearMessages(desiredSize);
            }

            UpdateUniqueLatestOfType(packetType, message);
            _messages.Add(message);
        }

        /// <summary>
        /// Clear a set amount of messages from internal messages list.
        /// </summary>
        /// <param name="amount"></param>
        public void ClearMessages(int amount)
        {
            if (amount <= 0)
                return;

            //If we need to clear more than existing messages then clear them all.
            if (amount > _messages.Count)
                amount = _messages.Count - 1;

            for (int i = 0; i < amount; i++)
            {
                if (_messages[i] != null)
                {
                    _messages[i].Release();
                    _messages.RemoveAt(i);
                }
            }
        }

        /// <summary>
        /// Add or replace the latest message type.
        /// </summary>
        /// <param name="packetType"></param>
        /// <param name="message"></param>
        private void UpdateUniqueLatestOfType(ushort packetType, Message message)
        {
            if (_latestOfUniqueType.ContainsKey(packetType))
            {
                _latestOfUniqueType[packetType] = message;
            }
            else
            {
                _latestOfUniqueType.Add(packetType, message);
            }
        }
    }
}