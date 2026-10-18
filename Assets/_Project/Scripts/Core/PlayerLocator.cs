using UnityEngine;

namespace MistProtocol.Core
{
    /// <summary>What the AI is allowed to know about the player. AI code depends on this
    /// interface only, never on the concrete player scripts.</summary>
    public interface IPlayerState
    {
        Transform Root { get; }
        Vector3 EyePosition { get; }
        Vector3 AimDirection { get; }
        Vector3 Velocity { get; }
        bool IsCrouching { get; }
        bool IsSprinting { get; }
        bool IsReloading { get; }
        bool IsHoldingObject { get; }
        Health Health { get; }
    }

    public static class PlayerLocator
    {
        public static IPlayerState State { get; private set; }
        public static bool Alive => State != null && State.Root != null && !State.Health.IsDead;

        public static void Register(IPlayerState state) => State = state;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reset() { State = null; StaticReset.Register(Reset); }
    }
}
