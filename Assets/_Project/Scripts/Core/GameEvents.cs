using System;
using UnityEngine;

namespace MistProtocol.Core
{
    public enum AgentKind { HunterDrone, Sentinel, Saboteur }

    /// <summary>
    /// Global event bus. Gameplay systems publish here so the AI, UI and
    /// director never hold hard references to each other.
    /// </summary>
    public static class GameEvents
    {
        public static event Action<string> OverseerMessage;
        public static event Action<string> ObjectiveChanged;
        public static event Action<AgentKind, Vector3> AgentKilled;
        public static event Action<float> PlayerDamaged;
        public static event Action PlayerShotFired;
        public static event Action<bool> PlayerShotHit;
        public static event Action PhysicsInteraction;
        public static event Action FinalWaveStarted;
        public static event Action<string> ZoneEntered;

        public static void Overseer(string line) => OverseerMessage?.Invoke(line);
        public static void Objective(string text) => ObjectiveChanged?.Invoke(text);
        public static void RaiseAgentKilled(AgentKind kind, Vector3 pos) => AgentKilled?.Invoke(kind, pos);
        public static void RaisePlayerDamaged(float amount) => PlayerDamaged?.Invoke(amount);
        public static void RaiseShotFired() => PlayerShotFired?.Invoke();
        public static void RaiseShotHit(bool hitAgent) => PlayerShotHit?.Invoke(hitAgent);
        public static void RaisePhysicsInteraction() => PhysicsInteraction?.Invoke();
        public static void RaiseFinalWave() => FinalWaveStarted?.Invoke();
        public static void RaiseZone(string name) => ZoneEntered?.Invoke(name);

        /// <summary>Static events survive domain-reload-less play mode; clear them on scene start.</summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reset()
        {
            StaticReset.Register(Reset);
            OverseerMessage = null; ObjectiveChanged = null; AgentKilled = null; PlayerDamaged = null;
            PlayerShotFired = null; PlayerShotHit = null; PhysicsInteraction = null; FinalWaveStarted = null; ZoneEntered = null;
        }
    }
}
