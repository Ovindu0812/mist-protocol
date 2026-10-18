using MistProtocol.Core;
using UnityEngine;

namespace MistProtocol.Animation
{
    /// <summary>
    /// Shared procedural animation features. Models are built from rigid parts
    /// pivoted at their joints (see Art/Blender/build_models.py), so animation is
    /// done by rotating those transforms directly: no skinning cost, and the
    /// motion is driven by the motor's real velocity so feet never slide.
    /// </summary>
    [RequireComponent(typeof(AgentMotor))]
    public abstract class AgentAnimatorBase : MonoBehaviour
    {
        [SerializeField] protected Renderer[] glowRenderers;
        protected AgentMotor Motor;
        protected float Recoil;
        protected float Flinch;
        protected float Alert;
        private MaterialPropertyBlock _mpb;
        private static readonly int EmissionId = Shader.PropertyToID("_EmissionColor");

        protected virtual void Awake()
        {
            Motor = GetComponent<AgentMotor>();
            _mpb = new MaterialPropertyBlock();
        }

        public void PlayFire() => Recoil = 1f;
        public void PlayHit() => Flinch = 1f;
        /// <summary>0 = calm (cyan/dim), 1 = hostile (bright red). Drives the eye emission.</summary>
        public void SetAlert(float value) => Alert = Mathf.Clamp01(value);

        protected virtual void LateUpdate()
        {
            float dt = Time.deltaTime;
            Recoil = Mathf.MoveTowards(Recoil, 0f, dt * 6f);
            Flinch = Mathf.MoveTowards(Flinch, 0f, dt * 4f);
            Animate(dt);
            if (glowRenderers != null)
            {
                Color c = Color.Lerp(new Color(0.25f, 0.9f, 1f), new Color(1f, 0.1f, 0.05f), Alert) * Mathf.Lerp(1.5f, 4f, Alert);
                foreach (var r in glowRenderers)
                {
                    if (r == null) continue;
                    r.GetPropertyBlock(_mpb);
                    _mpb.SetColor(EmissionId, c);
                    r.SetPropertyBlock(_mpb);
                }
            }
        }

        protected abstract void Animate(float dt);

        public void SetGlowRenderers(Renderer[] renderers) => glowRenderers = renderers;

        /// <summary>Death: every mesh part becomes a physics debris piece thrown away from the hit.</summary>
        public virtual void Shatter(Vector3 hitFrom, float force = 6f)
        {
            enabled = false;
            foreach (var mf in GetComponentsInChildren<MeshFilter>())
            {
                var t = mf.transform;
                t.SetParent(null, true);
                var col = t.gameObject.AddComponent<BoxCollider>();
                col.size = Vector3.Max(mf.sharedMesh.bounds.size, Vector3.one * 0.05f);
                col.center = mf.sharedMesh.bounds.center;
                var rb = t.gameObject.AddComponent<Rigidbody>();
                rb.mass = 2f;
                rb.interpolation = RigidbodyInterpolation.Interpolate;
                Vector3 dir = (t.position - hitFrom).normalized + Vector3.up * 0.6f + Random.insideUnitSphere * 0.4f;
                rb.AddForce(dir * force, ForceMode.VelocityChange);
                rb.AddTorque(Random.insideUnitSphere * force, ForceMode.VelocityChange);
                Destroy(t.gameObject, Random.Range(6f, 9f));
            }
        }
    }
}
