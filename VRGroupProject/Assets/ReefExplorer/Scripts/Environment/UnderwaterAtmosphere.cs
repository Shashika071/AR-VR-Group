using UnityEngine;
using UnityEngine.Rendering;

namespace ReefExplorer.Environment
{
    /// <summary>
    /// Forces a coherent underwater look (no ordinary sky/horizon) at runtime.
    /// </summary>
    public sealed class UnderwaterAtmosphere : MonoBehaviour
    {
        [SerializeField] Color fogColor = new(0.02f, 0.22f, 0.32f);
        [SerializeField] float fogDensity = 0.035f;
        [SerializeField] Color ambient = new(0.08f, 0.22f, 0.3f);

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
            RenderSettings.subtractiveShadowColor = fogColor;
            RenderSettings.skybox = null;

            foreach (var cam in FindObjectsByType<Camera>(FindObjectsInactive.Include))
            {
                cam.clearFlags = CameraClearFlags.SolidColor;
                cam.backgroundColor = fogColor;
                cam.farClipPlane = Mathf.Max(cam.farClipPlane, 80f);
            }
        }
    }
}
