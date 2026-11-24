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

                    // Turn off all individual icon and gizmo annotations across all Unity components
                    var getAnno = annoType.GetMethod("GetAnnotations", BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public);
                    var setIcon = annoType.GetMethod("SetIconEnabled", BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public);
                    var setGizmo = annoType.GetMethod("SetGizmoEnabled", BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public);
                    if (getAnno != null && setIcon != null)
                    {
                        var list = getAnno.Invoke(null, null) as System.Collections.IEnumerable;
                        if (list != null)
                        {
                            foreach (var item in list)
                            {
                                var cidField = item.GetType().GetField("classID");
                                var scField = item.GetType().GetField("scriptClass");
                                if (cidField != null && scField != null)
                                {
                                    int cid = (int)cidField.GetValue(item);
                                    string sc = (string)scField.GetValue(item);
                                    setIcon.Invoke(null, new object[] { cid, sc, 0 });
                                    setGizmo?.Invoke(null, new object[] { cid, sc, 0 });
                                }
                            }
                        }
                    }
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
                        // Set all gizmo-related properties to false
                        var allProps = vt.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                        foreach (var p in allProps)
                        {
                            if (p.PropertyType == typeof(bool) && p.Name.IndexOf("gizmo", StringComparison.OrdinalIgnoreCase) >= 0 && p.CanWrite)
                            {
                                p.SetValue(w, false);
                            }
                        }
                        // Set all gizmo-related fields to false
                        var allFields = vt.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                        foreach (var f in allFields)
                        {
                            if (f.FieldType == typeof(bool) && f.Name.IndexOf("gizmo", StringComparison.OrdinalIgnoreCase) >= 0)
                            {
                                f.SetValue(w, false);
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

                // 5. Disable NavMesh visualization settings if active
                var navVizType = typeof(Editor).Assembly.GetType("UnityEditor.AI.NavMeshVisualizationSettings") ??
                                 typeof(Editor).Assembly.GetType("UnityEditor.NavMeshVisualizationSettings");
                if (navVizType != null)
                {
                    var showNav = navVizType.GetProperty("showNavigation", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
                    if (showNav != null && showNav.CanWrite) showNav.SetValue(null, 0);
                }

                // 6. Clear selection so no selected object renders gizmos / bounds
                if (Selection.activeGameObject != null)
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
