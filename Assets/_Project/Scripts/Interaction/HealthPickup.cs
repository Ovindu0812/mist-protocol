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

        private void Start()
        {
            _basePosition = transform.position;
            _phaseOffset = Random.Range(0f, Mathf.PI * 2f);
        }

        private void Update()
        {
            transform.Rotate(0f, 75f * Time.deltaTime, 0f, Space.World);
            float newY = _basePosition.y + Mathf.Sin((Time.time + _phaseOffset) * hoverFrequency) * hoverAmplitude;
            transform.position = new Vector3(_basePosition.x, newY, _basePosition.z);
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!PlayerLocator.Alive || !other.transform.IsChildOf(PlayerLocator.State.Root)) return;
            var h = PlayerLocator.State.Health;
            if (h.Current >= h.Max) return;
            h.Heal(amount);
            ProceduralAudio.PlayAt("pickup", transform.position);
            Destroy(gameObject);
        }
    }
}
