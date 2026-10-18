using UnityEngine;

namespace MistProtocol.AI.Director
{
    /// <summary>Maintenance vent the OVERSEER can deploy robots from.</summary>
    public class SpawnPoint : MonoBehaviour
    {
        public float LastUsed { get; set; } = -999f;

        private void OnDrawGizmos()
        {
            Gizmos.color = new Color(1f, 0.4f, 0.1f, 0.6f);
            Gizmos.DrawWireCube(transform.position + Vector3.up * 0.5f, new Vector3(1f, 1f, 1f));
        }
    }
}
