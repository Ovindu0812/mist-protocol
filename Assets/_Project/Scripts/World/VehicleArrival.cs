using UnityEngine;

namespace MistProtocol.World
{
    /// <summary>Scripted, physics-free jeep arrival used before player control starts.</summary>
    public sealed class VehicleArrival : MonoBehaviour
    {
        [SerializeField] private Vector3 stopPosition;
        [SerializeField] private float duration = 5f;
        [SerializeField] private Transform[] wheels;
        private Vector3 _start;
        private float _elapsed;
        private bool _driving;

        public void Configure(Vector3 destination, Transform[] wheelTransforms)
        {
            stopPosition = destination;
            wheels = wheelTransforms;
        }

        private void Awake() => _start = transform.position;

        public void DriveIn()
        {
            transform.position = _start;
            _elapsed = 0f;
            _driving = true;
        }

        private void Update()
        {
            if (!_driving) return;
            _elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(_elapsed / duration);
            float eased = 1f - Mathf.Pow(1f - t, 3f);
            Vector3 last = transform.position;
            transform.position = Vector3.Lerp(_start, stopPosition, eased);
            float moved = Vector3.Distance(last, transform.position);
            foreach (var wheel in wheels)
                if (wheel != null) wheel.Rotate(Vector3.right, moved * 400f, Space.Self);
            if (t >= 1f) _driving = false;
        }
    }
}
