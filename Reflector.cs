using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using System.Reflection;
using System.Threading.Tasks;

using Logger = Sail.Util.Logger;
using System;
using System.Linq;
using Sail.Util;
using Riptide;
using Sail.Data;

namespace Sail
{
    /// <summary>
    /// Used to handle and manage the capturing and accessing of all attribute methods.
    /// At the moment this is only RPC calls.
    /// </summary>
    public static class Reflector
    {
        //Public delegates
        public delegate void RPCDelegate(params object[] arguments); //Delegate for a void function with any number of parameters.

        //Public properties

        /// <summary>
        /// Fetch information on a types methods. 
        /// </summary>
        public static Dictionary<Type, MethodInfo[]> RPCMethods = new Dictionary<Type, MethodInfo[]>();

        /// <summary>
        /// Fetch id of a specific method given its name in a specified type to be used in RPCMethods index.
        /// </summary>
        public static Dictionary<Type, Dictionary<string, int>> RPCFindMethodIndex = new Dictionary<Type, Dictionary<string, int>>();

        //Private variables
        private static Issuer _issuer;
        private static HashSet<Type> AllowedTypes = new HashSet<Type>
        {
            typeof(byte), typeof(sbyte), typeof(short), typeof(ushort), typeof(int), typeof(uint), typeof(long), typeof(ulong), typeof(float), typeof(double), typeof(bool), typeof(string),
            typeof(Vector3), typeof(Quaternion), typeof(Vector2), typeof(double[]), typeof(float[]), typeof(bool[]), typeof(string[]), typeof(byte[])
        };

        /// <summary>
        /// Grab and load all RPC methods into Reflector.
        /// </summary>
        /// <returns></returns>
        public static Task CaptureRPCMethods()
        {
            Logger.Log("Attempting capture of RPC methods...");
            List<Type> types = GetAllClassesThatImplement<NetworkObject>();


            foreach (Type classType in types)
            {
                Logger.Log("TYPE FOUND " + classType);
                //Fetch all methods tagged as RPC in this class.
                MethodInfo[] methods = classType
                    .GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                    .Where(method => Attribute.IsDefined(method, typeof(RPCAttribute)))
                    .ToArray();

                Array.Sort(methods, delegate (MethodInfo x, MethodInfo y) { return String.Compare(x.Name, y.Name, StringComparison.InvariantCulture); });

                Logger.Log("Methods count " + methods.Length);

                Dictionary<string, int> MethodNameToID = new Dictionary<string, int>();
                int index = 0;

                //Loop through each RPC method in class.
                foreach (MethodInfo method in methods)
                {
                    ParameterInfo[] parameters = method.GetParameters();

                    //Check Parameters
                    foreach (ParameterInfo par in parameters)
                    {
                        //Not an allowed type
                        if (!AllowedTypes.Contains(par.ParameterType))
                        {
                            Logger.LogError($"Incompatible parameter used: {par.Name} typeof: {par.ParameterType}");
                            return Task.CompletedTask;
                        }
                    }

                    //Unique id for each method
                    MethodNameToID.Add(method.Name, index++);
                    Logger.Log($"Found RPC Method on type {classType} of name {method.Name}");
                }

                RPCFindMethodIndex.Add(classType, MethodNameToID);
                RPCMethods.Add(classType, methods);
            }

            return Task.CompletedTask;
        }




        /// <summary>
        /// Gets all classes and children of the given class.
        /// Inspired by TinyBirdNet
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        private static List<Type> GetAllClassesAndChildsOf<T>() where T : class
        {
            List<Type> types = new List<Type>();

            foreach (Type type in
                Assembly.GetAssembly(typeof(T)).GetTypes()
                    .Where(myType => myType.IsClass && !myType.IsAbstract && myType.IsSubclassOf(typeof(T))))
            {
                types.Add(type);
            }

            return types;
        }

        /// <summary>
        /// Gets all classes and children of the given class.
        /// Inspired by TinyBirdNet
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        private static List<Type> GetAllClassesThatImplement<T>()
        {
            List<Type> types = new List<Type>();
            var desiredType = typeof(T);

            //  var type = typeof(T);
            foreach (Type type in AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(s => s.GetTypes())
                .Where(p => desiredType.IsAssignableFrom(p)))
            {
                types.Add(type);
            }

            return types;
        }

