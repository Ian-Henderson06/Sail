using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Sail
{
    public interface IActivateable
    {
        /// <summary>
        /// Called locally when activated.
        /// Clients prediction function.
        /// </summary>
        public void LocalActivate();
    }

}
