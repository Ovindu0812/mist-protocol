using System;
using System.Collections.Generic;

namespace MistProtocol.Core
{
    /// <summary>
    /// Static registries/events must not outlive their scene: a restart reloads the
    /// scene and old subscribers would point at destroyed objects. Every static
    /// registers its reset here, and the game manager runs them all right before
    /// reloading (deterministic, unlike relying on sceneUnloaded ordering).
    /// </summary>
    public static class StaticReset
    {
        private static readonly List<Action> Resets = new();

        public static void Register(Action reset)
        {
            if (!Resets.Contains(reset)) Resets.Add(reset);
        }

        public static void RunAll()
        {
            foreach (var r in Resets.ToArray()) r();
        }
    }
}
