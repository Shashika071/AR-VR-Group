using UnityEngine;
using UnityEngine.Rendering;

namespace ReefExplorer.Environment
{
    /// <summary>
    /// Hides prototype zone pads and keeps a scuba-diver murky water look.
    /// Does not override authored UnderwaterEnvironment_v1 sand materials.
    /// </summary>
    public sealed class UnderwaterFeelRuntime : MonoBehaviour
    {
        static readonly Color Fog = new(0.04f, 0.20f, 0.26f);
        static readonly Color Ambient = new(0.03f, 0.11f, 0.15f);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            if (GameObject.Find("ResearchStation") == null)
                return;
            if (FindAnyObjectByType<UnderwaterFeelRuntime>() != null)
                return;

            var host = new GameObject("UnderwaterFeelRuntime");
            host.AddComponent<UnderwaterFeelRuntime>();
        }

        void Start() => StartCoroutine(ApplyLate());

        System.Collections.IEnumerator ApplyLate()
        {
            yield return null;
            yield return new WaitForSeconds(0.2f);
            HideFakePads();
            SoftenWorld();
            SoftenLights();
            BoostMarineSnow();
        }

        static void HideFakePads()
        {
            HideRenderersNamed("ZonePad");
            HideRenderersNamed("SampleMarker");
            HideRenderersNamed("PathMarker");
            HideRenderersNamed("BuoyPathMarker");
        }

        static void HideRenderersNamed(string nameOrPrefix)
        {
            foreach (var t in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include))
            {
                if (t == null)
                    continue;
                if (t.name != nameOrPrefix && !t.name.StartsWith(nameOrPrefix))
                    continue;
                foreach (var r in t.GetComponentsInChildren<Renderer>(true))
                    r.enabled = false;
            }
        }

        static void SoftenWorld()
        {
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogColor = Fog;
            RenderSettings.fogDensity = 0.13f;
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = Ambient;
            RenderSettings.skybox = null;
            RenderSettings.subtractiveShadowColor = new Color(0.02f, 0.08f, 0.12f);

            foreach (var cam in Object.FindObjectsByType<Camera>(FindObjectsInactive.Include))
            {
                cam.clearFlags = CameraClearFlags.SolidColor;
                cam.backgroundColor = Fog;
                cam.farClipPlane = 26f;
                cam.nearClipPlane = 0.05f;
            }
        }

        static void SoftenLights()
        {
            foreach (var light in Object.FindObjectsByType<Light>(FindObjectsInactive.Include))
            {
                if (light == null || light.type != LightType.Directional)
                    continue;
                if (light.name.Contains("Fill"))
                {
                    light.color = new Color(0.12f, 0.32f, 0.42f);
                    light.intensity = Mathf.Min(light.intensity, 0.22f);
                    continue;
                }

                // Filtered sun through water — dimmer and greener than open air.
                light.color = new Color(0.42f, 0.68f, 0.78f);
                light.intensity = Mathf.Clamp(light.intensity, 0.45f, 0.62f);
                light.shadows = LightShadows.Soft;
                light.shadowStrength = 0.55f;
            }
        }

        static void BoostMarineSnow()
        {
            var go = GameObject.Find("UnderwaterParticles");
            if (go == null)
                return;
            var ps = go.GetComponent<ParticleSystem>();
            if (ps == null)
                return;

            var main = ps.main;
            main.maxParticles = 140;
            main.startColor = new Color(0.75f, 0.9f, 0.95f, 0.45f);
            var emission = ps.emission;
            emission.rateOverTime = 16f;
        }
    }
}
