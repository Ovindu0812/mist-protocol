using MistProtocol.AI.Navigation;
using MistProtocol.Core;
using UnityEngine;

namespace MistProtocol.Interaction
{
    /// <summary>
    /// Heavy tea crate the player can shove to block corridors. When it comes to
    /// rest in a new spot it tells the NavGrid to rebuild that area, so agents
    /// immediately path around (or the Saboteur comes to drag it back).
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class Barricade : MonoBehaviour
    {
        [SerializeField] private float settleSpeed = 0.08f;

        private Rigidbody _rb;
        private Bounds _lastBounds;
        private bool _moving;
        private Collider _col;

        public Vector3 HomePosition { get; private set; }
        public Quaternion HomeRotation { get; private set; }
        /// <summary>How far the crate has been pushed from where the level designer placed it.</summary>
        public float Displacement => Vector3.Distance(transform.position.Flat(), HomePosition.Flat());
        public bool IsCarried { get; private set; }
        public Transform ClaimedBy { get; set; }
        public Rigidbody Body => _rb;

        private void Awake()
        {
            _rb = GetComponent<Rigidbody>();
            _col = GetComponentInChildren<Collider>();
            HomePosition = transform.position;
            HomeRotation = transform.rotation;
        }

        private void Start() => _lastBounds = _col.bounds;

        private void FixedUpdate()
        {
            if (IsCarried) return;
            bool movingNow = _rb.linearVelocity.sqrMagnitude > settleSpeed * settleSpeed;
            if (movingNow) _moving = true;
            else if (_moving)
            {
                _moving = false;
                var g = NavGrid.Instance;
                if (g != null)
                {
                    var b = _col.bounds;
                    b.Encapsulate(_lastBounds);
                    g.RefreshArea(b);
                }
                _lastBounds = _col.bounds;
                if (Displacement > 0.5f) GameEvents.RaisePhysicsInteraction();
            }
        }

        public void BeginCarry(Transform holder, Vector3 localPosition)
        {
            IsCarried = true;
            _rb.isKinematic = true;
            transform.SetParent(holder, true);
            transform.localPosition = localPosition;
            Physics.SyncTransforms();
            var g = NavGrid.Instance;
            if (g != null) g.RefreshArea(_lastBounds); // old spot is free again
        }

        public void EndCarry(Vector3 dropPosition)
        {
            transform.SetParent(null, true);
            transform.position = dropPosition;
            transform.rotation = HomeRotation;
            _rb.isKinematic = false;
            IsCarried = false;
            ClaimedBy = null;
            _moving = true;
        }
    }
}
