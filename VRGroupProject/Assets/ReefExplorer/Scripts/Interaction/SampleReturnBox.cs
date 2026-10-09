using ReefExplorer.Audio;
using ReefExplorer.Core;
using ReefExplorer.UI;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace ReefExplorer.Interaction
{
    /// <summary>
    /// Open box on the station table. It appears after every water sample is collected.
    /// </summary>
    public sealed class SampleReturnBox : MonoBehaviour
    {
        static SampleReturnBox instance;

        GameObject crate;
        Transform snap;
        SampleBottle stored;
        Transform hint;

        public static bool IsReady { get; private set; }
        public static bool IsDeposited { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            if (GameObject.Find("ResearchStation") == null)
                return;
            if (FindAnyObjectByType<SampleReturnBox>() != null)
                return;

            var host = new GameObject("SampleReturnBox");
            host.AddComponent<SampleReturnBox>();
        }

        void Awake()
        {
            instance = this;
            MissionEvents.MissionRestarted += OnRestart;
            MissionEvents.WaterSampleCollected += OnSample;
        }

        void OnDestroy()
        {
            if (instance == this)
                instance = null;
            MissionEvents.MissionRestarted -= OnRestart;
            MissionEvents.WaterSampleCollected -= OnSample;
        }

        void Start()
        {
            BuildCrate();
            Refresh();
        }

        void OnSample(string _) => Refresh();

        void Refresh()
        {
            var ready = MissionController.Instance != null && MissionController.Instance.HasAllWaterSamples();
            IsReady = ready;
            if (crate != null)
                crate.SetActive(ready || IsDeposited);

            if (ready && !IsDeposited)
            {
                var player = FindAnyObjectByType<ReefExplorer.Input.DesktopPlayerController>();
                if (player != null && player.HeldObject != null &&
                    player.HeldObject.GetComponent<SampleBottle>() != null)
                    ShowHint(true);
            }
        }

        public static bool IsNear(Vector3 from)
        {
            if (instance == null || instance.crate == null || !instance.crate.activeInHierarchy)
                return false;
            return Vector3.Distance(from, instance.crate.transform.position) <= 3.6f;
        }

        public static bool TryDeposit(SampleBottle bottle)
        {
            if (instance == null || bottle == null)
                return false;
            if (!IsReady)
            {
                MissionEvents.RaiseFeedback("Collect every water sample first.");
                return false;
            }
            if (IsDeposited)
            {
                MissionEvents.RaiseFeedback("The sample is already in the box.");
                return false;
            }

            var mc = MissionController.Instance;
            if (mc != null)
            {
                foreach (var sample in mc.DiveLog.perSiteSamples)
                {
                    if (sample != null && sample.collected && !sample.analysed)
                        mc.TryAnalyseSample(sample.siteId);
                }
            }

            bottle.transform.SetParent(instance.snap, false);
            bottle.transform.localPosition = Vector3.zero;
            bottle.transform.localRotation = Quaternion.identity;
            var body = bottle.GetComponent<Rigidbody>();
            if (body != null)
                RigidbodyUtil.ParkKinematic(body);
            var grab = bottle.GetComponent<XRGrabInteractable>();
            if (grab != null)
                grab.enabled = false;

            instance.stored = bottle;
            IsDeposited = true;
            ShowHint(false);
            GameAudio.PlayAnalyserAccept(instance.crate.transform.position);
            DiveReadout.ShowWaterTest();
            MissionEvents.RaiseFeedback("Water test is on the screen.");
            return true;
        }

        public static void ShowHint(bool show)
        {
            if (instance == null || instance.hint == null)
                return;
            if (show && (instance.crate == null || !instance.crate.activeInHierarchy || IsDeposited))
                show = false;
            instance.hint.gameObject.SetActive(show);
        }

        void OnRestart()
        {
            IsReady = false;
            IsDeposited = false;
            ShowHint(false);
            if (stored != null)
            {
                stored.transform.SetParent(null, true);
                var station = GameObject.Find("ResearchStation");
                var spot = station != null
                    ? station.transform.TransformPoint(new Vector3(0.3f, 1.14f, -1.78f))
                    : new Vector3(0.3f, 1.14f, -1.78f);
                stored.transform.SetPositionAndRotation(spot, Quaternion.identity);
                var grab = stored.GetComponent<XRGrabInteractable>();
                if (grab != null)
                    grab.enabled = true;
                stored.gameObject.SetActive(true);
                stored = null;
            }

            if (crate != null)
                crate.SetActive(false);
        }

        void BuildCrate()
        {
            var station = GameObject.Find("ResearchStation");
            var spot = station != null
                ? station.transform.TransformPoint(new Vector3(1.05f, 1.05f, -2.2f))
                : new Vector3(1.05f, 1.05f, -2.2f);

            crate = new GameObject("SampleCrate");
            crate.transform.SetParent(transform, false);
            crate.transform.position = spot;
            crate.transform.rotation = Quaternion.identity;

            var paint = new Color(0.78f, 0.9f, 0.98f);
            Wall(crate.transform, new Vector3(0f, 0.04f, 0f), new Vector3(0.52f, 0.08f, 0.4f), paint);
            Wall(crate.transform, new Vector3(0f, 0.16f, 0.18f), new Vector3(0.52f, 0.24f, 0.04f), paint);
            Wall(crate.transform, new Vector3(0f, 0.16f, -0.18f), new Vector3(0.52f, 0.24f, 0.04f), paint);
            Wall(crate.transform, new Vector3(0.24f, 0.16f, 0f), new Vector3(0.04f, 0.24f, 0.36f), paint);
            Wall(crate.transform, new Vector3(-0.24f, 0.16f, 0f), new Vector3(0.04f, 0.24f, 0.36f), paint);

            var snapGo = new GameObject("Snap");
            snapGo.transform.SetParent(crate.transform, false);
            snapGo.transform.localPosition = new Vector3(0f, 0.2f, 0f);
            snap = snapGo.transform;

            var glowGo = new GameObject("BoxLight");
            glowGo.transform.SetParent(crate.transform, false);
            glowGo.transform.localPosition = new Vector3(0f, 0.2f, 0f);
            var glow = glowGo.AddComponent<Light>();
            glow.type = LightType.Point;
            glow.color = new Color(0.45f, 0.85f, 1f);
            glow.intensity = 1.8f;
            glow.range = 2.4f;
            glow.shadows = LightShadows.None;

            hint = HintRing(crate.transform, new Color(0.35f, 0.9f, 1f));
            hint.gameObject.SetActive(false);
            crate.SetActive(false);
        }

        static void Wall(Transform parent, Vector3 localPos, Vector3 scale, Color color)
        {
            var wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wall.name = "Wall";
            wall.transform.SetParent(parent, false);
            wall.transform.localPosition = localPos;
            wall.transform.localScale = scale;
            var renderer = wall.GetComponent<Renderer>();
            if (renderer != null)
                renderer.sharedMaterial = Paint(color);
        }

        static Transform HintRing(Transform parent, Color color)
        {
            var root = new GameObject("PlaceHint");
            root.transform.SetParent(parent, false);
            root.transform.localPosition = new Vector3(0f, 0.02f, 0f);

            var ring = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            ring.name = "Ring";
            ring.transform.SetParent(root.transform, false);
            ring.transform.localScale = new Vector3(0.7f, 0.02f, 0.7f);
            var col = ring.GetComponent<Collider>();
            if (col != null)
                Destroy(col);
            var renderer = ring.GetComponent<Renderer>();
            if (renderer != null)
                renderer.sharedMaterial = Paint(color);

            var lightGo = new GameObject("HintLight");
            lightGo.transform.SetParent(root.transform, false);
            var light = lightGo.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = color;
            light.intensity = 2.4f;
            light.range = 3.5f;
            light.shadows = LightShadows.None;
            root.AddComponent<SampleBoxHintPulse>();
            return root.transform;
        }

        static Material Paint(Color color)
        {
            var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            var mat = new Material(shader);
            if (mat.HasProperty("_BaseColor"))
                mat.SetColor("_BaseColor", color);
            mat.color = color;
            return mat;
        }
    }

    sealed class SampleBoxHintPulse : MonoBehaviour
    {
        Light hintLight;

        void Awake() => hintLight = GetComponentInChildren<Light>();

        void Update()
        {
            var pulse = 0.65f + Mathf.PingPong(Time.time, 0.7f);
            transform.localScale = new Vector3(pulse, 1f, pulse);
            if (hintLight != null)
                hintLight.intensity = 1.6f + pulse;
        }
    }
}
