using MistProtocol.Core;
using UnityEngine;

namespace MistProtocol.Interaction
{
    /// <summary>Thermos of hot Ceylon tea. Restores health.</summary>
    public class HealthPickup : MonoBehaviour
    {
        [SerializeField] private float amount = 35f;
        [SerializeField] private float hoverFrequency = 2.5f;
        [SerializeField] private float hoverAmplitude = 0.08f;

        private Vector3 _basePosition;
        private float _phaseOffset;
        private Light _glowLight;

        private void Start()
        {
            _basePosition = transform.position;
            _phaseOffset = Random.Range(0f, Mathf.PI * 2f);

            var lightGo = new GameObject("GlowLight", typeof(Light));
            lightGo.transform.SetParent(transform, false);
            lightGo.transform.localPosition = Vector3.up * 0.2f;
            _glowLight = lightGo.GetComponent<Light>();
            _glowLight.type = LightType.Point;
            _glowLight.range = 3.2f;
            _glowLight.color = new Color(0.25f, 1f, 0.45f);
            _glowLight.intensity = 1.2f;
        }

        private void Update()
        {
            transform.Rotate(0f, 75f * Time.deltaTime, 0f, Space.World);
            float newY = _basePosition.y + Mathf.Sin((Time.time + _phaseOffset) * hoverFrequency) * hoverAmplitude;
            transform.position = new Vector3(_basePosition.x, newY, _basePosition.z);

            if (_glowLight != null)
            {
                _glowLight.intensity = 1.1f + 0.35f * Mathf.Sin(Time.time * 3.5f);
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!PlayerLocator.Alive || !other.transform.IsChildOf(PlayerLocator.State.Root)) return;
            var h = PlayerLocator.State.Health;
            if (h.Current >= h.Max) return;
            h.Heal(amount);
            Vfx.Burst(transform.position, Vector3.up, new Color(0.3f, 1f, 0.5f), 16, 2.2f, 0.08f, 0.5f);
            ProceduralAudio.PlayAt("pickup", transform.position, 0.85f, 1.2f);
            Destroy(gameObject);
        }
    }
}
