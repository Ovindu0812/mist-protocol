using System.Collections.Generic;
using MistProtocol.Core;
using UnityEngine;

namespace MistProtocol.Interaction
{
    /// <summary>
    /// Fully simulated swinging door: a Rigidbody leaf on a HingeJoint whose spring
    /// drives it toward a target angle. Because it is a real physics body the
    /// player can also shove it, bullets knock it, and crates can jam it.
    /// </summary>
    [RequireComponent(typeof(Rigidbody), typeof(HingeJoint))]
    public class PhysicsDoor : MonoBehaviour, IInteractable
    {
        [SerializeField] private float openAngle = 100f;
        [SerializeField] private float springForce = 120f;
        [SerializeField] private float damper = 18f;
        [SerializeField] private bool locked;

        private static readonly Dictionary<int, PhysicsDoor> DoorCells = new();
        private readonly List<int> _cells = new();
        private Rigidbody _rb;
        private HingeJoint _hinge;
        private float _target;

        public bool IsOpen => Mathf.Abs(_hinge.angle) > 25f;
        public bool IsClosed => Mathf.Abs(_hinge.angle) < 12f;
        public bool Locked => locked;
        public float Openness => Mathf.Clamp01(Mathf.Abs(_hinge != null ? _hinge.angle : 0f) / openAngle);

        public string Prompt => locked ? "Locked" : IsOpen ? "[E] Close door" : "[E] Open door";
        public bool CanInteract => !locked;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() { DoorCells.Clear(); StaticReset.Register(ResetStatics); }

        public static bool IsClosedDoorCell(int cell) => DoorCells.TryGetValue(cell, out var d) && d != null && d.IsClosed;

        private void Awake()
        {
            _rb = GetComponent<Rigidbody>();
            _hinge = GetComponent<HingeJoint>();
            _hinge.useSpring = true;
            _hinge.useLimits = true;
            _hinge.limits = new JointLimits { min = -openAngle, max = openAngle };
            SetTarget(0f);
            _rb.isKinematic = locked;
        }

        private void Start()
        {
            var grid = AI.Navigation.NavGrid.Instance;
            if (grid == null) return;
            var b = GetComponentInChildren<Collider>().bounds;
            for (float x = b.min.x; x <= b.max.x; x += grid.CellSize * 0.5f)
            for (float z = b.min.z; z <= b.max.z; z += grid.CellSize * 0.5f)
            {
                int i = grid.WorldToIndex(new Vector3(x, 0, z));
                if (i >= 0 && !_cells.Contains(i)) { _cells.Add(i); DoorCells[i] = this; }
            }
        }

        private void SetTarget(float angle)
        {
            _target = angle;
            _hinge.spring = new JointSpring { spring = springForce, damper = damper, targetPosition = angle };
            _rb.WakeUp();
        }

        public void Interact()
        {
            if (locked) return;
            if (IsOpen) Close();
            else OpenAwayFrom(PlayerLocator.State != null ? PlayerLocator.State.Root.position : transform.position - transform.forward);
            GameEvents.RaisePhysicsInteraction();
        }

        /// <summary>Swing the leaf away from whoever is pushing through.</summary>
        public void OpenAwayFrom(Vector3 pusher)
        {
            if (locked || Mathf.Abs(_target) > 1f) return;
            float side = Vector3.Dot(pusher - transform.position, transform.forward);
            SetTarget(side > 0f ? openAngle * 0.95f : -openAngle * 0.95f);
            ProceduralAudio.PlayAt("door", transform.position, 0.6f);
            AI.Perception.SoundSystem.Emit(transform.position, 10f, AI.Perception.SoundType.Door, false);
        }

        public void Close()
        {
            if (locked || Mathf.Abs(_target) < 1f) return;
            SetTarget(0f);
            ProceduralAudio.PlayAt("door", transform.position, 0.6f, 0.8f);
            AI.Perception.SoundSystem.Emit(transform.position, 10f, AI.Perception.SoundType.Door, false);
        }

        public void SetLocked(bool value)
        {
            locked = value;
            _rb.isKinematic = value;
            var grid = AI.Navigation.NavGrid.Instance;
            if (grid != null) grid.RefreshArea(GetComponentInChildren<Collider>().bounds);
            if (!value) _rb.WakeUp();
        }
    }
}
