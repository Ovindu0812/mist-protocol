using System;
using System.Collections.Generic;
using MistProtocol.AI.Navigation;

namespace MistProtocol.AI.Sentinel
{
    /// <summary>World-state facts as bits of a ulong, so states hash and compare in O(1).</summary>
    [Flags]
    public enum Fact : ulong
    {
        None = 0,
        KnowsPlayer = 1 << 0,
        PlayerVisible = 1 << 1,
        InCover = 1 << 2,
        WeaponLoaded = 1 << 3,
        PlayerDamaged = 1 << 4,
        AreaSearched = 1 << 5,
        SquadAlerted = 1 << 6,
        Patrolled = 1 << 7,
        LowHealth = 1 << 8,
        HasLead = 1 << 9,
    }

    public enum ActionStatus { Running, Success, Failure }

    /// <summary>A GOAP action: symbolic preconditions/effects for planning, plus procedural Tick for execution.</summary>
    public abstract class GoapAction
    {
        public abstract string Name { get; }
        public Fact PreTrue, PreFalse, EffTrue, EffFalse;

        public bool Applicable(Fact s) => (s & PreTrue) == PreTrue && (s & PreFalse) == 0;
        public Fact Apply(Fact s) => (s | EffTrue) & ~EffFalse;

        /// <summary>Context-dependent cost (distance, danger, squad role...). Evaluated at plan time.</summary>
        public virtual float Cost(SentinelBrain b) => 1f;
        /// <summary>Procedural precondition the symbolic state can't express (e.g. a cover point exists).</summary>
        public virtual bool IsValid(SentinelBrain b) => true;
        public virtual void Enter(SentinelBrain b) { }
        public abstract ActionStatus Tick(SentinelBrain b, float dt);
        public virtual void Exit(SentinelBrain b) { }
    }

    public struct GoapGoal
    {
        public string Name;
        public Fact Want;
        public Fact WantFalse;
        public bool SatisfiedBy(Fact s) => (s & Want) == Want && (s & WantFalse) == 0;
        public int Unsatisfied(Fact s)
        {
            ulong miss = ((ulong)Want & ~(ulong)s) | ((ulong)WantFalse & (ulong)s);
            int c = 0;
            while (miss != 0) { miss &= miss - 1; c++; }
            return c;
        }
    }

    /// <summary>
    /// Forward A* over the symbolic state space.
    ///   node = world state, edge = applicable action, g = sum of action costs,
    ///   h = number of unsatisfied goal facts.
    /// h is admissible only while every action costs >= 1 and fixes one goal fact;
    /// our costs are all >= 1 except rare discounts, so plans are optimal or very
    /// close to it. The state space is tiny (10 facts -> at most 1024 states), so the
    /// search is capped at a few hundred nodes and runs in well under 0.1 ms.
    /// </summary>
    public static class GoapPlanner
    {
        private struct Node { public Fact State; public float G; public int Parent; public GoapAction Action; }

        private static readonly List<Node> Nodes = new(256);
        private static readonly MinHeap Open = new();
        private static readonly Dictionary<Fact, float> Best = new();

        public static int LastNodesExpanded { get; private set; }

        public static bool Plan(Fact start, GoapGoal goal, IReadOnlyList<GoapAction> actions, SentinelBrain ctx,
            List<GoapAction> plan, int maxNodes = 400)
        {
            plan.Clear();
            Nodes.Clear(); Open.Clear(); Best.Clear();
            if (goal.SatisfiedBy(start)) return true;

            // cache costs/validity once per plan: they depend on the real world, not the symbolic state
            var costs = new float[actions.Count];
            var valid = new bool[actions.Count];
            for (int i = 0; i < actions.Count; i++)
            {
                valid[i] = actions[i].IsValid(ctx);
                costs[i] = valid[i] ? Math.Max(0.1f, actions[i].Cost(ctx)) : 0f;
            }

            Nodes.Add(new Node { State = start, G = 0, Parent = -1 });
            Best[start] = 0;
            Open.Push(0, goal.Unsatisfied(start));
            int expanded = 0;
            while (Open.Count > 0 && expanded < maxNodes)
            {
                int ni = Open.Pop(out _);
                var n = Nodes[ni];
                if (Best.TryGetValue(n.State, out float bg) && n.G > bg) continue;
                expanded++;
                if (goal.SatisfiedBy(n.State))
                {
                    for (int c = ni; Nodes[c].Parent >= 0; c = Nodes[c].Parent) plan.Add(Nodes[c].Action);
                    plan.Reverse();
                    LastNodesExpanded = expanded;
                    return true;
                }
                for (int a = 0; a < actions.Count; a++)
                {
                    if (!valid[a] || !actions[a].Applicable(n.State)) continue;
                    Fact next = actions[a].Apply(n.State);
                    if (next == n.State) continue;
                    float g = n.G + costs[a];
                    if (Best.TryGetValue(next, out float old) && g >= old) continue;
                    Best[next] = g;
                    Nodes.Add(new Node { State = next, G = g, Parent = ni, Action = actions[a] });
                    Open.Push(Nodes.Count - 1, g + goal.Unsatisfied(next));
                }
            }
            LastNodesExpanded = expanded;
            return false;
        }
    }
}
