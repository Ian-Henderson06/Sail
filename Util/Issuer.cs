using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sail.Util
{
    /// <summary>
    /// Used to issue unique values.
    /// This is typically only used on the servers end.
    /// </summary>
    public class Issuer
    {
        //Private fields
        private int _nextID = 1;

        /// <summary>
        /// Request a unique ID.
        /// </summary>
        /// <returns></returns>
        public int RequestIssue()
        {
            return _nextID++;
        }
    }
}
