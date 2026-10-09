using ReefExplorer.Audio;
using ReefExplorer.Core;
using ReefExplorer.Environment;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

namespace ReefExplorer.Interaction
{
    /// <summary>
    /// Accepts only PowerCell objects. Validates correct object for buoy restore.
    /// </summary>
    public sealed class BuoyPowerSocket : MonoBehaviour
    {
        [SerializeField] XRSocketInteractor socket;
        [SerializeField] Transform snapPoint;
        [SerializeField] MonitoringBuoy buoy;
        [SerializeField] float proximityRadius = 1.15f;
        bool connected;

        void Awake()
        {
            if (socket == null)
                socket = GetComponent<XRSocketInteractor>();
            if (snapPoint == null)
                snapPoint = transform;
            if (buoy == null)
                buoy = GetComponentInParent<MonitoringBuoy>();

            var renderer = GetComponent<Renderer>();
            if (renderer != null)
                renderer.enabled = false;
            var box = GetComponent<Collider>();
            if (box != null)
                box.enabled = false;
        }

        void OnEnable()
        {
            if (socket != null)
                socket.selectEntered.AddListener(OnSocketSelect);
        }

        void OnDisable()
        {
            if (socket != null)
                socket.selectEntered.RemoveListener(OnSocketSelect);
        }

        void Update()
        {
            if (MissionController.Instance == null || MissionController.Instance.BuoyRestored)
                return;

            var cells = FindObjectsByType<PowerCell>();
            foreach (var cell in cells)
            {
                if (cell == null || cell.IsHeld)
                    continue;
                if (Vector3.Distance(cell.transform.position, snapPoint.position) <= proximityRadius)
                    Accept(cell);
            }
        }

        public static void ShowPlaceHint(bool show)
        {
            var sockets = FindObjectsByType<BuoyPowerSocket>(FindObjectsSortMode.None);
            foreach (var socket in sockets)
            {
                if (socket != null)
                    socket.SetPlaceHint(show && !socket.connected);
            }
        }

        void SetPlaceHint(bool show)
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

            var ring = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            ring.name = "Ring";
            ring.transform.SetParent(root.transform, false);
            ring.transform.localScale = new Vector3(2.4f, 0.08f, 2.4f);
            var col = ring.GetComponent<Collider>();
            if (col != null)
                Destroy(col);

            var renderer = ring.GetComponent<Renderer>();
            if (renderer != null)
            {
                var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
                var mat = new Material(shader);
                var color = new Color(0.15f, 1f, 0.45f);
                if (mat.HasProperty("_BaseColor"))
                    mat.SetColor("_BaseColor", color);
                if (mat.HasProperty("_EmissionColor"))
                {
                    mat.EnableKeyword("_EMISSION");
                    mat.SetColor("_EmissionColor", color * 2.2f);
                }
                mat.color = color;
                renderer.sharedMaterial = mat;
            }

            var lightGo = new GameObject("HintLight");
            lightGo.transform.SetParent(root.transform, false);
            var light = lightGo.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = new Color(0.2f, 1f, 0.45f);
            light.intensity = 4f;
            light.range = 6f;
            light.shadows = LightShadows.None;

            root.AddComponent<PlaceHintPulse>();
            return root.transform;
        }

        public bool TryInsert(PowerCell cell)
        {
            if (cell == null || connected)
                return false;
            return Accept(cell);
        }

        void OnSocketSelect(SelectEnterEventArgs args)
        {
            var cell = args.interactableObject.transform.GetComponent<PowerCell>();
            if (cell == null)
            {
                MissionEvents.RaiseFeedback("That battery does not fit. The buoy needs the buoy battery.");
                GameAudio.PlayInvalid(transform.position);
                return;
            }

            Accept(cell);
        }

        bool Accept(PowerCell cell)
        {
            if (connected || MissionController.Instance == null)
                return false;

            if (!MissionController.Instance.TryRestoreBuoy())
            {
                GameAudio.PlayInvalid(transform.position);
                return false;
            }

            connected = true;
            cell.transform.SetParent(snapPoint, true);
            cell.transform.SetPositionAndRotation(snapPoint.position, snapPoint.rotation);
            RigidbodyUtil.ParkKinematic(cell.GetComponent<Rigidbody>());
            var grab = cell.GetComponent<XRGrabInteractable>();
            if (grab != null)
                grab.enabled = false;
            if (buoy != null)
                buoy.SetRestored(true);
            ShowConnected();
            cell.gameObject.SetActive(false);

            MissionEvents.RaiseFeedback("Buoy battery connected.");
            return true;
        }

        void ShowConnected()
        {
            SetPlaceHint(false);
            Tint(gameObject, new Color(0.15f, 0.95f, 0.4f));

            if (transform.Find("ConnectedLight") == null)
            {
                var lightGo = new GameObject("ConnectedLight");
                lightGo.transform.SetParent(transform, false);
                var light = lightGo.AddComponent<Light>();
                light.type = LightType.Point;
                light.color = new Color(0.2f, 1f, 0.45f);
                light.intensity = 3f;
                light.range = 5f;
                light.shadows = LightShadows.None;
            }

            var beacon = transform.parent != null ? transform.parent.Find("SeeBeacon") : null;
            if (beacon != null)
                beacon.gameObject.SetActive(false);

            if (buoy != null)
            {
                var glow = buoy.transform.Find("GlowLight");
                if (glow != null)
                    glow.gameObject.SetActive(false);
            }
        }

        static void Tint(GameObject go, Color color)
        {
            var renderer = go.GetComponent<Renderer>();
            if (renderer == null)
                return;
            foreach (var mat in renderer.materials)
            {
                if (mat == null)
                    continue;
                if (mat.HasProperty("_BaseColor"))
                    mat.SetColor("_BaseColor", color);
                else
                    mat.color = color;
            }
        }
    }

    /// <summary>Pulses the buoy socket ring while the buoy battery is being carried.</summary>
    sealed class PlaceHintPulse : MonoBehaviour
    {
        Light hintLight;

        void Awake()
        {
            hintLight = GetComponentInChildren<Light>();
        }

        void Update()
        {
            var pulse = 0.5f + 0.5f * Mathf.Sin(Time.time * 5f);
            var ring = transform.Find("Ring");
            if (ring != null)
                ring.localScale = new Vector3(2.2f + pulse * 0.45f, 0.08f, 2.2f + pulse * 0.45f);
            if (hintLight != null)
                hintLight.intensity = 2.5f + pulse * 3f;
        }
    }
}
