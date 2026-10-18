using MistProtocol.Core;
using UnityEngine;

namespace MistProtocol.Interaction
{
    /// <summary>Entering a zone updates the HUD and lets OVERSEER taunt the player.</summary>
    [RequireComponent(typeof(BoxCollider))]
    public class ZoneTrigger : MonoBehaviour
    {
        public string zoneName;
        [TextArea] public string overseerLine;
        public bool isControlRoom;
        private bool _fired;

        private void OnTriggerEnter(Collider other)
        {
            if (_fired || !PlayerLocator.Alive || !other.transform.IsChildOf(PlayerLocator.State.Root)) return;
            _fired = true;
            GameManager.Instance?.OnZoneEntered(this);
        }
    }
}
