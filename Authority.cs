using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Sail.Util;

namespace Sail
{
    public enum ClientAuthorityType
    {
        None = 0,
        Input,
        Full
    }

    /// <summary>
    /// Keeps a record of any set object authority levels.
    /// </summary>
    sealed internal class Authority
    {
        //Every player has net objects at levels of authority
        private static Dictionary<int, int> objectsWithClientInputAuthority = new Dictionary<int, int>();
        private static Dictionary<int, int> objectsWithClientFullAuthority = new Dictionary<int, int>();

        /// <summary>
        /// Register or update a clients authority with the authority system.
        /// Objects can only have one owner at one level of authority.
        /// </summary>
        /// <param name="clientID"></param>
        /// <param name="networkID"></param>
        /// <param name="authorityLevel"></param>
        public static void RegisterClientAuthority(int networkID, int clientID, ClientAuthorityType authorityLevel)
        {
            //Multiple authorities detected
            if (GetAuthority(networkID) != ClientAuthorityType.None)
            {
                //If existing equals requested
                if(GetAuthority(networkID) == authorityLevel)
                    return;
                else
                    UnregisterClientAuthority(networkID);
            }

            if(authorityLevel == ClientAuthorityType.Full)
                objectsWithClientFullAuthority.Add(networkID, clientID);
            
            if(authorityLevel == ClientAuthorityType.Input)
                objectsWithClientInputAuthority.Add(networkID, clientID);
            
            if(authorityLevel == ClientAuthorityType.None)
                UnregisterClientAuthority(networkID);
        }

        /// <summary>
        /// Unregister a clients authority.
        /// </summary>
        /// <param name="networkID"></param>
        public static void UnregisterClientAuthority(int networkID)
        {
            if (objectsWithClientFullAuthority.ContainsKey(networkID))
                objectsWithClientFullAuthority.Remove(networkID);
            
            if (objectsWithClientInputAuthority.ContainsKey(networkID))
                objectsWithClientInputAuthority.Remove(networkID);
        }

        /// <summary>
        /// Get the client that owns this authority.
        /// </summary>
        /// <param name="networkID"></param>
        /// <returns></returns>
        public static int? GetAuthorityOwner(int networkID)
        {
            int? owner = null;

            if (objectsWithClientInputAuthority.ContainsKey(networkID))
                owner = objectsWithClientInputAuthority[networkID];
            
            if (objectsWithClientFullAuthority.ContainsKey(networkID))
                owner = objectsWithClientFullAuthority[networkID];

            return owner;
        }

        /// <summary>
        /// Get the authority level for an object.
        /// </summary>
        /// <param name="networkID"></param>
        /// <returns></returns>
        public static ClientAuthorityType GetAuthority(int networkID)
        {
            ClientAuthorityType type = ClientAuthorityType.None;

            if (objectsWithClientInputAuthority.ContainsKey(networkID))
                type = ClientAuthorityType.Input;
            
            if (objectsWithClientFullAuthority.ContainsKey(networkID))
                type = ClientAuthorityType.Full;

            return type;
        }
    }
}