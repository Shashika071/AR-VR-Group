using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ReefExplorer.EditorTools
{
    public static class ReefExplorerValidator
    {
        const string ScenePath = "Assets/ReefExplorer/Scenes/ReefExplorer.unity";

        [MenuItem("Reef Explorer/6. Validate ReefExplorer Scene")]
        public static void Validate()
        {
            if (EditorApplication.isPlaying)
            {
                EditorUtility.DisplayDialog("Stop Play Mode", "Exit Play Mode first.", "OK");
                return;
            }

            if (!System.IO.File.Exists(ScenePath))
            {
                EditorUtility.DisplayDialog("Missing scene", "Run menu 2 to build the scene first.", "OK");
                return;
            }

            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            var report = new System.Text.StringBuilder();
            var ok = true;

            void Check(string name, bool present)
            {
                if (!present)
                {
                    ok = false;
                    report.AppendLine("MISSING: " + name);
                }
                else
                {
                    report.AppendLine("OK: " + name);
                }
            }

            Check("MissionController", Object.FindAnyObjectByType<ReefExplorer.Core.MissionController>() != null);
            Check("MonitoringBuoy", GameObject.Find("MonitoringBuoy") != null);
            Check("PowerCell", GameObject.Find("PowerCell") != null);
            Check("Scanner", GameObject.Find("Scanner") != null);
            Check("SampleBottle", GameObject.Find("SampleBottle") != null);
            Check("DesktopPlayer", GameObject.Find("DesktopPlayer") != null);
            Check("XR Origin (XR Rig)", GameObject.Find("XR Origin (XR Rig)") != null);
            Check("MissionCanvas", GameObject.Find("MissionCanvas") != null);
            Check("Zone_Coral", GameObject.Find("Zone_Coral") != null);
            Check("Zone_Turtle", GameObject.Find("Zone_Turtle") != null);
            Check("Zone_Ray", GameObject.Find("Zone_Ray") != null);
            Check("SampleZone", GameObject.Find("SampleZone") != null);
            Check("BottleHolder", GameObject.Find("BottleHolder") != null);

            EditorUtility.DisplayDialog(
                ok ? "Validation passed" : "Validation found gaps",
                report.ToString() +
                (ok
                    ? "\nScene looks ready. Press Play and complete the mission."
                    : "\nRun Reef Explorer → 2. Build / Refresh ReefExplorer Scene, then validate again."),
                "OK");
        }
    }
}
