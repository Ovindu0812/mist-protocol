using MistProtocol.AI.Agents;
using MistProtocol.AI.Director;
using MistProtocol.AI.Navigation;
using MistProtocol.AI.Perception;
using UnityEngine;
using UnityEngine.InputSystem;

namespace MistProtocol.UI
{
    /// <summary>
    /// F1: shows every agent's live decision, perception and planned path, plus the
    /// director's pacing state and recent sound events. Built for the demo video and viva.
    /// </summary>
    public class DebugOverlay : MonoBehaviour
    {
        private bool _on;
        private GUIStyle _style, _box;
        private Material _lineMat;

        private void Update()
        {
            if (Keyboard.current != null && Keyboard.current.f1Key.wasPressedThisFrame) _on = !_on;
        }

        private void OnGUI()
        {
            if (!_on) return;
            _style ??= new GUIStyle(GUI.skin.label) { fontSize = 14, richText = true, normal = { textColor = Color.white } };
            _box ??= new GUIStyle(GUI.skin.box) { fontSize = 14, alignment = TextAnchor.UpperLeft, richText = true };
            var cam = Camera.main;
            if (cam == null) return;

            foreach (var a in AgentBase.Active)
            {
                Vector3 sp = cam.WorldToScreenPoint(a.transform.position + Vector3.up * 2.4f);
                if (sp.z < 0f) continue;
                string col = a.Sensor.CanSeePlayer ? "#ff6050" : a.Sensor.HasTarget ? "#ffc040" : "#80e0ff";
                string txt = $"<b><color={col}>{a.Kind}</color></b>\n{a.DebugState}\n<size=11>{a.DebugDetail}</size>";
                GUI.Label(new Rect(sp.x - 130, Screen.height - sp.y - 60, 260, 80), txt, _style);
            }

            var d = OverseerDirector.Instance;
            string dir = d == null ? "" :
                $"<b>OVERSEER DIRECTOR</b>\nphase {d.CurrentPhase}   intensity {d.Intensity:0.00}\nprofile {d.Profile}\nlast spawn: {d.LastDecision}\n" +
                $"agents alive {AgentBase.Active.Count}   A* last expansions {AStarPathfinder.LastExpanded}";
            GUI.Box(new Rect(10, Screen.height - 150, 640, 110), dir, _box);
        }

        private void OnRenderObject()
        {
            if (!_on) return;
            if (_lineMat == null)
            {
                _lineMat = new Material(Shader.Find("Hidden/Internal-Colored")) { hideFlags = HideFlags.HideAndDontSave };
                _lineMat.SetInt("_ZTest", (int)UnityEngine.Rendering.CompareFunction.Always);
            }
            _lineMat.SetPass(0);
            GL.Begin(GL.LINES);
            foreach (var a in AgentBase.Active)
            {
                var path = a.Body.DebugPath;
                GL.Color(new Color(0.3f, 0.9f, 1f, 0.9f));
                for (int i = 1; i < path.Count; i++) { GL.Vertex(path[i - 1] + Vector3.up * 0.15f); GL.Vertex(path[i] + Vector3.up * 0.15f); }
            }
            var snd = SoundSystem.Instance;
            if (snd != null)
            {
                foreach (var r in snd.Recent)
                {
                    float age = Time.time - r.Time;
                    if (age > 2f) continue;
                    GL.Color(r.Player ? new Color(1f, 0.8f, 0.2f, 1f - age / 2f) : new Color(1f, 0.3f, 0.3f, 1f - age / 2f));
                    float rad = Mathf.Lerp(0.5f, r.Radius, age / 2f);
                    for (int s = 0; s < 32; s++)
                    {
                        float a0 = s / 32f * Mathf.PI * 2f, a1 = (s + 1) / 32f * Mathf.PI * 2f;
                        GL.Vertex(r.Pos + new Vector3(Mathf.Cos(a0), 0.1f, Mathf.Sin(a0)) * rad);
                        GL.Vertex(r.Pos + new Vector3(Mathf.Cos(a1), 0.1f, Mathf.Sin(a1)) * rad);
                    }
                }
            }
            GL.End();
        }
    }
}
