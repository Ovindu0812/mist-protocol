using MistProtocol.Core;
using UnityEngine;

namespace MistProtocol.AI.Sentinel
{
    public sealed class PatrolAction : GoapAction
    {
        public override string Name => "Patrol";
        private Vector3 _target;
        private float _wait;
        public PatrolAction() { EffTrue = Fact.Patrolled; }
        public override float Cost(SentinelBrain b) => 1f;

        public override void Enter(SentinelBrain b)
        {
            _target = b.NextPatrolPoint();
            _wait = Random.Range(1.5f, 3f);
            b.Mover.MaxSpeed = b.WalkSpeed;
            b.Mover.SetLookTarget(null);
            if (b.Walker) { b.Walker.AimPoint = null; b.Walker.Crouched = false; }
        }

        public override ActionStatus Tick(SentinelBrain b, float dt)
        {
            // always (re)issue the move: a previous action may have stopped the motor
            if (!b.Mover.MoveTo(_target)) return ActionStatus.Failure;
            if ((b.transform.position - _target).Flat().magnitude > 0.8f) return ActionStatus.Running;
            _wait -= dt;
            return _wait <= 0f ? ActionStatus.Success : ActionStatus.Running;
        }
    }

    public sealed class InvestigateAction : GoapAction
    {
        public override string Name => "Investigate";
        private float _look, _timeout;
        public InvestigateAction() { PreTrue = Fact.HasLead; EffTrue = Fact.AreaSearched; }
        public override float Cost(SentinelBrain b) => 1f;

        public override void Enter(SentinelBrain b)
        {
            _look = 3f; _timeout = 20f;
            b.Mover.MaxSpeed = (b.WalkSpeed + b.RunSpeed) * 0.5f;
            b.Mover.SetLookTarget(null);
            if (b.Walker) b.Walker.AimPoint = null;
        }

        public override ActionStatus Tick(SentinelBrain b, float dt)
        {
            _timeout -= dt;
            if (_timeout < 0f) { b.ClearLead(); return ActionStatus.Failure; }
            if (!b.Mover.MoveTo(b.LeadPoint)) { b.ClearLead(); return ActionStatus.Failure; }
            if ((b.transform.position - b.LeadPoint).Flat().magnitude > 1.2f) return ActionStatus.Running;
            // look around the spot
            _look -= dt;
            b.Mover.SetLookTarget(b.transform.position + Quaternion.Euler(0, Mathf.Sin(Time.time * 1.3f) * 120f, 0) * b.transform.forward * 3f);
            if (_look > 0f) return ActionStatus.Running;
            b.ClearLead();
            b.Mover.SetLookTarget(null);
            return ActionStatus.Success;
        }
    }

    public sealed class AlertSquadAction : GoapAction, ICommitted
    {
        public override string Name => "AlertSquad";
        private float _t;
        public AlertSquadAction() { PreTrue = Fact.KnowsPlayer; PreFalse = Fact.SquadAlerted; EffTrue = Fact.SquadAlerted; }
        public override float Cost(SentinelBrain b) => 1f;

        public override void Enter(SentinelBrain b)
        {
            _t = 0.6f;
            b.Mover.Stop();
            b.Mover.SetLookTarget(b.PlayerPosition);
            ProceduralAudio.PlayAt("beep", b.transform.position, 0.6f, 0.7f);
            ProceduralAudio.PlayAt("beep", b.transform.position + Vector3.up, 0.6f, 0.9f);
            SentinelSquad.Alert(b, b.PlayerPosition);
            b.MarkSquadAlerted();
        }

        public override ActionStatus Tick(SentinelBrain b, float dt)
        {
            _t -= dt;
            return _t <= 0f ? ActionStatus.Success : ActionStatus.Running;
        }
    }

    public sealed class ReloadAction : GoapAction, ICommitted
    {
        public override string Name => "Reload";
        public ReloadAction() { PreFalse = Fact.WeaponLoaded; EffTrue = Fact.WeaponLoaded; }
        // reloading in the open is risky: expensive when exposed to a visible player
        public override float Cost(SentinelBrain b) =>
            1f + ((b.State & Fact.PlayerVisible) != 0 && (b.State & Fact.InCover) == 0 ? 4f : 0f);

