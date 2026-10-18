using MistProtocol.AI.Navigation;
using MistProtocol.Core;
using MistProtocol.Interaction;
using UnityEngine;

namespace MistProtocol.AI.Saboteur
{
    public sealed class RestoreBarricadeAction : UtilityAction
    {
        public override string Name => "RestoreBarricade";
        private Barricade _target;
        private bool _carrying;

        private static Barricade[] _all;
        private static Barricade[] All => _all ??= Object.FindObjectsByType<Barricade>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reset() { _all = null; StaticReset.Register(Reset); }

        private Barricade Best(SaboteurBrain b, out float score)
        {
            Barricade best = null; score = 0f;
            foreach (var c in All)
            {
                if (c == null || c.IsCarried || (c.ClaimedBy != null && c.ClaimedBy != b.transform)) continue;
                float displaced = Curves.Logistic(c.Displacement / 4f, 0.35f, 9f);
                if (c.Displacement < 1f) continue;
                float near = Curves.Inverse(Vector3.Distance(b.transform.position, c.transform.position) / 35f);
                float s = displaced * (0.4f + 0.6f * near);
                if (s > score) { score = s; best = c; }
            }
            return best;
        }

        public override float Score(SaboteurBrain b)
        {
            if (_carrying) return 0.9f; // finish the job once committed
            var c = Best(b, out float s);
            if (c == null) return 0f;
            return Combine(s, 1f - b.Threat() * 0.8f) * 0.85f;
        }

        public override void Enter(SaboteurBrain b)
        {
            _target = Best(b, out _);
            if (_target != null) _target.ClaimedBy = b.transform;
            _carrying = false;
            b.Mover.MaxSpeed = b.BaseSpeed;
            b.Mover.SetLookTarget(null);
        }

        public override bool Tick(SaboteurBrain b, float dt)
        {
            if (_target == null) return true;
            if (!_carrying)
            {
                if (!b.Mover.MoveTo(_target.transform.position)) return true;
                if (Vector3.Distance(b.transform.position.Flat(), _target.transform.position.Flat()) < 1.4f)
                {
                    b.Grab(_target);
                    _carrying = true;
                    ProceduralAudio.PlayAt("impact", b.transform.position, 0.5f, 0.6f);
                }
                return false;
            }
            if (b.Carrying == null) { _carrying = false; return true; } // dropped (shot)
            Vector3 home = _target.HomePosition;
            Vector3 approach = home - (home - b.transform.position).Flat().normalized * 0.9f;
            if (!b.Mover.MoveTo(approach)) { b.DropCarried(b.transform.position + b.transform.forward * 0.9f); _carrying = false; return true; }
            if (b.Mover.Arrived || Vector3.Distance(b.transform.position.Flat(), home.Flat()) < 1.3f)
            {
                b.DropCarried(home);
                _carrying = false;
                return true;
            }
            return false;
        }

        public override void Exit(SaboteurBrain b)
        {
            if (_carrying && b.Carrying != null) b.DropCarried(b.transform.position + b.transform.forward * 0.9f);
            _carrying = false;
            if (_target != null && _target.ClaimedBy == b.transform) _target.ClaimedBy = null;
        }
    }

    public sealed class CloseDoorAction : UtilityAction
    {
        public override string Name => "CloseDoor";
        private PhysicsDoor _door;
        private static PhysicsDoor[] _all;
        private static PhysicsDoor[] All => _all ??= Object.FindObjectsByType<PhysicsDoor>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reset() { _all = null; StaticReset.Register(Reset); }

        private PhysicsDoor Best(SaboteurBrain b, out float score)
        {
            PhysicsDoor best = null; score = 0f;
            Vector3 objective = b.PlayerObjective;
            foreach (var d in All)
            {
                if (d == null || d.Locked || !d.IsOpen) continue;
                float near = Curves.Inverse(Vector3.Distance(b.transform.position, d.transform.position) / 25f);
                // doors that lie between the player and their objective matter most
                float playerToDoor = Vector3.Distance(b.PlayerPosition, d.transform.position);
                float onRoute = Curves.Inverse(Mathf.Abs(playerToDoor + Vector3.Distance(d.transform.position, objective) -
                                                         Vector3.Distance(b.PlayerPosition, objective)) / 12f);
                float playerFar = Curves.Logistic(playerToDoor / 10f, 0.6f, 8f); // don't do it in their face
                float s = Combine(d.Openness, near, 0.3f + 0.7f * onRoute, playerFar);
                if (s > score) { score = s; best = d; }
            }
            return best;
        }

        public override float Score(SaboteurBrain b)
        {
            Best(b, out float s);
            return s * (1f - b.Threat() * 0.7f) * 0.7f;
        }

        public override void Enter(SaboteurBrain b) { _door = Best(b, out _); b.Mover.MaxSpeed = b.BaseSpeed; }

