using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Riptide;
using Riptide.Utils;


using Logger = INet.Util.Logger;

namespace INet
{
    /// <summary>
    /// Shared across client and server.
    /// Gateway to the networking system and allows access to players as well as the main network object.
    /// </summary>
    [RequireComponent(typeof(NetworkCore))]
    public class NetworkManager : MonoBehaviour
    {
        //Properties
        public Peer Network { get; private set; }
        public NetworkCore Core { get {return _core; } }
        public Dictionary<ushort, NetworkPlayer> Players { get; private set; }
        public Dictionary<int, NetworkObject> NetworkedObjects { get; private set; }
        public TimeManager TimeManager { get { return _timeManager; } }
        public Measure Measure { get{ return _measure; } }

        //Events
        public event Action OnTick;
        public event Action OnPostTick;

        //Serialized private fields
        [SerializeField] private float _ticksPerSecond;
        [SerializeField] private bool _measurePackets;

        //Private fields
        private TimeManager _timeManager;
        private NetworkCore _core;
        private Measure _measure;
        private float _tickRate;

        //Singleton
        private static NetworkManager _instance;
        public static NetworkManager Instance
        {
            get => _instance;
            private set
            {
                if (_instance == null)
                    _instance = value;
                else if (_instance != value)
                {
                    Logger.Log($"{nameof(NetworkManager)} instance already exists, destroying object!");
                    Destroy(value);
                }
            }
        }

        private void Awake()
        {
            #if UNITY_EDITOR
            RiptideLogger.Initialize(Debug.Log, Debug.Log, Debug.LogWarning, Debug.LogError, true);
#else
                    RiptideLogger.Initialize(Debug.Log, true);
#endif

            DontDestroyOnLoad(this);
            Instance = this;

            Players = new Dictionary<ushort, NetworkPlayer>();
            NetworkedObjects = new Dictionary<int, NetworkObject>();

            _tickRate = 1 / _ticksPerSecond;

            _timeManager = new TimeManager();
            _timeManager.SetupTimeManager(_tickRate);
            _timeManager.OnTick += Tick; //Bind local OnTick method to the time managers OnTick method.
            

            _measure = new Measure();
            OnPostTick += () => {
                if (_measurePackets)
                {
                    _measure.PrintMeasure();
                }
                _measure.ClearMeasure();
            };
           

            Logger.Log($"Initialising INet with a tick rate of {_tickRate}ms estimated {_ticksPerSecond} ticks per second.");

            if(_core == null) { 
                _core = gameObject.GetComponent<NetworkCore>();
                _core.InitialiseCore();
            }

            //These methods are only used on the client.
            Reflector.CaptureRPCMethods();
        }

        private void Tick()
        {
            OnTick?.Invoke();
            OnPostTick?.Invoke();
        }

        private void OnDestroy()
        {
            _timeManager.OnTick -= Tick;
        }

        private void Update()
        {
            _timeManager.UpdateTimeManager(Time.deltaTime);
        }

        /// <summary>
        /// Adds a reference of a player to the manager.
        /// This is called from the NetworkCore.
        /// </summary>
        /// <param name="playerID"></param>
        /// <param name="player"></param>
        public void AddPlayer(NetworkPlayer player)
        {
            if (Players.TryGetValue(player.PlayerID, out _))
            {
                Logger.LogError("Player already exists and cannot be added to the NWM Players dictionary.");
                return;
            }

            Players.Add(player.PlayerID, player);
        }

        /// <summary>
        /// Removes a reference of a player to the manager.
        /// This is called from the NetworkCore.
        /// </summary>
        /// <param name="playerID"></param>
        /// <param name="player"></param>
        public void RemovePlayer(NetworkPlayer player)
        {
            if (!Players.TryGetValue(player.PlayerID, out _))
            {
                Logger.LogError("Player cannot be removed as they don't exist in NWM dictionary.");
                return;
            }

            Players.Remove(player.PlayerID);
        }

        /// <summary>
        /// Adds a reference to a networked object to the internal register.
        /// </summary>
        /// <param name="nwo"></param>
        public void AddNetworkObject(NetworkObject nwo)
        {
            if (NetworkedObjects.TryGetValue(nwo.NetworkID, out _))
            {
                Logger.LogError("Networked Object already exists and cannot be added to the register.");
                return;
            }

            NetworkedObjects.Add(nwo.NetworkID, nwo);
        }

        /// <summary>
        /// Removes a reference to a networked object from the internal register.
        /// </summary>
        /// <param name="playerID"></param>
        /// <param name="player"></param>
        public void RemoveNetworkObject(NetworkObject nwo)
        {
            if (!NetworkedObjects.TryGetValue(nwo.NetworkID, out _))
            {
                Logger.LogError("Network Object cannot be removed as it doesn't exist in the register.");
                return;
            }

            NetworkedObjects.Remove(nwo.NetworkID);
        }
    }
}