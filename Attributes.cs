using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Sail
{
    /// <summary>
    /// Marks a method has remotely callable by the server.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
    public class RPCAttribute : Attribute
    {}
}