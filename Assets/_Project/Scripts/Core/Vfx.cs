using UnityEngine;

namespace MistProtocol.Core
{
    /// <summary>Lightweight code-driven effects: no prefab dependencies, everything cleaned up by lifetime.</summary>
    public static class Vfx
    {
        public static void Tracer(Vector3 from, Vector3 to, Color color, float width = 0.025f)
        {
            var lib = VfxLibrary.Instance;
            if (lib == null) return;
            var go = new GameObject("Tracer");
            var lr = go.AddComponent<LineRenderer>();
            lr.sharedMaterial = lib.tracerMaterial;
            lr.positionCount = 2;
            lr.SetPosition(0, from);
            lr.SetPosition(1, to);
            lr.startWidth = width;
            lr.endWidth = width * 0.4f;
            lr.startColor = color;
            lr.endColor = new Color(color.r, color.g, color.b, 0f);
            lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            Object.Destroy(go, 0.06f);
        }

        public static void Flash(Vector3 pos, Color color, float intensity, float range, float duration)
        {
            var go = new GameObject("Flash");
            go.transform.position = pos;
            var l = go.AddComponent<Light>();
            l.type = LightType.Point;
            l.color = color;
            l.intensity = intensity;
            l.range = range;
            l.shadows = LightShadows.None;
            Object.Destroy(go, duration);
        }

        public static void Burst(Vector3 pos, Vector3 normal, Color color, int count, float speed, float size, float life)
        {
            var lib = VfxLibrary.Instance;
            if (lib == null) return;
            var go = new GameObject("Burst");
            go.transform.SetPositionAndRotation(pos, Quaternion.LookRotation(normal == Vector3.zero ? Vector3.up : normal));
            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = ps.main;
            main.duration = 0.2f;
            main.loop = false;
            main.startLifetime = new ParticleSystem.MinMaxCurve(life * 0.5f, life);
            main.startSpeed = new ParticleSystem.MinMaxCurve(speed * 0.3f, speed);
            main.startSize = new ParticleSystem.MinMaxCurve(size * 0.5f, size);
            main.startColor = color;
            main.gravityModifier = 0.4f;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            var em = ps.emission;
            em.rateOverTime = 0f;
            em.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)count) });
            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 35f;
            shape.radius = 0.05f;
            var col = ps.colorOverLifetime;
            col.enabled = true;
            var grad = new Gradient();
            grad.SetKeys(new[] { new GradientColorKey(color, 0f), new GradientColorKey(color * 0.3f, 1f) },
                         new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0f, 1f) });
            col.color = grad;
            var r = go.GetComponent<ParticleSystemRenderer>();
            r.sharedMaterial = lib.particleMaterial;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            ps.Play();
            Object.Destroy(go, life + 0.5f);
        }

        public static void Impact(Vector3 pos, Vector3 normal, bool metal)
        {
            Burst(pos, normal, metal ? new Color(1f, 0.8f, 0.4f) : new Color(0.6f, 0.58f, 0.55f), metal ? 10 : 6, 4f, 0.06f, 0.35f);
        }

        public static void Explosion(Vector3 pos)
        {
            Burst(pos, Vector3.up, new Color(1f, 0.55f, 0.15f), 60, 9f, 0.6f, 0.9f);
            Burst(pos, Vector3.up, new Color(0.35f, 0.4f, 0.3f), 30, 3f, 1.6f, 2.2f);
            Flash(pos + Vector3.up, new Color(1f, 0.6f, 0.25f), 40f, 14f, 0.25f);
        }
    }
}