        public override bool Tick(SaboteurBrain b, float dt)
        {
            if (_door == null || !_door.IsOpen) return true;
            // stand beside the hinge so the closing leaf doesn't hit us
            Vector3 spot = _door.transform.position + _door.transform.forward * 1.4f;
            if (!NavGrid.Instance.IsWalkableWorld(spot)) spot = _door.transform.position - _door.transform.forward * 1.4f;
            if (!b.Mover.MoveTo(spot)) return true;
            if (Vector3.Distance(b.transform.position.Flat(), spot.Flat()) < 1.2f)
            {
                _door.Close();
                return true;
            }
            return false;
        }
    }

    public sealed class PlantTrapAction : UtilityAction
    {
        public override string Name => "PlantTrap";
        private GasCanister _can;
        private Vector3 _site;
        private int _stage;

        private static GasCanister FindCanister(SaboteurBrain b)
        {
            GasCanister best = null; float bd = 30f;
            foreach (var c in Object.FindObjectsByType<GasCanister>())
            {
                if (c.IsTrap || c.IsHeld || c.Exploded) continue;
                float d = Vector3.Distance(c.transform.position, b.transform.position);
                if (d < bd) { bd = d; best = c; }
            }
            return best;
        }

        /// <summary>Trap site: a walkable spot on the straight line from the player toward their objective, 8-14 m ahead.</summary>
        private static bool FindSite(SaboteurBrain b, out Vector3 site)
        {
            Vector3 p = b.PlayerPosition, o = b.PlayerObjective;
            Vector3 dir = (o - p).Flat();
            float len = dir.magnitude;
            site = default;
            if (len < 6f) return false;
            dir /= len;
            for (float d = Mathf.Min(14f, len - 1f); d >= 8f; d -= 1.5f)
            {
                Vector3 c = p + dir * d;
                int n = NavGrid.Instance.NearestWalkable(c, 4);
                if (n < 0) continue;
                site = NavGrid.Instance.CellCenter(n);
                return true;
            }
            return false;
        }

        public override float Score(SaboteurBrain b)
        {
            if (_stage > 0) return 0.8f;
            if (b.ActiveTrap != null || !b.PlayerIsAlive || !b.Eyes.HasTarget) return 0f;
            var c = FindCanister(b);
            if (c == null || !FindSite(b, out _)) return 0f;
            float distOk = Curves.Logistic(b.DistanceToPlayer / 12f, 0.6f, 8f); // only when not right next to the player
            return Combine(distOk, 1f - b.Threat()) * 0.6f;
        }

        public override void Enter(SaboteurBrain b)
        {
            _can = FindCanister(b);
            _stage = _can != null && FindSite(b, out _site) ? 1 : 0;
            b.Mover.MaxSpeed = b.BaseSpeed * 1.1f;
        }

        public override bool Tick(SaboteurBrain b, float dt)
        {
            if (_can == null || _can.Exploded) { _stage = 0; return true; }
            if (_stage == 1)
            {
                if (!b.Mover.MoveTo(_can.transform.position)) { _stage = 0; return true; }
                if (Vector3.Distance(b.transform.position.Flat(), _can.transform.position.Flat()) < 1.2f)
                {
                    b.GrabCanister(_can);
                    _stage = 2;
                }
                return false;
            }
            if (b.CarryingCanister == null) { _stage = 0; return true; }
            if (!b.Mover.MoveTo(_site)) { _stage = 0; b.DropCarried(b.transform.position); return true; }
            if (Vector3.Distance(b.transform.position.Flat(), _site.Flat()) < 1.2f)
            {
                var can = b.CarryingCanister;
                b.DropCarried(_site);
                can.IsTrap = true;
                b.ActiveTrap = can;
                _stage = 0;
                return true;
            }
            return false;
        }

        public override void Exit(SaboteurBrain b)
        {
            if (b.CarryingCanister != null) b.DropCarried(b.transform.position + b.transform.forward * 0.6f);
            _stage = 0;
        }
    }

    public sealed class DetonateTrapAction : UtilityAction
    {
        public override string Name => "DetonateTrap";
        private float _charge;

        public override float Score(SaboteurBrain b)
        {
            var t = b.ActiveTrap;
            if (t == null || t.Exploded || !b.PlayerIsAlive) return 0f;
            float d = Vector3.Distance(t.transform.position, b.PlayerPosition);
            float playerClose = Curves.InverseQuadratic(d / 5f);
            bool los = MistUtil.HasLineOfSight(b.transform.position + Vector3.up * 0.6f, t.transform.position + Vector3.up * 0.3f, b.transform, t.transform);
            float selfSafe = Curves.Logistic(Vector3.Distance(b.transform.position, t.transform.position) / 6f, 0.9f, 10f);
            return los ? Combine(playerClose, selfSafe) * 1.2f : 0f;
        }

        public override void Enter(SaboteurBrain b) { _charge = 0.35f; b.Mover.Stop(); }

