using MistProtocol.Core;
using MistProtocol.Interaction;
using UnityEngine;

namespace MistProtocol.FinalRescue
{
    /// <summary>
    /// Interactive Father NPC (Appa) imprisoned in the Secret Laboratory.
    /// Interacting after barrier deactivation triggers dialogue, rescues father,
    /// and initiates the 60-second emergency escape sequence.
    /// </summary>
    public class FatherRescueNPC : MonoBehaviour, IInteractable
    {
        [SerializeField] private EnergyBarrier energyBarrier;

        private bool _isRescued;
        private Vector3 _basePos;

        public bool IsRescued => _isRescued;

        public bool CanInteract
        {
            get
            {
                if (_isRescued) return false;
                if (energyBarrier != null && energyBarrier.IsActive) return false;
                return true;
            }
        }

        public string Prompt
        {
            get
            {
                if (_isRescued) return "[Father Rescued]";
                if (energyBarrier != null && energyBarrier.IsActive) return "[Protected by Energy Barrier]";
                return "[E] Rescue Father";
            }
        }

        public void BindBarrier(EnergyBarrier barrier)
        {
            energyBarrier = barrier;
        }

        private void Start()
        {
            _basePos = transform.localPosition;
            if (energyBarrier == null) energyBarrier = FindAnyObjectByType<EnergyBarrier>();
        }

        private void Update()
        {
            if (_isRescued) return;

            // Idle breathing bob
            float t = Time.time * 2.2f;
            float bob = Mathf.Sin(t) * 0.015f;
            transform.localPosition = _basePos + new Vector3(0f, bob, 0f);
        }

        public void Interact()
        {
            if (_isRescued) return;
            if (energyBarrier != null && energyBarrier.IsActive) return;

            _isRescued = true;

            // 1. Audio and feedback
            ProceduralAudio.PlayAt("pickup", transform.position, 1f, 1.2f);
            ProceduralAudio.PlayAt("success", transform.position, 0.8f, 1.1f);

            // 2. Display "Father rescued!" and dialogue
            GameEvents.Objective("Father rescued!");
            GameEvents.Overseer("APPA: \"You found me! But we must leave now. This facility is going to collapse!\"");

            // 3. Attach companion follower so Father follows the player
            var companion = gameObject.GetComponent<FatherCompanion>();
            if (companion == null) companion = gameObject.AddComponent<FatherCompanion>();
            companion.StartFollowing();

            // 4. Trigger emergency escape in mission manager
            if (FinalRescueMission.Instance != null)
            {
                FinalRescueMission.Instance.StartEmergencyEscape();
            }
        }
    }
}
