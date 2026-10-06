using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace ReefExplorer.EditorTools
{
    /// <summary>
    /// Entry points for batchmode execution:
    /// Unity.exe -batchmode -quit -projectPath ... -executeMethod ReefExplorer.EditorTools.ReefExplorerBatchMenus.RunFullSetup
    /// </summary>
    public static class ReefExplorerBatchMenus
    {
        public static void RunFullSetup()
        {
            if (EditorApplication.isPlaying)
            {
                Debug.LogWarning("[ReefExplorer] Stop Play Mode before running Full Setup.");
                return;
            }

            Debug.Log("[ReefExplorer] Batch setup starting...");
            VRTestSceneGrabFixer.FixGrabSetup();
            ReefExplorerSceneBuilder.BuildOrRefreshScene();
            Debug.Log("[ReefExplorer] Batch setup finished.");
        }

        [MenuItem("Reef Explorer/0. Run Full Setup (Fix Grab + Build Scene)")]
        public static void RunFullSetupMenu()
        {
            if (EditorApplication.isPlaying)
            {
                EditorUtility.DisplayDialog(
                    "Stop Play Mode",
                    "Exit Play Mode first, then run Reef Explorer → 0. Run Full Setup.",
                    "OK");
                return;
            }

            RunFullSetup();
            EditorUtility.DisplayDialog(
                "Reef Explorer Setup",
                "Done.\n\n1) Open Assets/VRTestScene and test grab (Space + aim + G).\n2) Open Assets/ReefExplorer/Scenes/ReefExplorer.unity for the mission.",
                "OK");
        }
    }
}
