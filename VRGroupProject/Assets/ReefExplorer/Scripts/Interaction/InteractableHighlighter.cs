using System.Collections.Generic;
using ReefExplorer.Core;
using UnityEngine;

namespace ReefExplorer.Interaction
{
    /// <summary>
    /// Automatically finds all interactable objects in the scene and adds
    /// pulsing glow highlights + floating labels so the player knows what to interact with.
    /// Runs once after scene load, similar to StylizedToolVisuals.
    /// </summary>
    public sealed class InteractableHighlighter : MonoBehaviour
    {
        static readonly Color CoralColor = new Color(0.9f, 0.3f, 0.5f);
        static readonly Color SampleColor = new Color(0.2f, 0.6f, 1f);
        static readonly Color RubbishColor = new Color(0.8f, 0.7f, 0.3f);
        static readonly Color HazardColor = new Color(1f, 0.3f, 0.15f);
        static readonly Color MarkerColor = new Color(0.3f, 1f, 0.5f);
        static readonly Color BuoyColor = new Color(1f, 0.85f, 0.2f);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            if (GameObject.Find("ResearchStation") == null)
                return;
            if (FindAnyObjectByType<InteractableHighlighter>() != null)
                return;

            var host = new GameObject("InteractableHighlighter");
            host.AddComponent<InteractableHighlighter>();
        }

        void Start()
        {
            HighlightCoralPoints();
            HighlightSampleZones();
            HighlightRubbish();
            HighlightHazards();
            HighlightMarkerHolders();
            HighlightRecommendationMarker();
            HighlightAnalyser();
            HighlightMonitoringBuoy();
        }

        void HighlightCoralPoints()
        {
            foreach (var cp in FindObjectsByType<CoralSurveyPoint>(FindObjectsSortMode.None))
            {
                AddPulseGlow(cp.gameObject, CoralColor, 0.8f);
                AddBeacon(cp.transform, BeaconShape.Crystal, CoralColor, 0.55f);
            }
        }

        void HighlightSampleZones()
        {
            foreach (var sz in FindObjectsByType<SampleZone>(FindObjectsSortMode.None))
            {
                // Create a visible marker for the sample zone
                var indicator = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                indicator.name = "SampleZoneIndicator";
                indicator.transform.SetParent(sz.transform, false);
                indicator.transform.localPosition = Vector3.zero;
                indicator.transform.localScale = new Vector3(1.5f, 0.02f, 1.5f);
                var col = indicator.GetComponent<Collider>();
                if (col != null) Destroy(col);
                SetColor(indicator, new Color(SampleColor.r, SampleColor.g, SampleColor.b, 0.4f));
                AddPulseGlow(indicator, SampleColor, 0.5f);
                AddBeacon(sz.transform, BeaconShape.Drop, SampleColor, 0.7f);
            }
        }

        void HighlightRubbish()
        {
            foreach (var ri in FindObjectsByType<RubbishItem>(FindObjectsSortMode.None))
            {
                AddPulseGlow(ri.gameObject, RubbishColor, 0.4f);
                AddBeacon(ri.transform, BeaconShape.Ring, RubbishColor, 0.28f);
            }
        }

        void HighlightHazards()
        {
            foreach (var hf in FindObjectsByType<HazardFlag>(FindObjectsSortMode.None))
            {
                AddPulseGlow(hf.gameObject, HazardColor, 1.2f);
                AddBeacon(hf.transform, BeaconShape.Ring, new Color(0.35f, 0.95f, 0.3f), 0.9f);
            }
        }

        void HighlightMarkerHolders()
        {
            foreach (var mh in FindObjectsByType<MarkerHolder>(FindObjectsSortMode.None))
            {
                AddPulseGlow(mh.gameObject, MarkerColor, 0.5f);
                AddBeacon(mh.transform, BeaconShape.Ring, MarkerColor, 0.45f);
            }
        }

        void HighlightRecommendationMarker()
        {
            foreach (var rm in FindObjectsByType<RecommendationMarker>(FindObjectsSortMode.None))
            {
                AddPulseGlow(rm.gameObject, MarkerColor, 0.6f);
                AddBeacon(rm.transform, BeaconShape.Crystal, MarkerColor, 0.4f);
            }
        }

        void HighlightAnalyser()
        {
            foreach (var sa in FindObjectsByType<SampleAnalyser>(FindObjectsSortMode.None))
            {
                AddPulseGlow(sa.gameObject, SampleColor, 0.6f);
                AddBeacon(sa.transform, BeaconShape.Drop, SampleColor, 0.45f);
            }
        }

