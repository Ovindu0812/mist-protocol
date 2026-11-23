using System.Collections;
using MistProtocol.Core;
using UnityEngine;

namespace MistProtocol.FinalRescue
{
    /// <summary>
    /// High-voltage energy barrier that encloses Appa in the Secret Laboratory.
    /// Blocks player movement until disabled by entering the correct switch sequence (Blue -> Green -> Red).
    /// </summary>
    public class EnergyBarrier : MonoBehaviour
    {
        [SerializeField] private Collider barrierCollider;
        [SerializeField] private Renderer barrierRenderer;
        [SerializeField] private Light barrierLight;

        private Material _barrierMat;
        private bool _isDisabling;

        public bool IsActive { get; private set; } = true;

        public void Configure(Collider col, Renderer rend, Light lt)
        {
            barrierCollider = col;
            barrierRenderer = rend;
            barrierLight = lt;
            if (barrierRenderer != null) _barrierMat = barrierRenderer.material;
        }

        private void Awake()
        {
            if (barrierCollider == null) barrierCollider = GetComponent<Collider>();
            if (barrierRenderer == null) barrierRenderer = GetComponent<Renderer>();
            if (barrierLight == null) barrierLight = GetComponentInChildren<Light>();
            if (barrierRenderer != null) _barrierMat = barrierRenderer.material;
        }

        private void Update()
        {
            if (!IsActive || _isDisabling) return;

            // Electric shimmer / oscillation
            float pulse = 0.85f + 0.15f * Mathf.Sin(Time.time * 6f);
            if (barrierLight != null)
            {
                barrierLight.intensity = 0.75f * pulse;
                barrierLight.range = 3.2f;
            }

            if (_barrierMat != null)
            {
                Color c = new Color(0.1f, 0.5f, 0.9f, 0.35f * pulse);
                _barrierMat.SetColor("_BaseColor", c);
                _barrierMat.SetColor("_EmissionColor", new Color(0.1f, 0.55f, 1f) * (0.65f * pulse));
            }
        }

        public void DisableBarrier()
        {
            if (!IsActive || _isDisabling) return;
            StartCoroutine(DissolveSequence());
        }

        private IEnumerator DissolveSequence()
        {
            _isDisabling = true;

            // Audio feedback
            ProceduralAudio.PlayAt("zap", transform.position, 1f, 0.7f);
            ProceduralAudio.PlayAt("hum", transform.position, 0.8f, 0.5f);

            // Turn off physical collision barrier immediately so player can step in
            if (barrierCollider != null)
            {
                barrierCollider.enabled = false;
            }

            // Rapid flicker and collapse
            float elapsed = 0f;
            while (elapsed < 1.2f)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / 1.2f;
                float flicker = (1f - t) * (Mathf.Sin(elapsed * 40f) > 0f ? 1f : 0.2f);

                if (barrierLight != null)
                {
                    barrierLight.intensity = 4f * flicker;
                    barrierLight.color = Color.Lerp(new Color(0.2f, 0.7f, 1f), new Color(0.3f, 1f, 0.5f), t);
                }

                if (_barrierMat != null)
                {
                    _barrierMat.SetColor("_BaseColor", new Color(0.2f, 0.8f, 1f, flicker * 0.7f));
                    _barrierMat.SetColor("_EmissionColor", new Color(0.2f, 0.8f, 1f) * (flicker * 3f));
                }

                transform.localScale = Vector3.Lerp(Vector3.one, new Vector3(0.05f, 1f, 0.05f), t);
                yield return null;
            }

            IsActive = false;
            _isDisabling = false;

            if (barrierRenderer != null) barrierRenderer.enabled = false;
            if (barrierLight != null) barrierLight.enabled = false;
            gameObject.SetActive(false);
        }
    }
}
