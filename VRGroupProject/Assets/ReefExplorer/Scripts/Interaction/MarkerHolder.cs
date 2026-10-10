using System;
using ReefExplorer.Audio;
using ReefExplorer.Core;
using ReefExplorer.Survey;
using ReefExplorer.UI;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace ReefExplorer.Interaction
{
    public sealed class MarkerHolder : MonoBehaviour
    {
        [SerializeField] string siteId = "site_coral";
        [SerializeField] UnityEngine.XR.Interaction.Toolkit.Interactors.XRSocketInteractor socket;

        void Awake()
        {
            if (socket == null)
                socket = GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactors.XRSocketInteractor>();
        }

        void OnEnable()
        {
            if (socket != null)
                socket.selectEntered.AddListener(OnMarkerPlaced);
            MissionEvents.MissionRestarted += OnRestart;
        }

        void OnDisable()
        {
            if (socket != null)
                socket.selectEntered.RemoveListener(OnMarkerPlaced);
            MissionEvents.MissionRestarted -= OnRestart;
        }

        void OnRestart() => ShowPlaceHints(false);

        public static void ShowPlaceHints(bool show)
        {
            foreach (var holder in FindObjectsByType<MarkerHolder>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (holder == null)
                    continue;
                holder.SetHint(show && holder.CanAcceptMarker());
            }
        }

        bool CanAcceptMarker()
        {
            var mc = MissionController.Instance;
            if (mc != null && mc.MarkerPlaced)
                return false;

            if (mc != null && !string.IsNullOrEmpty(mc.RecommendedSiteId))
                return string.Equals(mc.RecommendedSiteId, siteId, StringComparison.OrdinalIgnoreCase);

            var site = FindSite();
            return site == null || site.SuitableForRestoration;
        }

        SiteDefinition FindSite()
        {
            var mc = MissionController.Instance;
            if (mc?.Sites == null)
                return null;
            foreach (var site in mc.Sites)
            {
                if (site != null && string.Equals(site.SiteId, siteId, StringComparison.OrdinalIgnoreCase))
                    return site;
            }

            return null;
        }

        void SetHint(bool show)
        {
            var hint = transform.Find("PlaceHint");
            if (hint == null && show)
                hint = CreatePlaceHint();
            if (hint != null)
                hint.gameObject.SetActive(show);
        }

        Transform CreatePlaceHint()
        {
            var root = new GameObject("PlaceHint");
            root.transform.SetParent(transform, false);
            root.transform.localPosition = Vector3.zero;
            root.transform.localRotation = Quaternion.identity;

            var scale = transform.lossyScale;
            var up = 1f / Mathf.Max(0.01f, scale.y);
            var ring = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            ring.name = "Ring";
            ring.transform.SetParent(root.transform, false);
            ring.transform.localPosition = new Vector3(0f, 0.08f * up, 0f);
            ring.transform.localScale = WorldScale(scale, 2.6f, 0.08f, 2.6f);
            StripCollider(ring);

            var beam = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            beam.name = "Beam";
            beam.transform.SetParent(root.transform, false);
            beam.transform.localPosition = new Vector3(0f, 1.6f * up, 0f);
            beam.transform.localScale = WorldScale(scale, 0.18f, 1.6f, 0.18f);
            StripCollider(beam);

            var color = new Color(0.2f, 1f, 0.45f);
            Paint(ring, color);
            Paint(beam, new Color(0.2f, 1f, 0.45f, 0.85f));

            var site = FindSite();
            var caption = site != null ? site.DisplayName : "PUT MARKER HERE";
            var label = new GameObject("Label");
            label.transform.SetParent(root.transform, false);
            label.transform.localPosition = new Vector3(0f, 3.4f * up, 0f);
            var mesh = label.AddComponent<TextMesh>();
            mesh.text = caption;
            mesh.characterSize = 0.16f / Mathf.Max(0.01f, scale.x);
            mesh.fontSize = 48;
            mesh.anchor = TextAnchor.MiddleCenter;
            mesh.alignment = TextAlignment.Center;
            mesh.color = Color.white;
            mesh.fontStyle = FontStyle.Bold;
            label.AddComponent<FaceCameraLabel>();

            var lightGo = new GameObject("HintLight");
            lightGo.transform.SetParent(root.transform, false);
            lightGo.transform.localPosition = new Vector3(0f, 1.4f * up, 0f);
            var light = lightGo.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = color;
            light.intensity = 4f;
            light.range = 14f;
            light.shadows = LightShadows.None;

            root.AddComponent<MarkerHintPulse>();
            return root.transform;
        }

        static Vector3 WorldScale(Vector3 parentScale, float x, float y, float z)
        {
            return new Vector3(
                x / Mathf.Max(0.01f, parentScale.x),
                y / Mathf.Max(0.01f, parentScale.y),
                z / Mathf.Max(0.01f, parentScale.z));
        }

        static void StripCollider(GameObject go)
        {
            var col = go.GetComponent<Collider>();
            if (col != null)
                Destroy(col);
        }

        static void Paint(GameObject go, Color color)
        {
            var renderer = go.GetComponent<Renderer>();
            if (renderer == null)
                return;
            var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            var mat = new Material(shader);
            if (mat.HasProperty("_BaseColor"))
                mat.SetColor("_BaseColor", color);
            if (mat.HasProperty("_EmissionColor"))
            {
                mat.EnableKeyword("_EMISSION");
                mat.SetColor("_EmissionColor", color * 1.6f);
            }
            mat.color = color;
            renderer.sharedMaterial = mat;
        }

        public static bool TryPlaceNearest(Vector3 eye, Vector3 body, RecommendationMarker marker)
        {
            if (marker == null)
                return false;

            MarkerHolder best = null;
            var bestDist = 6.5f;
            foreach (var holder in FindObjectsByType<MarkerHolder>(FindObjectsSortMode.None))
            {
                if (holder == null || !holder.gameObject.activeInHierarchy || !holder.CanAcceptMarker())
                    continue;

                var spot = holder.transform.position;
                var dist = Mathf.Min(FlatDistance(eye, spot), FlatDistance(body, spot));
                if (dist < bestDist)
                {
                    bestDist = dist;
                    best = holder;
                }
            }

            if (best == null)
            {
                MissionEvents.RaiseFeedback("Stand in the green ring, then press E.");
                return false;
            }

            return best.Plant(marker);
        }

        static float FlatDistance(Vector3 a, Vector3 b)
        {
            a.y = 0f;
            b.y = 0f;
            return Vector3.Distance(a, b);
        }

        bool Plant(RecommendationMarker marker)
        {
            var mc = MissionController.Instance;
            if (mc == null)
                return false;
            if (string.IsNullOrEmpty(mc.RecommendedSiteId) && !mc.TryRecommendSite(siteId))
                return false;
            if (!mc.TryPlaceMarker(siteId))
                return false;

            ShowPlaceHints(false);
            GameAudio.PlayMarkerPlace(transform.position);
            if (socket != null)
                socket.socketActive = false;
            marker.transform.SetParent(transform, true);
            marker.transform.position = transform.position + Vector3.up * 0.35f;
            marker.transform.rotation = Quaternion.identity;
            var grab = marker.GetComponent<XRGrabInteractable>();
            if (grab != null)
                grab.enabled = false;
            return true;
        }

        void OnMarkerPlaced(SelectEnterEventArgs args)
        {
            var marker = args.interactableObject.transform.GetComponent<RecommendationMarker>();
            if (marker == null) return;

            var mc = MissionController.Instance;
            if (mc == null) return;

            if (mc.TryPlaceMarker(siteId))
            {
                ShowPlaceHints(false);
                GameAudio.PlayMarkerPlace(transform.position);
                socket.socketActive = false; // lock it in
            }
            else
            {
                // Force drop if incorrect site or premature
                if (args.interactorObject is UnityEngine.XR.Interaction.Toolkit.Interactors.XRBaseInteractor baseInteractor)
                    baseInteractor.interactionManager.SelectCancel((UnityEngine.XR.Interaction.Toolkit.Interactors.IXRSelectInteractor)baseInteractor, marker.GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.IXRSelectInteractable>());
            }
        }
    }

    sealed class MarkerHintPulse : MonoBehaviour
    {
        Light hintLight;
        Transform ring;
        Vector3 ringBase;

        void Awake()
        {
            hintLight = GetComponentInChildren<Light>();
            ring = transform.Find("Ring");
            if (ring != null)
                ringBase = ring.localScale;
        }

        void Update()
        {
            var pulse = 0.65f + Mathf.PingPong(Time.time, 0.7f);
            if (ring != null)
                ring.localScale = new Vector3(ringBase.x * pulse, ringBase.y, ringBase.z * pulse);
            if (hintLight != null)
                hintLight.intensity = 2.4f + pulse * 2.5f;
        }
    }
}
