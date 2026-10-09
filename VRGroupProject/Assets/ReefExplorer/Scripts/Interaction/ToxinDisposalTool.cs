using System.Collections.Generic;
using ReefExplorer.Audio;
using ReefExplorer.Core;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace ReefExplorer.Interaction
{
    /// <summary>
    /// Canister kept on the station table. Press E inside the green cloud to dispose the toxin.
    /// </summary>
    [RequireComponent(typeof(XRGrabInteractable))]
    public sealed class ToxinDisposalTool : MonoBehaviour
    {
        static Transform hint;
        XRGrabInteractable grab;

        public static bool IsDisposed { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            if (GameObject.Find("ResearchStation") == null)
                return;
            if (FindAnyObjectByType<ToxinDisposalTool>() != null)
                return;

            Spawn();
        }

        void Awake()
        {
            grab = GetComponent<XRGrabInteractable>();
            MissionEvents.MissionRestarted += OnRestart;
        }

        void OnDestroy()
        {
            MissionEvents.MissionRestarted -= OnRestart;
        }

        void OnEnable()
        {
            if (grab == null)
                grab = GetComponent<XRGrabInteractable>();
            if (grab == null)
                return;
            grab.selectEntered.AddListener(OnGrab);
            grab.selectExited.AddListener(OnDrop);
        }

        void OnDisable()
        {
            if (grab == null)
                return;
            grab.selectEntered.RemoveListener(OnGrab);
            grab.selectExited.RemoveListener(OnDrop);
        }

        void OnGrab(SelectEnterEventArgs _)
        {
            ShowHint(true);
            MissionEvents.RaiseFeedback("Disposal tool. Press E in the green cloud.");
        }

        void OnDrop(SelectExitEventArgs _) => ShowHint(false);

        void OnRestart()
        {
            IsDisposed = false;
            ShowHint(false);
            ToxinPatch.ResetAll();
        }

        public static bool TryUse(Vector3 from)
        {
            if (IsDisposed)
            {
                MissionEvents.RaiseFeedback("This toxin is already disposed.");
                return true;
            }

            ToxinPatch nearest = null;
            var best = 2.8f;
            foreach (var patch in FindObjectsByType<ToxinPatch>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (patch == null || patch.Cleared)
                    continue;
                var dist = Vector3.Distance(from, patch.transform.position);
                if (dist < best)
                {
                    best = dist;
                    nearest = patch;
                }
            }

            if (nearest == null)
                return false;

            nearest.Clear();
            GameAudio.PlayHazardFlag(nearest.transform.position);
            var left = ToxinPatch.Total - ToxinPatch.ClearedCount;
            if (left > 0)
            {
                MissionEvents.RaiseFeedback("Toxin place cleared, " + left + " left.");
                ShowHint(true);
                return true;
            }

            var hazard = FindAnyObjectByType<HazardFlag>();
            if (hazard == null || !hazard.TryDispose())
                MissionEvents.RaiseFeedback("Toxin disposed.");
            IsDisposed = true;
            ShowHint(false);
            return true;
        }

        public static void ShowHint(bool show)
        {
            if (IsDisposed)
                show = false;

            var patches = FindObjectsByType<ToxinPatch>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            if (patches.Length > 0)
            {
                foreach (var patch in patches)
                {
                    if (patch == null)
                        continue;
                    var ring = patch.transform.Find("DisposeHint");
                    if (ring == null && show && !patch.Cleared)
                        ring = CreateHint(patch.transform);
                    if (ring != null)
                        ring.gameObject.SetActive(show && !patch.Cleared);
                }
                return;
            }

            var hazard = FindAnyObjectByType<HazardFlag>();
            if (hazard == null)
                return;

            if (hint == null)
                hint = CreateHint(hazard.transform);
            hint.gameObject.SetActive(show);
        }

        static void Spawn()
        {
            var station = GameObject.Find("ResearchStation");
            var spot = station != null
                ? station.transform.TransformPoint(new Vector3(0.9f, 1.05f, -1.78f))
                : new Vector3(0.9f, 1.05f, -1.78f);

            var tool = new GameObject("ToxinDisposalTool");
            tool.transform.position = spot;
            tool.transform.rotation = Quaternion.identity;

            var box = tool.AddComponent<BoxCollider>();
            box.size = new Vector3(0.16f, 0.36f, 0.16f);
            box.center = new Vector3(0f, 0.18f, 0f);

            var body = tool.AddComponent<Rigidbody>();
            body.isKinematic = true;
            body.useGravity = false;

            var grab = tool.AddComponent<XRGrabInteractable>();
            grab.movementType = XRBaseInteractable.MovementType.Instantaneous;
            grab.colliders.Clear();
            grab.colliders.Add(box);
            tool.AddComponent<ToxinDisposalTool>();

            var paint = new Color(0.72f, 0.92f, 0.18f);
            var canister = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            canister.name = "Canister";
            canister.transform.SetParent(tool.transform, false);
            canister.transform.localPosition = new Vector3(0f, 0.16f, 0f);
            canister.transform.localScale = new Vector3(0.14f, 0.14f, 0.14f);
            var capCol = canister.GetComponent<Collider>();
            if (capCol != null)
                Destroy(capCol);
            var renderer = canister.GetComponent<Renderer>();
            if (renderer != null)
                renderer.sharedMaterial = Paint(paint);

            var cap = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            cap.name = "Cap";
            cap.transform.SetParent(tool.transform, false);
            cap.transform.localPosition = new Vector3(0f, 0.32f, 0f);
            cap.transform.localScale = new Vector3(0.08f, 0.03f, 0.08f);
            var capCollider = cap.GetComponent<Collider>();
            if (capCollider != null)
                Destroy(capCollider);
            var capRenderer = cap.GetComponent<Renderer>();
            if (capRenderer != null)
                capRenderer.sharedMaterial = Paint(new Color(0.2f, 0.35f, 0.12f));
        }

        static Transform CreateHint(Transform parent)
        {
            var root = new GameObject("DisposeHint");
            root.transform.SetParent(parent, false);
            root.transform.localPosition = Vector3.zero;

            var ring = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            ring.name = "Ring";
            ring.transform.SetParent(root.transform, false);
            ring.transform.localPosition = new Vector3(0f, 0.05f, 0f);
            ring.transform.localScale = new Vector3(1.6f, 0.03f, 1.6f);
            var col = ring.GetComponent<Collider>();
            if (col != null)
                Destroy(col);
            var renderer = ring.GetComponent<Renderer>();
            var color = new Color(0.45f, 1f, 0.25f);
            if (renderer != null)
                renderer.sharedMaterial = Paint(color);

            var lightGo = new GameObject("HintLight");
            lightGo.transform.SetParent(root.transform, false);
            var light = lightGo.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = color;
            light.intensity = 2.8f;
            light.range = 5f;
            light.shadows = LightShadows.None;
            root.AddComponent<DisposeHintPulse>();
            root.SetActive(false);
            return root.transform;
        }

        static Material Paint(Color color)
        {
            var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            var mat = new Material(shader);
            if (mat.HasProperty("_BaseColor"))
                mat.SetColor("_BaseColor", color);
            if (mat.HasProperty("_EmissionColor"))
            {
                mat.EnableKeyword("_EMISSION");
                mat.SetColor("_EmissionColor", color * 0.35f);
            }
            mat.color = color;
            return mat;
        }
    }

    public sealed class ToxinPatch : MonoBehaviour
    {
        static readonly List<ToxinPatch> all = new List<ToxinPatch>();

        public bool Cleared { get; private set; }
        public static int Total => all.Count;

        public static int ClearedCount
        {
            get
            {
                var count = 0;
                foreach (var patch in all)
                {
                    if (patch != null && patch.Cleared)
                        count++;
                }
                return count;
            }
        }

        void OnEnable()
        {
            if (!all.Contains(this))
                all.Add(this);
        }

        void OnDestroy()
        {
            all.Remove(this);
        }

        public void Clear()
        {
            Cleared = true;
            gameObject.SetActive(false);
        }

        public static void HideAll()
        {
            foreach (var patch in all)
            {
                if (patch != null)
                    patch.gameObject.SetActive(false);
            }
        }

        public static void ResetAll()
        {
            foreach (var patch in all)
            {
                if (patch == null)
                    continue;
                patch.Cleared = false;
                patch.gameObject.SetActive(true);
            }
        }
    }

    sealed class DisposeHintPulse : MonoBehaviour
    {
        Light hintLight;

        void Awake() => hintLight = GetComponentInChildren<Light>();

        void Update()
        {
            var pulse = 0.75f + Mathf.PingPong(Time.time, 0.55f);
            if (hintLight != null)
                hintLight.intensity = 1.8f + pulse * 2f;
        }
    }
}
