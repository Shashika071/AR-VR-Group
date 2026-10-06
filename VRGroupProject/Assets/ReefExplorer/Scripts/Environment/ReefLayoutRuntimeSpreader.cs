using UnityEngine;

namespace ReefExplorer.Environment
{
    /// <summary>
    /// Compact mission-area positions. Skips when UnderwaterEnvironment_v1 owns layout.
    /// </summary>
    public sealed class ReefLayoutRuntimeSpreader : MonoBehaviour
    {
        static readonly Vector3 CoralZone = new(-6.5f, 0f, 11f);
        static readonly Vector3 TurtleZone = new(0f, 0f, 15.5f);
        static readonly Vector3 RayZone = new(6.5f, 0f, 11.5f);
        static readonly Vector3 SamplePoint = new(3.5f, 0.6f, 13.5f);
        static readonly Vector3 BuoyPoint = new(0f, 0f, 7f);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            if (GameObject.Find("ResearchStation") == null)
                return;
            if (GameObject.Find("UnderwaterEnvironment_v1") != null)
                return;
            if (FindAnyObjectByType<ReefLayoutRuntimeSpreader>() != null)
                return;

            var host = new GameObject("ReefLayoutRuntimeSpreader");
            host.AddComponent<ReefLayoutRuntimeSpreader>();
        }

        void Start() => Apply();

        void Apply()
        {
            if (GameObject.Find("UnderwaterEnvironment_v1") != null)
                return;

            Move("Zone_Coral", CoralZone);
            Move("Zone_Turtle", TurtleZone);
            Move("Zone_Ray", RayZone);
            Move("SampleZone", SamplePoint);
            Move("MonitoringBuoy", BuoyPoint);

            var seabed = GameObject.Find("Seabed");
            if (seabed != null)
            {
                seabed.transform.position = new Vector3(0f, 0f, 12f);
                seabed.transform.localScale = new Vector3(4.2f, 1f, 4.2f);
            }

            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogColor = new Color(0.04f, 0.20f, 0.26f);
            RenderSettings.fogDensity = 0.13f;
        }

        static void Move(string name, Vector3 pos)
        {
            var go = GameObject.Find(name);
            if (go != null)
                go.transform.position = pos;
        }
    }
}
