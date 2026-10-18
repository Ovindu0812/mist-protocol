using MistProtocol.Core;
using UnityEngine;

namespace MistProtocol.Interaction
{
    /// <summary>Dad's keycard on his desk in the Withering Loft.</summary>
    public class KeycardPickup : MonoBehaviour, IInteractable
    {
        public string Prompt => "[E] Take Appa's keycard";
        public bool CanInteract => true;

        private void Update() => transform.Rotate(0f, 60f * Time.deltaTime, 0f, Space.World);

        public void Interact()
        {
            ProceduralAudio.PlayAt("pickup", transform.position);
            GameManager.Instance?.OnKeycardCollected();
            Destroy(gameObject);
        }
    }
}
