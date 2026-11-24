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
                // 1. Turn off GameView Gizmos (this removes cyan NavMesh and yellow light icons)
                var gameViewType = typeof(Editor).Assembly.GetType("UnityEditor.GameView");
                if (gameViewType != null)
                {
                    var gameViews = Resources.FindObjectsOfTypeAll(gameViewType);
                    foreach (var gv in gameViews)
                    {
                        var prop = gameViewType.GetProperty("gizmos", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                        if (prop != null)
                        {
                            bool isGizmosOn = (bool)prop.GetValue(gv);
                            if (isGizmosOn)
                            {
                                prop.SetValue(gv, false);
                                var repaint = gameViewType.GetMethod("Repaint", BindingFlags.Public | BindingFlags.Instance);
                                repaint?.Invoke(gv, null);
                            }
                        }
                    }
                }

                // 2. Turn off SceneView Gizmos
                var sceneViews = Resources.FindObjectsOfTypeAll<SceneView>();
                foreach (var sv in sceneViews)
                {
                    if (sv != null && sv.drawGizmos)
                    {
                        sv.drawGizmos = false;
                        sv.Repaint();
                    }
                }

                // 3. Close Navigation Window if it is open (prevents blue navmesh overlay)
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

                // 4. Deselect Level if selected so NavMeshSurface doesn't show debug mesh
                if (Selection.activeGameObject != null && Selection.activeGameObject.name == "Level")
                {
                    Selection.activeGameObject = null;
                }
            }
            catch
            {
                // Ignore any internal reflection errors gracefully
            }
        }
    }
}
