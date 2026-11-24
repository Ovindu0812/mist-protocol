using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace MistProtocol.EditorTools
{
    [InitializeOnLoad]
    public static class AutoFixGizmosOnPlay
    {
        static AutoFixGizmosOnPlay()
        {
            EditorApplication.update += CheckAndDisableGizmos;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
            DisableAllGizmos();
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            DisableAllGizmos();
        }

        private static int _updateCount = 0;
        private static void CheckAndDisableGizmos()
        {
            if (++_updateCount % 120 == 0)
            {
                DisableAllGizmos();
            }
        }

        [MenuItem("Mist Protocol/Restore Original Dark Colors (Turn Off Gizmos)")]
        public static void ForceDisable()
        {
            DisableAllGizmos();
            Debug.Log("[Mist Protocol] Original dark factory colors restored! Gizmos turned OFF.");
        }

        public static void DisableAllGizmos()
        {
            try
            {
                // 1. Shrink 3D Gizmo icons to 0 so they completely disappear
                var annoType = typeof(Editor).Assembly.GetType("UnityEditor.AnnotationUtility");
                if (annoType != null)
                {
                    var iconSizeProp = annoType.GetProperty("iconSize", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
                    if (iconSizeProp != null && iconSizeProp.CanWrite) iconSizeProp.SetValue(null, 0f);

                    var use3dProp = annoType.GetProperty("use3dGizmos", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
                    if (use3dProp != null && use3dProp.CanWrite) use3dProp.SetValue(null, false);

                    var showGridProp = annoType.GetProperty("showGrid", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
                    if (showGridProp != null && showGridProp.CanWrite) showGridProp.SetValue(null, false);
                }

                // 2. Turn off PlayModeView / GameView Gizmos in Unity 6
                var viewTypes = new[]
                {
                    typeof(Editor).Assembly.GetType("UnityEditor.PlayModeView"),
                    typeof(Editor).Assembly.GetType("UnityEditor.GameView")
                };

                foreach (var vt in viewTypes)
                {
                    if (vt == null) continue;
                    var windows = Resources.FindObjectsOfTypeAll(vt);
                    foreach (var w in windows)
                    {
                        foreach (var name in new[] { "drawGizmos", "gizmos", "showGizmos", "m_Gizmos" })
                        {
                            var prop = vt.GetProperty(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                            if (prop != null && prop.CanWrite)
                            {
                                prop.SetValue(w, false);
                            }
                            var field = vt.GetField(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                            if (field != null)
                            {
                                field.SetValue(w, false);
                            }
                        }
                        var repaint = vt.GetMethod("Repaint", BindingFlags.Public | BindingFlags.Instance);
                        repaint?.Invoke(w, null);
                    }
                }

                // 3. Turn off SceneView Gizmos
                var sceneViews = Resources.FindObjectsOfTypeAll<SceneView>();
                foreach (var sv in sceneViews)
                {
                    if (sv != null && sv.drawGizmos)
                    {
                        sv.drawGizmos = false;
                        sv.Repaint();
                    }
                }

                // 4. Close Navigation Window if it is open
                var navWindowTypes = new[]
                {
                    typeof(Editor).Assembly.GetType("UnityEditor.NavMeshEditorWindow"),
                    typeof(Editor).Assembly.GetType("UnityEditor.AI.NavMeshEditorWindow")
                };
                foreach (var t in navWindowTypes)
                {
                    if (t != null)
                    {
                        var windows = Resources.FindObjectsOfTypeAll(t);
                        foreach (var w in windows)
                        {
                            if (w is EditorWindow ew) ew.Close();
                        }
                    }
                }

                // 5. Deselect Level if selected so NavMeshSurface doesn't show debug mesh
                if (Selection.activeGameObject != null && Selection.activeGameObject.name == "Level")
                {
                    Selection.activeGameObject = null;
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[Mist Protocol] Gizmo disable notice: " + ex.Message);
            }
        }
    }
}
