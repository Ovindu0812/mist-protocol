using System.Collections.Generic;
using UnityEngine;

namespace MistProtocol.AI.Sentinel
{
    /// <summary>
    /// Shared blackboard for Sentinels. Lets individual GOAP planners coordinate
    /// without a central commander: action costs read the squad's current roles
    /// (e.g. flanking gets cheaper once someone else is already suppressing).
    /// </summary>
    public static class SentinelSquad
    {
        public static readonly List<SentinelBrain> Members = new();
        private static readonly List<(Vector3 pos, SentinelBrain owner)> Claims = new();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reset() { Members.Clear(); Claims.Clear(); Core.StaticReset.Register(Reset); }

        public static void Join(SentinelBrain b) { if (!Members.Contains(b)) Members.Add(b); }
        public static void Leave(SentinelBrain b) { Members.Remove(b); ReleaseCover(b); }

        public static int CountDoing(string actionName, SentinelBrain except = null)
        {
            int c = 0;
            foreach (var m in Members)
                if (m != except && m.CurrentActionName == actionName) c++;
            return c;
        }

        public static void Alert(SentinelBrain caller, Vector3 playerPos, float radius = 30f)
        {
            foreach (var m in Members)
                if (m != caller && Vector3.Distance(m.transform.position, caller.transform.position) < radius)
                    m.ReceiveAlert(playerPos);
        }

        public static bool IsCoverClaimed(Vector3 p, SentinelBrain asker)
        {
            foreach (var c in Claims)
                if (c.owner != asker && c.owner != null && (c.pos - p).sqrMagnitude < 2.25f) return true;
            return false;
        }

        public static void ClaimCover(SentinelBrain b, Vector3 p)
        {
            ReleaseCover(b);
            Claims.Add((p, b));
        }

        public static void ReleaseCover(SentinelBrain b) => Claims.RemoveAll(c => c.owner == b || c.owner == null);
    }
}
