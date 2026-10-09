using ReefExplorer.Core;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace ReefExplorer.Interaction
{
    /// <summary>
    /// One spare pack for the station dive craft. It does not power the buoy.
    /// </summary>
    [RequireComponent(typeof(XRGrabInteractable))]
    public sealed class VehiclePowerPack : MonoBehaviour
    {
        [SerializeField] XRGrabInteractable grab;
        static AudioClip startClip;
        static bool restartHooked;

        public static bool IsInstalled { get; private set; }

        public static void MarkInstalled() => IsInstalled = true;

        void Awake()
        {
            if (grab == null)
                grab = GetComponent<XRGrabInteractable>();
            if (!restartHooked)
            {
                MissionEvents.MissionRestarted += () => IsInstalled = false;
                restartHooked = true;
            }
        }

        void OnEnable()
        {
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
            ShowVehicleHint(true);
            MissionEvents.RaiseFeedback("Craft battery. Press E at the dive vehicle to set it on the craft.");
        }

        void OnDrop(SelectExitEventArgs _)
        {
            ShowVehicleHint(false);
        }

        public static void ShowVehicleHint(bool show)
        {
            var craft = GameObject.Find("StationDiveCraft");
            if (craft == null)
                return;

            var hint = craft.transform.Find("VehiclePlaceHint");
            if (hint == null && show)
                hint = CreateHint(craft.transform);
            if (hint != null)
                hint.gameObject.SetActive(show);
        }

        public static void PlayStart(Vector3 position)
        {
            var sourceGo = new GameObject("VehicleStartSound");
            sourceGo.transform.position = position;
            var source = sourceGo.AddComponent<AudioSource>();
            source.spatialBlend = 1f;
            source.minDistance = 1.5f;
            source.maxDistance = 18f;
            source.clip = StartClip();
            source.Play();
            Destroy(sourceGo, source.clip.length + 0.2f);
        }

        static Transform CreateHint(Transform craft)
        {
            var root = new GameObject("VehiclePlaceHint");
            root.transform.SetParent(craft, false);
            root.transform.localPosition = Vector3.zero;

            var ring = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            ring.name = "Ring";
            ring.transform.SetParent(root.transform, false);
            ring.transform.localPosition = new Vector3(0f, -0.4f, 0f);
            ring.transform.localScale = new Vector3(2.4f, 0.04f, 2.4f);
            var col = ring.GetComponent<Collider>();
            if (col != null)
                Destroy(col);

            var renderer = ring.GetComponent<Renderer>();
            if (renderer != null)
            {
                var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
                var mat = new Material(shader);
                var color = new Color(1f, 0.45f, 0.05f);
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

            var lightGo = new GameObject("HintLight");
            lightGo.transform.SetParent(root.transform, false);
            var light = lightGo.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = new Color(1f, 0.5f, 0.1f);
            light.intensity = 3.5f;
            light.range = 7f;
            light.shadows = LightShadows.None;
            root.AddComponent<VehicleHintPulse>();
            return root.transform;
        }

        static AudioClip StartClip()
        {
            if (startClip != null)
                return startClip;

            const int rate = 22050;
            const float seconds = 1.5f;
            var count = (int)(rate * seconds);
            var clip = AudioClip.Create("VehicleStart", count, 1, rate, false);
            var data = new float[count];
            for (var i = 0; i < count; i++)
            {
                var t = i / (float)rate;
                var freq = Mathf.Lerp(48f, 130f, Mathf.Clamp01(t / 0.8f));
                var env = t < 0.06f ? t / 0.06f : Mathf.Lerp(1f, 0.25f, Mathf.Clamp01((t - 0.85f) / 0.65f));
                var rumble = Mathf.Sin(2f * Mathf.PI * freq * t);
                var buzz = Mathf.Sin(2f * Mathf.PI * freq * 2.2f * t) * 0.4f;
                data[i] = (rumble + buzz) * env * 0.5f;
            }

            clip.SetData(data, 0);
            startClip = clip;
            return clip;
        }
    }

    sealed class VehicleHintPulse : MonoBehaviour
    {
        Light hintLight;

        void Awake() => hintLight = GetComponentInChildren<Light>();

        void Update()
        {
            var pulse = 0.5f + 0.5f * Mathf.Sin(Time.time * 4f);
            var ring = transform.Find("Ring");
            if (ring != null)
                ring.localScale = new Vector3(2.2f + pulse * 0.35f, 0.04f, 2.2f + pulse * 0.35f);
            if (hintLight != null)
                hintLight.intensity = 2f + pulse * 2.5f;
        }
    }
}