        public override void Enter(SentinelBrain b)
        {
            b.Gun?.StartReload();
            if (b.Walker) { b.Walker.AimPoint = null; b.Walker.Crouched = b.InCover; }
            if (b.InCover) b.Mover.Stop();
        }

        public override ActionStatus Tick(SentinelBrain b, float dt)
        {
            if (b.Gun == null) return ActionStatus.Failure;
            if (!b.Gun.IsReloading && b.Gun.Ammo <= 0) b.Gun.StartReload();
            return b.Gun.Loaded ? ActionStatus.Success : ActionStatus.Running;
        }
    }

    public sealed class TakeCoverAction : GoapAction
    {
        public override string Name => "TakeCover";
        private Vector3 _cover;
        private float _timeout;
        public TakeCoverAction() { PreTrue = Fact.KnowsPlayer; PreFalse = Fact.InCover; EffTrue = Fact.InCover; }

        public override bool IsValid(SentinelBrain b) => b.FindCover(out _cover);
        public override float Cost(SentinelBrain b) => 1.5f + Vector3.Distance(b.transform.position, _cover) / 6f;

        public override void Enter(SentinelBrain b)
        {
            b.FindCover(out _cover);
            SentinelSquad.ClaimCover(b, _cover);
            _timeout = 9f;
            b.Mover.MaxSpeed = b.RunSpeed;
            if (b.Walker) b.Walker.Crouched = false;
        }

        public override ActionStatus Tick(SentinelBrain b, float dt)
        {
            _timeout -= dt;
            if (_cover == Vector3.zero || _timeout < 0f) return ActionStatus.Failure;
            bool sees = b.Eyes.CanSeePlayer;
            b.Mover.SetLookTarget(sees ? b.PlayerPosition : (Vector3?)null);
            if (b.Walker) b.Walker.AimPoint = sees ? b.PlayerPosition : (Vector3?)null;
            if (sees && (b.State & Fact.WeaponLoaded) != 0) b.Shoot(0.45f); // suppressive fire on the move
            if (!b.Mover.MoveTo(_cover)) return ActionStatus.Failure;
            if (!b.Mover.Arrived) return ActionStatus.Running;
            b.InCover = true;
            b.CoverPoint = _cover;
            return ActionStatus.Success;
        }
    }

    public sealed class ShootFromCoverAction : GoapAction, ICommitted
    {
        public override string Name => "ShootFromCover";
        private float _t;
        private int _cycles;
        private bool _peeking;
        public ShootFromCoverAction() { PreTrue = Fact.InCover | Fact.WeaponLoaded | Fact.KnowsPlayer; EffTrue = Fact.PlayerDamaged; }
        public override float Cost(SentinelBrain b) => 1.5f;

        public override void Enter(SentinelBrain b) { _t = 0.8f; _cycles = 0; _peeking = false; b.Mover.Stop(); }

        public override ActionStatus Tick(SentinelBrain b, float dt)
        {
            if (!b.InCover) return ActionStatus.Failure;
            if (b.Gun == null || b.Gun.Ammo <= 0) return ActionStatus.Failure; // -> replan: Reload
            _t -= dt;
            b.Mover.SetLookTarget(b.PlayerPosition);
            if (b.Walker) { b.Walker.Crouched = !_peeking; b.Walker.AimPoint = _peeking ? b.PlayerPosition : (Vector3?)null; }
            if (_peeking && b.Eyes.CanSeePlayer) b.Shoot(b.PlayerReloading ? 1.2f : 0.9f);
            if (_t > 0f) return ActionStatus.Running;
            _peeking = !_peeking;
            _t = _peeking ? Random.Range(1.6f, 2.4f) : Random.Range(0.8f, 1.4f);
            if (!_peeking && ++_cycles >= 3) return ActionStatus.Success;
            return ActionStatus.Running;
        }

        public override void Exit(SentinelBrain b) { if (b.Walker) { b.Walker.Crouched = false; b.Walker.AimPoint = null; } }
    }

    public sealed class ShootAction : GoapAction
    {
        public override string Name => "Shoot";
        private float _t, _lost;
        private float _strafe;
        public ShootAction() { PreTrue = Fact.PlayerVisible | Fact.WeaponLoaded; EffTrue = Fact.PlayerDamaged; }

        public override float Cost(SentinelBrain b)
        {
            float c = 2.5f;
            if ((b.State & Fact.LowHealth) != 0) c += 6f;
            if (b.PlayerReloading) c -= 1f; // punish the player's reload window
            return Mathf.Max(1f, c);
        }