        /// <summary>
        /// Fetch a method from a class given its type and method name.
        /// This is pre-loaded into dictionaries in reflector so it should be fairly fast.
        /// </summary>
        /// <param name="type"></param>
        /// <param name="methodName"></param>
        /// <returns></returns>
        public static MethodInfo GetMethod(Type type, string methodName)
        {
            MethodInfo[] methods = RPCMethods[type];
            int index = RPCFindMethodIndex[type][methodName];
            return methods[index];
        }

        public static void SerializeRPC(ref Message message, in object[] parameters)
        {
            //Serialize parameters
            foreach (object par in parameters)
            {
                if (par is byte)
                    message.AddByte((byte)par);
                else if (par is short)
                    message.AddShort((short)par);
                else if (par is ushort)
                    message.AddUShort((ushort)par);
                else if (par is int)
                    message.AddInt((int)par);
                else if (par is uint)
                    message.AddUInt((uint)par);
                else if (par is long)
                    message.AddLong((long)par);
                else if (par is ulong)
                    message.AddULong((ulong)par);
                else if (par is float)
                    message.AddFloat((float)par);
                else if (par is double)
                    message.AddDouble((double)par);
                else if (par is bool)
                    message.AddBool((bool)par);
                else if (par is string)
                    message.AddString((string)par);
                else if (par is Vector3)
                    message.AddVector3((Vector3)par);
                else if (par is Vector2)
                    message.AddVector2((Vector2)par);
                else if (par is Quaternion)
                    message.AddQuaternion((Quaternion)par);
                else if (par is double[])
                    message.AddDoubles((double[])par);
                else if (par is float[])
                    message.AddFloats((float[])par);
                else if (par is bool[])
                    message.AddBools((bool[])par);
                else if (par is string[])
                    message.AddStrings((string[])par);
                else if (par is byte[])
                    message.AddBytes((byte[])par);
                else
                {
                    Logger.LogError("Cant send RPC, as cant serialize specified type. Incompatible parameter type: " + par.GetType());
                    return;
                }
            }
        }

        public static void DeSerializeAndCallRPC(int networkID, string methodName, ref Message message)
        {
            Type targetType = Manager.Instance.NetworkedObjects[networkID].GetType();
            Debug.Log("RPC RECEIVED: TARGET TYPE " + targetType);
            MethodInfo methodInfo = GetMethod(targetType, methodName);
            ParameterInfo[] parameterInfos = methodInfo.GetParameters();
            object[] parameters = new object[parameterInfos.Length];

            for (int i = 0; i < parameterInfos.Length; i++)
            {
                ParameterInfo info = parameterInfos[i];

                if (info.ParameterType == typeof(byte))
                    parameters[i] = message.GetByte();
                else if (info.ParameterType == typeof(short))
                    parameters[i] = message.GetShort();
                else if (info.ParameterType == typeof(ushort))
                    parameters[i] = message.GetUShort();
                else if (info.ParameterType == typeof(int))
                    parameters[i] = message.GetInt();
                else if (info.ParameterType == typeof(uint))
                    parameters[i] = message.GetUInt();
                else if (info.ParameterType == typeof(long))
                    parameters[i] = message.GetLong();
                else if (info.ParameterType == typeof(ulong))
                    parameters[i] = message.GetULong();
                else if (info.ParameterType == typeof(float))
                    parameters[i] = message.GetFloat();
                else if (info.ParameterType == typeof(double))
                    parameters[i] = message.GetDouble();
                else if (info.ParameterType == typeof(bool))
                    parameters[i] = message.GetBool();
                else if (info.ParameterType == typeof(string))
                    parameters[i] = message.GetString();
                else if (info.ParameterType == typeof(Vector3))
                    parameters[i] = message.GetVector3();
                else if (info.ParameterType == typeof(Vector2))
                    parameters[i] = message.GetVector2();
                else if (info.ParameterType == typeof(Quaternion))
                    parameters[i] = message.GetQuaternion();
                else if (info.ParameterType == typeof(double[]))
                    parameters[i] = message.GetDoubles();
                else if (info.ParameterType == typeof(float[]))
                    parameters[i] = message.GetFloats();
                else if (info.ParameterType == typeof(bool[]))
                    parameters[i] = message.GetBools();
                else if (info.ParameterType == typeof(string[]))
                    parameters[i] = message.GetStrings();
                else if (info.ParameterType == typeof(byte[]))
                    parameters[i] = message.GetBytes();
                else
                    throw new System.Exception("DeSerialization for RPC is impossible - Incompatible parameter type: " + info.ParameterType.GetType());
            }

            methodInfo.Invoke(Manager.Instance.NetworkedObjects[networkID], parameters);
        }
    }
}