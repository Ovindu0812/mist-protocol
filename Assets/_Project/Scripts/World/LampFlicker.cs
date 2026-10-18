using UnityEngine;

namespace MistProtocol.World
{
    /// <summary>Storm-damaged lamp: noise-driven flicker on a realtime light and its emissive bulb.</summary>
    [RequireComponent(typeof(Light))]
    public class LampFlicker : MonoBehaviour
    {
        [SerializeField] private Renderer bulb;
        [SerializeField] private float speed = 7f;
        private Light _light;
        private float _base, _seed;
        private MaterialPropertyBlock _mpb;
        private Color _emission;

        private void Awake()
        {
            _light = GetComponent<Light>();
            _base = _light.intensity;
            _seed = Random.value * 100f;
            _mpb = new MaterialPropertyBlock();
            if (bulb != null) _emission = bulb.sharedMaterial.GetColor("_EmissionColor");
        }

        private void Update()
        {
            float n = Mathf.PerlinNoise(_seed, Time.time * speed);
            float k = n < 0.3f ? 0.05f : Mathf.Lerp(0.6f, 1.1f, n);
            _light.intensity = _base * k;
            if (bulb == null) return;
            bulb.GetPropertyBlock(_mpb);
            _mpb.SetColor("_EmissionColor", _emission * k);
            bulb.SetPropertyBlock(_mpb);
        }
    }
}