        void HighlightMonitoringBuoy()
        {
            var buoy = GameObject.Find("MonitoringBuoy");
            if (buoy != null)
            {
                AddPulseGlow(buoy, BuoyColor, 2f);
                AddBeacon(buoy.transform, BeaconShape.Ring, BuoyColor, 2.2f);
            }
        }

        static void AddPulseGlow(GameObject go, Color color, float radius)
        {
            if (go.GetComponent<PulseGlow>() != null) return;

            var glow = go.AddComponent<PulseGlow>();
            glow.glowColor = color;
            glow.radius = radius;
        }

        enum BeaconShape { Crystal, Drop, Ring }

        static void AddBeacon(Transform parent, BeaconShape shape, Color color, float height)
        {
            if (parent.Find("SeeBeacon") != null)
                return;

            var beacon = GameObject.CreatePrimitive(shape switch
            {
                BeaconShape.Drop => PrimitiveType.Sphere,
                BeaconShape.Ring => PrimitiveType.Cylinder,
                _ => PrimitiveType.Cube
            });
            beacon.name = "SeeBeacon";
            beacon.transform.SetParent(parent, false);
            beacon.transform.localPosition = new Vector3(0f, height, 0f);
            var col = beacon.GetComponent<Collider>();
            if (col != null)
                Destroy(col);

            if (shape == BeaconShape.Crystal)
            {
                beacon.transform.localScale = new Vector3(0.12f, 0.22f, 0.12f);
                beacon.transform.localRotation = Quaternion.Euler(0f, 45f, 0f);
            }
            else if (shape == BeaconShape.Drop)
            {
                beacon.transform.localScale = Vector3.one * 0.16f;
            }
            else
            {
                beacon.transform.localScale = new Vector3(0.55f, 0.02f, 0.55f);
                beacon.transform.localPosition = new Vector3(0f, 0.08f, 0f);
            }

            SetColor(beacon, new Color(color.r, color.g, color.b, 0.85f));
            beacon.AddComponent<BeaconBob>();
        }

        static void SetColor(GameObject go, Color color)
        {
            var renderer = go.GetComponent<Renderer>();
            if (renderer == null) return;
            var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            var mat = new Material(shader);
            if (mat.HasProperty("_BaseColor"))
                mat.SetColor("_BaseColor", color);
            else
                mat.color = color;
            if (mat.HasProperty("_Surface"))
            {
                mat.SetFloat("_Surface", 1f); // Transparent
                mat.SetFloat("_Blend", 0f);
                mat.SetOverrideTag("RenderType", "Transparent");
                mat.renderQueue = 3000;
            }
            renderer.sharedMaterial = mat;
        }
    }

    /// <summary>
    /// Adds a pulsing point light to indicate interactable objects.
    /// Disappears when the associated IScannable is scanned or the GameObject is disabled.
    /// </summary>
    public sealed class PulseGlow : MonoBehaviour
    {
        public Color glowColor = Color.cyan;
        public float radius = 1f;

        Light pointLight;
        float baseIntensity = 0.6f;
        IScannable scannable;
        RubbishItem rubbishItem;
        bool wasCompleted;

        void Start()
        {
            var lightGo = new GameObject("GlowLight");
            lightGo.transform.SetParent(transform, false);
            lightGo.transform.localPosition = new Vector3(0f, 0.2f, 0f);
            pointLight = lightGo.AddComponent<Light>();
            pointLight.type = LightType.Point;
            pointLight.color = glowColor;
            pointLight.intensity = baseIntensity;
            pointLight.range = radius;
            pointLight.shadows = LightShadows.None;
            pointLight.renderMode = LightRenderMode.Auto;

            scannable = GetComponent<IScannable>();
            rubbishItem = GetComponent<RubbishItem>();
        }

        void Update()
        {
            if (pointLight == null) return;

            // Check if already completed
            bool completed = false;
            if (scannable != null) completed = scannable.IsScanned;

            if (completed && !wasCompleted)
            {
                wasCompleted = true;
                pointLight.color = new Color(0.45f, 1f, 0.55f);
                baseIntensity = 0.25f;

                var beacon = transform.Find("SeeBeacon");
                if (beacon != null)
                    beacon.gameObject.SetActive(false);
            }

            // Pulse the light
            float pulse = 0.5f + 0.5f * Mathf.Sin(Time.time * 3f);
            pointLight.intensity = baseIntensity * (0.6f + 0.4f * pulse);
        }
    }

    /// <summary>Slow hover so a beacon reads as a sign, not a piece of the object.</summary>
    public sealed class BeaconBob : MonoBehaviour
    {
        Vector3 origin;

        void Start() => origin = transform.localPosition;

        void Update()
        {
            var p = origin;
            p.y += Mathf.Sin(Time.time * 2.2f + origin.x) * 0.06f;
            transform.localPosition = p;
        }
    }
}
