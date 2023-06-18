using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;

namespace Sail
{
    /// <summary>
    /// Ensures ticks are ran at the same time across multiple machines.
    /// To be used to ensure the network step is in sync.
    /// </summary>
    public class TimeManager
    {
        private bool _isTicking = false;
        private float _tickRate = 0.2f;
        private float _tickTimer = 0;
        private uint _currentTick = 0;

        /// <summary>
        /// Current local tick.
        /// </summary>
        public uint CurrentTick { get { return _currentTick; } set { Debug.Log($"Time Manager tick updated: {_currentTick}->{value}"); _currentTick = value; } }

        /// <summary>
        /// Get current tick rate.
        /// </summary>
        public float TickRate { get { return _tickRate; } }

        /// <summary>
        /// Called on every tick by the time manager.
        /// </summary>
        public event Action OnTick;

        /// <summary>
        /// Sets up the time manager, running at the specified tick rate.
        /// </summary>
        /// <param name="tickrate"></param>
        public void SetupTimeManager(float tickrate)
        {
            _tickRate = tickrate;
            _isTicking = true;
        }

        /// <summary>
        /// Update the time manager. This should be called in a Unity update method.
        /// </summary>
        public void UpdateTimeManager(float deltaTime)
        {
            if (!_isTicking) return;

            //Update loop just removes tick rate from the timer rather than setting it in order to keep
            //any milisecond values relevant.
            _tickTimer += deltaTime;
            while (_tickTimer >= _tickRate)
            {
                _tickTimer -= _tickRate;
                _currentTick++;
                OnTick?.Invoke();
            }
        }
    }
}
