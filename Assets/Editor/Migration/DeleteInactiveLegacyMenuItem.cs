#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Luzart.Migration.EditorTools
{
    /// <summary>
    /// One-off menu utility to delete inactive root GameObjects that Unity MCP cannot
    /// reach (its delete tool rejects integer instance IDs and name-search ignores
    /// inactive GOs). Used by Phase C.3a / Phase D.8 of the Luzart migration.
    /// </summary>
    public static class DeleteInactiveLegacyMenuItem
    {
        // Names of root GameObjects that are safe to delete in Phase D.
        // Enverement: inactive container with a Light2D + empty levels child, no inbound refs.
        // _LegacyManagers/GamePlay child (with inactive ManagerWeapons) gets removed when
        // its parent _LegacyManagers eventually gets deleted in Phase F.
        private static readonly string[] PhaseDeleteTargets = { "Enverement" };

        [MenuItem("Tools/Migration/Delete Inactive Legacy GOs (Phase D)")]
        public static void DeleteInactiveLegacyGOs()
        {
            var scene = SceneManager.GetActiveScene();
            var roots = scene.GetRootGameObjects();
            int deleted = 0;
            foreach (var go in roots)
            {
                foreach (var t in PhaseDeleteTargets)
                {
                    if (go.name == t)
                    {
                        Debug.Log($"[Migration] Deleting root GO: '{t}' (was activeSelf={go.activeSelf})");
                        Object.DestroyImmediate(go);
                        deleted++;
                        break;
                    }
                }
            }
            if (deleted > 0)
            {
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
                Debug.Log($"[Migration] Deleted {deleted} GameObject(s) and saved scene.");
            }
            else
            {
                Debug.Log("[Migration] No target GOs found — already deleted.");
            }
        }
    }
}
#endif
