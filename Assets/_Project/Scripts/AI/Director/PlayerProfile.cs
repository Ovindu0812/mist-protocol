using MistProtocol.Core;
using UnityEngine;

namespace MistProtocol.AI.Director
{
    /// <summary>
    /// Exponentially-weighted running statistics of how the player plays.
    /// Every metric is an EMA with time constant tau: m += (sample - m) * (1 - e^(-dt/tau)),
    /// so the director reacts to the last ~30 s of behaviour, not the whole session.
    /// </summary>
    public class PlayerProfile
    {
        private const float Tau = 30f;
        public float Camping { get; private set; }      // fraction of time standing still
        public float Stealth { get; private set; }      // fraction of time crouched
        public float Accuracy { get; private set; } = 0.3f;
        public float PhysicsUse { get; private set; }   // physics interactions per minute (normalised)
        public float Aggression { get; private set; }   // shots per second (normalised)

        private int _shots, _hits, _physics;

        public PlayerProfile()
        {
            GameEvents.PlayerShotFired += () => _shots++;
            GameEvents.PlayerShotHit += hit => { if (hit) _hits++; };
            GameEvents.PhysicsInteraction += () => _physics++;
        }

        public void Tick(float dt)
        {
            if (!PlayerLocator.Alive) return;
            var p = PlayerLocator.State;
            float k = 1f - Mathf.Exp(-dt / Tau);
            Camping += ((p.Velocity.Flat().magnitude < 0.4f ? 1f : 0f) - Camping) * k;
            Stealth += ((p.IsCrouching ? 1f : 0f) - Stealth) * k;
            Aggression += (Mathf.Clamp01(_shots / dt / 6f) - Aggression) * k;
            PhysicsUse += (Mathf.Clamp01(_physics / dt * 60f / 6f) - PhysicsUse) * k;
            if (_shots > 0) Accuracy += ((float)_hits / _shots - Accuracy) * Mathf.Min(1f, k * _shots * 3f);
            _shots = _hits = _physics = 0;
        }

        public override string ToString() =>
            $"camp {Camping:0.00} stealth {Stealth:0.00} acc {Accuracy:0.00} phys {PhysicsUse:0.00} aggr {Aggression:0.00}";
    }
}
