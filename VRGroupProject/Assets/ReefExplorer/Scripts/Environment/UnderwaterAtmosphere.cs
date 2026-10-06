using UnityEngine;
using UnityEngine.Rendering;

namespace ReefExplorer.Environment
{
    /// <summary>
    /// Scuba-diver underwater look: murky green-blue haze, short visibility.
    /// </summary>
    public sealed class UnderwaterAtmosphere : MonoBehaviour
    {
        // Murky tropical water column — not crystal clear.
        [SerializeField] Color fogColor = new(0.04f, 0.20f, 0.26f);
        [SerializeField] float fogDensity = 0.13f;
        [SerializeField] Color ambient = new(0.03f, 0.11f, 0.15f);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            if (GameObject.Find("ResearchStation") == null)
                return;
            if (FindAnyObjectByType<UnderwaterAtmosphere>() != null)
                return;

            var go = new GameObject("UnderwaterAtmosphere");
            go.AddComponent<UnderwaterAtmosphere>();
        }

        void Awake() => Apply();
        void OnEnable() => Apply();

        public void Apply()
        {
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogColor = fogColor;
            RenderSettings.fogDensity = fogDensity;
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = ambient;
            RenderSettings.subtractiveShadowColor = new Color(0.02f, 0.08f, 0.12f);
            RenderSettings.skybox = null;

            foreach (var cam in FindObjectsByType<Camera>(FindObjectsInactive.Include))
            {
                cam.clearFlags = CameraClearFlags.SolidColor;
                cam.backgroundColor = fogColor;
                // Divers rarely see much past ~15–20 m in reef water.
                cam.farClipPlane = 26f;
            }
        }
    }
}
