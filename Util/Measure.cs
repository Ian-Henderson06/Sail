using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Logger = Sail.Util.Logger;

namespace Sail
{
    /// <summary>
    /// Used to measure outgoing bytes.
    /// This is a rough estimate.
    /// </summary>
    public class Measure
    {
        //Private fields

        /// <summary>
        /// Lists the byte count for each packet type
        /// </summary>
        private Dictionary<ushort, int> _bytes;
     
        public Measure()
        {
            _bytes = new Dictionary<ushort, int>();
        }

        /// <summary>
        /// Add to bytes list
        /// </summary>
        /// <param name="bytes"></param>
        /// <param name="packetType"></param>
        public void AddToMeasure(int bytes, ushort packetType)
        {
            if(_bytes.ContainsKey(packetType))
            {
                int oldBytes = _bytes[packetType];
                _bytes[packetType] = oldBytes + bytes;
            }
            else
            {
                _bytes.Add(packetType, bytes);
            }
        }

        /// <summary>
        /// Reset the measurement.
        /// </summary>
        public void ClearMeasure()
        {
            _bytes.Clear();
        }

        /// <summary>
        /// Print measurement.
        /// </summary>
        public void PrintMeasure()
        {
            Logger.Log("MEASURE RESULTS");
            int total = 0;
            int keyOfLargest = 0;
            int largest = 0;
            foreach (KeyValuePair<ushort, int> pair in _bytes)
            {
                if(NetworkManager.Instance.Core.IsServer())
                    Logger.Log($"MEASURE: Packet {Enum.GetName(typeof(PacketType.ServerPacket), pair.Key)} Total: {pair.Value} bytes per tick.");
                else
                    Logger.Log($"MEASURE: Packet {Enum.GetName(typeof(PacketType.ClientPacket), pair.Key)} Total: {pair.Value} bytes per tick.");

                if(pair.Value > largest)
                {
                    largest = pair.Value;
                    keyOfLargest = pair.Key;
                }

                total += pair.Value;
            }
            Logger.Log($"TOTAL: {total} bytes per tick.");

              if(NetworkManager.Instance.Core.IsServer())
                    Logger.Log($"LARGEST is: {Enum.GetName(typeof(PacketType.ServerPacket), keyOfLargest)} with total size of {largest} bytes per tick.");
                else
                     Logger.Log($"LARGEST is: {Enum.GetName(typeof(PacketType.ClientPacket), keyOfLargest)} with total size of {largest} bytes per tick.");
           
            Logger.Log("END MEASURE");
        }
    }
}
