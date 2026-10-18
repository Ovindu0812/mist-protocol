using MistProtocol.Core;
using UnityEngine;

namespace MistProtocol.AI.Perception
{
    /// <summary>
    /// Cone-of-vision sensor with a detection meter. Awareness builds up while
    /// the player is visible (faster when close / sprinting, slower when the
    /// player crouches) so agents don't snap-detect at the edge of the mist.
    /// </summary>
    public class VisionSensor : MonoBehaviour
    {
        [SerializeField] private float viewDistance = 24f;
        [SerializeField] private float fieldOfView = 110f;
        [SerializeField] private float closeSenseRadius = 3f;
        [SerializeField] private Vector3 eyeOffset = new Vector3(0f, 1.6f, 0f);
        [SerializeField] private float tickRate = 10f;

        public bool CanSeePlayer { get; private set; }
        /// <summary>0..1 detection meter. 1 = fully aware of the player.</summary>
        public float Awareness { get; private set; }
        public bool HasTarget => Awareness >= 1f || (Time.time - LastSeenTime < 6f && HasLastKnown);
        public bool HasLastKnown { get; private set; }
        public Vector3 LastKnownPosition { get; private set; }
        public float LastSeenTime { get; private set; } = -999f;
        public float TimeSinceSeen => Time.time - LastSeenTime;
        public Vector3 Eye => transform.position + transform.rotation * eyeOffset;

        public float ViewDistance { get => viewDistance; set => viewDistance = value; }
        public Vector3 EyeOffset { get => eyeOffset; set => eyeOffset = value; }
        public float FieldOfView { get => fieldOfView; set => fieldOfView = value; }

        private float _next;

        /// <summary>External intel (squad call-out, director) about where the player is.</summary>
        public void ReportPosition(Vector3 pos, float awarenessBoost = 1f)
        {
            LastKnownPosition = pos;
            HasLastKnown = true;
            Awareness = Mathf.Max(Awareness, awarenessBoost);
            LastSeenTime = Mathf.Max(LastSeenTime, Time.time - 2f);
        }

        public void Forget()
        {
            HasLastKnown = false;
            Awareness = 0f;
        }

        private void Update()
        {
            if (Time.time < _next) return;
            float dt = 1f / tickRate;
            _next = Time.time + dt;

            CanSeePlayer = false;
            if (!PlayerLocator.Alive) { Awareness = Mathf.Max(0f, Awareness - dt); return; }
            var p = PlayerLocator.State;
            Vector3 target = p.EyePosition - Vector3.up * 0.4f;
            Vector3 to = target - Eye;
            float dist = to.magnitude;

            bool inCone = dist <= closeSenseRadius ||
                          (dist <= viewDistance && Vector3.Angle(transform.forward, to) <= fieldOfView * 0.5f);
            if (inCone && MistUtil.HasLineOfSight(Eye, target, transform, p.Root))
            {
                CanSeePlayer = true;
                float distFactor = Mathf.Lerp(3f, 0.6f, dist / viewDistance);
                float stance = p.IsCrouching ? 0.5f : p.IsSprinting ? 1.5f : 1f;
                Awareness = Mathf.Min(1f, Awareness + dt * distFactor * stance);
                if (Awareness > 0.35f)
                {
                    LastKnownPosition = p.Root.position;
                    HasLastKnown = true;
                    LastSeenTime = Time.time;
                }
            }
            else
            {
                Awareness = Mathf.Max(0f, Awareness - dt * 0.08f);
            }
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = CanSeePlayer ? Color.red : Color.yellow;
            Vector3 l = Quaternion.Euler(0, -fieldOfView * 0.5f, 0) * transform.forward;
            Vector3 r = Quaternion.Euler(0, fieldOfView * 0.5f, 0) * transform.forward;
            Gizmos.DrawRay(Eye, l * viewDistance);
            Gizmos.DrawRay(Eye, r * viewDistance);
        }
    }
}
