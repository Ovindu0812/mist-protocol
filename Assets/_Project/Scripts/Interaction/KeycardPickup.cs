using MistProtocol.Core;
using UnityEngine;

namespace MistProtocol.Interaction
{
    /// <summary>Dad's keycard on his desk in the Withering Loft.</summary>
    public class KeycardPickup : MonoBehaviour, IInteractable
    {
        public string Prompt => "[E] Take Appa's keycard";
        public bool CanInteract => !IsLocked;

        public bool IsLocked { get; set; }

        private Vector3 _basePos;
        private Light _haloLight;

        private void Start()
        {
            _basePos = transform.position;
            var lightGo = new GameObject("Keycard_Halo", typeof(Light));
            lightGo.transform.SetParent(transform, false);
            lightGo.transform.localPosition = Vector3.up * 0.15f;
            _haloLight = lightGo.GetComponent<Light>();
            _haloLight.type = LightType.Point;
            _haloLight.color = new Color(0.2f, 0.8f, 1f);
            _haloLight.range = 2.8f;
            _haloLight.intensity = 1.0f;
        }

        private void Update()
        {
            if (IsLocked) return;

            transform.Rotate(0f, 65f * Time.deltaTime, 0f, Space.World);
            float newY = _basePos.y + Mathf.Sin(Time.time * 3f) * 0.04f;
            transform.position = new Vector3(_basePos.x, newY, _basePos.z);
            if (_haloLight != null)
            {
                _haloLight.intensity = 0.9f + 0.3f * Mathf.Sin(Time.time * 5f);
            }
        }

        public void UnlockAndReveal()
        {
            IsLocked = false;
            _basePos = transform.position;
            if (_haloLight != null)
            {
                _haloLight.intensity = 1.6f;
                _haloLight.range = 3.6f;
            }
        }

        public void Interact()
        {
            if (IsLocked) return;
            Vfx.Burst(transform.position, Vector3.up, new Color(0.2f, 0.85f, 1f), 15, 2.5f, 0.08f, 0.45f);
            ProceduralAudio.PlayAt("success", transform.position, 0.9f, 1.2f);
            GameManager.Instance?.OnKeycardCollected();
            Destroy(gameObject);
        }
    }
}