        public override bool Tick(SaboteurBrain b, float dt)
        {
            var t = b.ActiveTrap;
            if (t == null) return true;
            b.Mover.SetLookTarget(t.transform.position);
            _charge -= dt;
            if (_charge > 0f) return false;
            Vfx.Tracer(b.ClawPoint.position, t.transform.position + Vector3.up * 0.3f, new Color(0.3f, 0.9f, 1f), 0.05f);
            ProceduralAudio.PlayAt("zap", b.transform.position);
            t.Explode();
            b.ActiveTrap = null;
            b.Mover.SetLookTarget(null);
            return true;
        }
    }

    public sealed class EvadeAction : UtilityAction
    {
        public override string Name => "Evade";
        private Vector3 _dest;
        private float _t;

        public override float Score(SaboteurBrain b)
        {
            if (!b.PlayerIsAlive) return 0f;
            float threat = b.Threat();
            // a held crate/canister is about to be thrown: dodge harder when close
            float throwRisk = b.Player.IsHoldingObject ? Curves.Inverse(b.DistanceToPlayer / 10f) : 0f;
            return Mathf.Max(threat * 0.9f, throwRisk * 0.95f);
        }

        public override void Enter(SaboteurBrain b)
        {
            _t = 1.4f;
            Vector3 aim = b.Player.AimDirection.Flat().normalized;
            Vector3 side = Vector3.Cross(Vector3.up, aim) * (Random.value < 0.5f ? -1f : 1f);
            Vector3 away = (b.transform.position - b.PlayerPosition).Flat().normalized;
            _dest = NavGrid.Instance.RandomWalkablePoint(b.transform.position + (side * 4f + away * 1.5f), 1.5f);
            b.Mover.MaxSpeed = b.BaseSpeed * 1.6f;
            ProceduralAudio.PlayAt("beep", b.transform.position, 0.3f, 1.8f);
        }

        public override bool Tick(SaboteurBrain b, float dt)
        {
            _t -= dt;
            if (!b.Mover.MoveTo(_dest) || b.Mover.Arrived || _t <= 0f) return true;
            return false;
        }

        public override void Exit(SaboteurBrain b) => b.Mover.MaxSpeed = b.BaseSpeed;
    }

    public sealed class ZapAction : UtilityAction
    {
        public override string Name => "Zap";
        private float _cool;
        private float _t;

        public override float Score(SaboteurBrain b)
        {
            if (!b.PlayerIsAlive || !b.Eyes.HasTarget) return 0f;
            var p = b.Player;
            float close = Curves.Inverse(b.DistanceToPlayer / 9f);
            float opening = p.IsReloading ? 1f : p.IsHoldingObject ? 0.8f : 0.35f;
            float health = 0.4f + 0.6f * b.HealthComponent.Normalized;
            return Combine(close, opening, health) * (b.ActiveTrap != null ? 0.6f : 1f);
        }

        public override void Enter(SaboteurBrain b)
        {
            _t = 4f;
            b.Mover.MaxSpeed = b.BaseSpeed * 1.5f;
            if (b.Spider) b.Spider.Attacking = true;
        }

        public override bool Tick(SaboteurBrain b, float dt)
        {
            _t -= dt; _cool -= dt;
            if (!b.PlayerIsAlive || _t < 0f) return true;
            Vector3 p = b.PlayerPosition;
            b.Mover.SetLookTarget(p);
            if (b.DistanceToPlayer > 1.7f) { if (!b.Mover.MoveTo(p)) return true; return false; }
            b.Mover.Stop();
            if (_cool <= 0f)
            {
                _cool = 0.8f;
                b.Player.Health.TakeDamage(12f, b.transform.position);
                Vfx.Tracer(b.ClawPoint.position, b.Player.EyePosition - Vector3.up * 0.6f, new Color(0.3f, 0.9f, 1f), 0.06f);
                Vfx.Flash(b.ClawPoint.position, new Color(0.3f, 0.9f, 1f), 6f, 4f, 0.08f);
                ProceduralAudio.PlayAt("zap", b.transform.position);
            }
            return false;
        }

        public override void Exit(SaboteurBrain b)
        {
            if (b.Spider) b.Spider.Attacking = false;
            b.Mover.MaxSpeed = b.BaseSpeed;
            b.Mover.SetLookTarget(null);
        }
    }

    public sealed class WanderAction : UtilityAction
    {
        public override string Name => "Wander";
        private Vector3 _dest;
        public override float Score(SaboteurBrain b) => 0.08f;

        public override void Enter(SaboteurBrain b)
        {
            _dest = NavGrid.Instance.RandomWalkablePoint(b.transform.position, 8f);
            b.Mover.MaxSpeed = b.BaseSpeed * 0.5f;
            b.Mover.SetLookTarget(null);
        }

        public override bool Tick(SaboteurBrain b, float dt) => !b.Mover.MoveTo(_dest) || b.Mover.Arrived;
    }
}
