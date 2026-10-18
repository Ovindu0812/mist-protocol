using MistProtocol.Core;
using UnityEngine;

namespace MistProtocol.Interaction
{
    /// <summary>Thermos of hot Ceylon tea. Restores health.</summary>
    public class HealthPickup : MonoBehaviour
    {
        [SerializeField] private float amount = 35f;
        private void Update() => transform.Rotate(0f, 90f * Time.deltaTime, 0f, Space.World);

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
