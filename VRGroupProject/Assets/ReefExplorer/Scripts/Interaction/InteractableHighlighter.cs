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
                AddLabel(cp.transform, "SCAN CORAL\n(Aim scanner + click)", 0.6f, CoralColor);
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
                AddLabel(sz.transform, "WATER SAMPLE\n(Bring bottle + press E)", 1.4f, SampleColor);
            }
        }

        void HighlightRubbish()
        {
            foreach (var ri in FindObjectsByType<RubbishItem>(FindObjectsSortMode.None))
            {
                AddPulseGlow(ri.gameObject, RubbishColor, 0.4f);
                AddLabel(ri.transform, "RUBBISH\n(Grab to collect)", 0.35f, RubbishColor);
            }
        }

        void HighlightHazards()
        {
            foreach (var hf in FindObjectsByType<HazardFlag>(FindObjectsSortMode.None))
            {
                AddPulseGlow(hf.gameObject, HazardColor, 1.2f);
                AddLabel(hf.transform, "⚠ HAZARD ⚠\n(Scan — do NOT touch)", 1.0f, HazardColor);
            }
        }

        void HighlightMarkerHolders()
        {
            foreach (var mh in FindObjectsByType<MarkerHolder>(FindObjectsSortMode.None))
            {
                AddPulseGlow(mh.gameObject, MarkerColor, 0.5f);
                AddLabel(mh.transform, "MARKER SLOT\n(Place marker here)", 0.6f, MarkerColor);
            }
        }

        void HighlightRecommendationMarker()
        {
            foreach (var rm in FindObjectsByType<RecommendationMarker>(FindObjectsSortMode.None))
            {
                AddPulseGlow(rm.gameObject, MarkerColor, 0.6f);
                AddLabel(rm.transform, "RESEARCH MARKER\n(Grab after choosing site)", 0.55f, MarkerColor);
            }
        }

        void HighlightAnalyser()
        {
            foreach (var sa in FindObjectsByType<SampleAnalyser>(FindObjectsSortMode.None))
            {
                AddPulseGlow(sa.gameObject, SampleColor, 0.6f);
                AddLabel(sa.transform, "SAMPLE ANALYSER\n(Place bottle here)", 0.5f, SampleColor);
            }
        }

        void HighlightMonitoringBuoy()
        {
            var buoy = GameObject.Find("MonitoringBuoy");
            if (buoy != null)
            {
                AddPulseGlow(buoy, BuoyColor, 2f);
                AddLabel(buoy.transform, "MONITORING BUOY\n(Insert power cell)", 4f, BuoyColor);
            }
        }

        static void AddPulseGlow(GameObject go, Color color, float radius)
        {
            if (go.GetComponent<PulseGlow>() != null) return;

            var glow = go.AddComponent<PulseGlow>();
            glow.glowColor = color;
            glow.radius = radius;
        }

        static void AddLabel(Transform parent, string text, float height, Color color)
        {
            var labelGo = new GameObject("InteractLabel");
            labelGo.transform.SetParent(parent, false);
            labelGo.transform.localPosition = new Vector3(0f, height, 0f);

            var mesh = labelGo.AddComponent<TextMesh>();
            mesh.text = text;
            mesh.characterSize = 0.04f;
            mesh.fontSize = 24;
            mesh.anchor = TextAnchor.MiddleCenter;
            mesh.alignment = TextAlignment.Center;
            mesh.color = color;
            mesh.fontStyle = FontStyle.Bold;
            labelGo.AddComponent<BillboardLabel>();
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
                // Change to green "done" glow
                pointLight.color = Color.green;
                baseIntensity = 0.3f;

                // Add a checkmark label
                var check = new GameObject("Checkmark");
                check.transform.SetParent(transform, false);
                check.transform.localPosition = new Vector3(0f, 0.4f, 0f);
                var mesh = check.AddComponent<TextMesh>();
                mesh.text = "✓ DONE";
                mesh.characterSize = 0.05f;
                mesh.fontSize = 28;
                mesh.anchor = TextAnchor.MiddleCenter;
                mesh.alignment = TextAlignment.Center;
                mesh.color = Color.green;
                mesh.fontStyle = FontStyle.Bold;
                check.AddComponent<BillboardLabel>();

                // Hide the instruction label
                var label = transform.Find("InteractLabel");
                if (label != null) label.gameObject.SetActive(false);
            }

            // Pulse the light
            float pulse = 0.5f + 0.5f * Mathf.Sin(Time.time * 3f);
            pointLight.intensity = baseIntensity * (0.6f + 0.4f * pulse);
        }
    }
}