        public override void Enter(SentinelBrain b)
        {
            _t = Random.Range(2f, 3f); _lost = 0f;
            _strafe = Random.value < 0.5f ? -1f : 1f;
            b.Mover.MaxSpeed = b.WalkSpeed;
        }

        public override ActionStatus Tick(SentinelBrain b, float dt)
        {
            _t -= dt;
            if (!b.Eyes.CanSeePlayer) { _lost += dt; if (_lost > 1f) return ActionStatus.Failure; }
            else _lost = 0f;
            Vector3 p = b.PlayerPosition;
            Vector3 side = Vector3.Cross(Vector3.up, (p - b.transform.position).normalized) * _strafe * 2.5f;
            b.Mover.MoveTo(b.transform.position + side);
            if (b.Mover.PathFailed || b.Mover.Arrived) _strafe = -_strafe;
            b.Mover.SetLookTarget(p);
            if (b.Walker) b.Walker.AimPoint = p;
            if (b.Eyes.CanSeePlayer) b.Shoot(0.8f);
            return _t <= 0f ? ActionStatus.Success : ActionStatus.Running;
        }

        public override void Exit(SentinelBrain b) { if (b.Walker) b.Walker.AimPoint = null; }
    }

    public sealed class FlankAction : GoapAction, ICommitted
    {
        public override string Name => "Flank";
        private Vector3 _point;
        private float _timeout;
        public FlankAction() { PreTrue = Fact.KnowsPlayer | Fact.WeaponLoaded; PreFalse = Fact.PlayerVisible; EffTrue = Fact.PlayerVisible; }

        public override bool IsValid(SentinelBrain b) =>
            SentinelSquad.CountDoing(Name, b) == 0 && b.FindFlankPoint(out _point);

        // emergent pincer: flanking is cheap when a squadmate is already pinning the player down
        public override float Cost(SentinelBrain b) =>
            SentinelSquad.CountDoing("ShootFromCover", b) + SentinelSquad.CountDoing("Shoot", b) > 0 ? 1.2f : 3f;

        public override void Enter(SentinelBrain b)
        {
            b.FindFlankPoint(out _point);
            _timeout = 12f;
            b.Mover.MaxSpeed = b.RunSpeed;
            b.BeginDangerQuery();
            b.Mover.MoveTo(_point, b.DangerCost, true);
            if (b.Walker) { b.Walker.Crouched = false; b.Walker.AimPoint = null; }
            b.Mover.SetLookTarget(null);
        }

        public override ActionStatus Tick(SentinelBrain b, float dt)
        {
            _timeout -= dt;
            if (_timeout < 0f || b.Mover.PathFailed) return ActionStatus.Failure;
            if (b.Eyes.CanSeePlayer && b.Eyes.Awareness >= 1f && Vector3.Distance(b.transform.position, _point) < 4f) return ActionStatus.Success;
            if (b.Mover.Arrived) return b.Eyes.CanSeePlayer ? ActionStatus.Success : ActionStatus.Failure;
            return ActionStatus.Running;
        }
    }

    public sealed class ApproachAction : GoapAction
    {
        public override string Name => "Approach";
        private float _timeout;
        public ApproachAction() { PreTrue = Fact.KnowsPlayer; PreFalse = Fact.PlayerVisible; EffTrue = Fact.PlayerVisible; }
        public override float Cost(SentinelBrain b) => 5f;

        public override void Enter(SentinelBrain b)
        {
            _timeout = 12f;
            b.Mover.MaxSpeed = (b.WalkSpeed + b.RunSpeed) * 0.5f;
            b.Mover.SetLookTarget(null);
        }

        public override ActionStatus Tick(SentinelBrain b, float dt)
        {
            _timeout -= dt;
            if (b.Eyes.CanSeePlayer && b.Eyes.Awareness >= 1f) return ActionStatus.Success;
            if (_timeout < 0f || !b.Mover.MoveTo(b.Eyes.LastKnownPosition)) return ActionStatus.Failure;
            if (b.Mover.Arrived)
            {
                // reached last known position and found nothing: downgrade knowledge to a lead
                b.Eyes.Forget();
                return ActionStatus.Failure;
            }
            return ActionStatus.Running;
        }
    }
}
