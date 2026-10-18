using UnityEngine;

namespace MistProtocol.AI.Saboteur
{
    /// <summary>Response curves that map a normalised input (0..1) to a utility (0..1).</summary>
    public static class Curves
    {
        public static float Linear(float x) => Mathf.Clamp01(x);
        public static float Inverse(float x) => 1f - Mathf.Clamp01(x);
        public static float Quadratic(float x) { x = Mathf.Clamp01(x); return x * x; }
        public static float InverseQuadratic(float x) { x = 1f - Mathf.Clamp01(x); return x * x; }
        /// <summary>S-curve centred on <paramref name="mid"/>; steepness k.</summary>
        public static float Logistic(float x, float mid = 0.5f, float k = 10f) => 1f / (1f + Mathf.Exp(-k * (x - mid)));
        /// <summary>Peaks at <paramref name="peak"/>, falls off with width w.</summary>
        public static float Bell(float x, float peak, float w) => Mathf.Exp(-((x - peak) * (x - peak)) / (2f * w * w));
    }

    /// <summary>
    /// A utility action. Score() multiplies several considerations; the brain picks
    /// the highest scorer every decision tick (with a momentum bonus for the
    /// current action so it doesn't dither between near-equal options).
    /// Multiplying considerations means any single "veto" (score 0) kills the action,
    /// and a compensation factor stops actions with many considerations being unfairly
    /// penalised (Dave Mark's "Infinite Axis Utility System" correction).
    /// </summary>
    public abstract class UtilityAction
    {
        public abstract string Name { get; }
        public float LastScore { get; set; }

        public abstract float Score(SaboteurBrain b);
        public virtual void Enter(SaboteurBrain b) { }
        /// <returns>true when the action has finished.</returns>
        public abstract bool Tick(SaboteurBrain b, float dt);
        public virtual void Exit(SaboteurBrain b) { }

        protected static float Combine(params float[] considerations)
        {
            if (considerations.Length == 0) return 0f;
            float mod = 1f - 1f / considerations.Length;
            float score = 1f;
            foreach (float raw in considerations)
            {
                float c = Mathf.Clamp01(raw);
                score *= c + (1f - c) * mod * c;
            }
            return score;
        }
    }
}
