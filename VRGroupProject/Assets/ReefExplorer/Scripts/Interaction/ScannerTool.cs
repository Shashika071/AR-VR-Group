using ReefExplorer.Audio;
using ReefExplorer.Core;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace ReefExplorer.Interaction
{
    [RequireComponent(typeof(XRGrabInteractable))]
    public sealed class ScannerTool : MonoBehaviour
    {
        [SerializeField] string toolId = "scanner";
        [SerializeField] Transform rayOrigin;
        [SerializeField] float range = 8f;
        [SerializeField] float scanDuration = 2.1f;
        [SerializeField] LayerMask hitMask = ~0;
        [SerializeField] LineRenderer beam;

        XRGrabInteractable grab;
        float scanProgress;
        IScannable currentTarget;
        bool activateHeld;
        bool desktopActivate;
        bool desktopHeld;
        bool warnedInvalid;
        ScanLoadBar loadBar;

        public float ScanProgress01 => Mathf.Clamp01(scanProgress / Mathf.Max(0.01f, scanDuration));
        public bool IsHeld => desktopHeld || (grab != null && grab.isSelected);
        public bool IsActivated => activateHeld || desktopActivate;

        void Awake()
        {
            grab = GetComponent<XRGrabInteractable>();
            if (rayOrigin == null)
            {
                var beamTf = transform.Find("Beam");
                rayOrigin = beamTf != null ? beamTf : transform;
            }
            if (beam == null)
                beam = GetComponentInChildren<LineRenderer>(true);
            scanDuration = Mathf.Max(scanDuration, 2.1f);
        }

        void OnEnable()
        {
            grab.selectEntered.AddListener(OnGrabbed);
            grab.selectExited.AddListener(OnReleased);
            grab.activated.AddListener(OnActivated);
            grab.deactivated.AddListener(OnDeactivated);
            MissionEvents.MissionRestarted += OnMissionRestarted;
        }

        void OnDisable()
        {
            grab.selectEntered.RemoveListener(OnGrabbed);
            grab.selectExited.RemoveListener(OnReleased);
            grab.activated.RemoveListener(OnActivated);
            grab.deactivated.RemoveListener(OnDeactivated);
            MissionEvents.MissionRestarted -= OnMissionRestarted;
        }

        void Update()
        {
            if (!IsHeld)
            {
                StopScan();
                SetBeam(false, false, Vector3.zero);
                return;
            }

            if (!IsActivated)
            {
                StopScan();
                SetBeam(false, false, Vector3.zero);
                warnedInvalid = false;
                return;
            }

            if (FindScanHit(out var hit))
            {
                var hazard = hit.collider.GetComponentInParent<HazardFlag>();
                if (hazard != null && !hazard.IsScanned)
                {
                    SetBeam(true, false, hit.point);
                    HideBar();
                    if (!warnedInvalid)
                    {
                        warnedInvalid = true;
                        MissionEvents.RaiseFeedback("Use the disposal tool on the green cloud.");
                        GameAudio.PlayInvalid(transform.position);
                    }

                    StopScan();
                    return;
                }

                var scannable = hit.collider.GetComponentInParent<IScannable>();
                var valid = scannable != null && !scannable.IsScanned;
                SetBeam(true, valid, hit.point);

                if (!valid)
                {
                    HideBar();
                    if (!warnedInvalid)
                    {
                        warnedInvalid = true;
                        if (scannable != null && scannable.IsScanned)
                            MissionEvents.RaiseFeedback("Already scanned.");
                        else
                            MissionEvents.RaiseFeedback("Aim at coral, a fish, or another survey target.");
                        GameAudio.PlayInvalid(transform.position);
                    }

                    StopScan();
                    return;
                }

                warnedInvalid = false;

                if (currentTarget != scannable)
                {
                    currentTarget = scannable;
                    scanProgress = 0f;
                    GameAudio.PlayScannerStart(transform.position);
                }

                scanProgress += Time.deltaTime;
                GameAudio.PlayScannerProgress(transform.position, ScanProgress01);
                ShowBar(ScanProgress01);

                if (scanProgress >= scanDuration)
                {
                    if (currentTarget.TryScan())
                        GameAudio.PlayScannerSuccess(transform.position);
                    StopScan();
                }
            }
            else
            {
                HideBar();
                var origin = AimOrigin();
                var dir = AimDirection();
                SetBeam(true, false, origin + dir * range);
                StopScan();
            }
        }

        public void DesktopSetHeld(bool held) => desktopHeld = held;

        bool FindScanHit(out RaycastHit hit)
        {
            hit = default;
            var origin = AimOrigin();
            var dir = AimDirection();
            var hits = Physics.SphereCastAll(origin, 0.4f, dir, range, hitMask, QueryTriggerInteraction.Collide);
            System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
            foreach (var candidate in hits)
            {
                if (candidate.collider == null)
                    continue;
                if (candidate.collider.transform.IsChildOf(transform))
                    continue;
                if (candidate.collider.GetComponentInParent<IScannable>() == null &&
                    candidate.collider.GetComponentInParent<HazardFlag>() == null)
                    continue;
                hit = candidate;
                return true;
            }

            return false;
        }

        Vector3 AimOrigin()
        {
            if (desktopHeld && Camera.main != null)
                return Camera.main.transform.position + Camera.main.transform.forward * 0.55f;
            return rayOrigin != null ? rayOrigin.position : transform.position;
        }

        Vector3 AimDirection()
        {
            if (desktopHeld && Camera.main != null)
                return Camera.main.transform.forward;
            return rayOrigin != null ? rayOrigin.forward : transform.forward;
        }

        void ShowBar(float amount)
        {
            if (loadBar == null)
                loadBar = ScanLoadBar.Create();
            loadBar.Set(amount);
        }

        void HideBar()
        {
            if (loadBar != null)
                loadBar.Hide();
        }

        void OnGrabbed(SelectEnterEventArgs _)
        {
            MissionController.Instance?.NotifyToolPicked(toolId);
            if (MissionController.Instance != null &&
                MissionController.Instance.State == MissionState.TutorialGrab)
            {
                MissionController.Instance.NotifyTutorialStep("grab");
            }
        }

        void OnReleased(SelectExitEventArgs _)
        {
            activateHeld = false;
            desktopActivate = false;
            StopScan();
            SetBeam(false, false, Vector3.zero);
        }

        void OnActivated(ActivateEventArgs _)
        {
            activateHeld = true;
            if (MissionController.Instance != null &&
                MissionController.Instance.State == MissionState.TutorialActivate)
            {
                MissionController.Instance.NotifyTutorialStep("activate");
            }
        }

        void OnDeactivated(DeactivateEventArgs _) => activateHeld = false;

        public void DesktopSetActivated(bool activated) => desktopActivate = activated;

        void StopScan()
        {
            scanProgress = 0f;
            currentTarget = null;
            HideBar();
        }

        void SetBeam(bool enabledBeam, bool valid, Vector3 end)
        {
            if (beam == null)
                return;

            beam.enabled = enabledBeam;
            if (!enabledBeam)
                return;

            beam.positionCount = 2;
            beam.SetPosition(0, rayOrigin.position);
            beam.SetPosition(1, end);
            beam.startColor = valid ? new Color(0.2f, 0.95f, 0.75f, 0.9f) : new Color(0.95f, 0.45f, 0.2f, 0.85f);
            beam.endColor = beam.startColor;
        }

        void OnMissionRestarted()
        {
            activateHeld = false;
            desktopActivate = false;
            StopScan();
            SetBeam(false, false, Vector3.zero);
            HideBar();
        }
    }

    sealed class ScanLoadBar : MonoBehaviour
    {
        Image fill;

        public static ScanLoadBar Create()
        {
            var canvasGo = new GameObject("ScanLoadCanvas");
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 80;
            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);

            var root = new GameObject("ScanBar");
            root.transform.SetParent(canvasGo.transform, false);
            var rect = root.AddComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = new Vector2(0f, -80f);
            rect.sizeDelta = new Vector2(420f, 28f);
            var back = root.AddComponent<Image>();
            back.color = new Color(0.02f, 0.08f, 0.1f, 0.9f);
            back.sprite = White();

            var labelGo = new GameObject("Label");
            labelGo.transform.SetParent(root.transform, false);
            var labelRect = labelGo.AddComponent<RectTransform>();
            labelRect.anchorMin = new Vector2(0f, 1f);
            labelRect.anchorMax = new Vector2(1f, 1f);
            labelRect.pivot = new Vector2(0.5f, 0f);
            labelRect.anchoredPosition = new Vector2(0f, 4f);
            labelRect.sizeDelta = new Vector2(0f, 24f);
            var label = labelGo.AddComponent<Text>();
            label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            label.fontSize = 18;
            label.alignment = TextAnchor.MiddleCenter;
            label.color = Color.white;
            label.text = "Scanning";

            var fillGo = new GameObject("Fill");
            fillGo.transform.SetParent(root.transform, false);
            var fillRect = fillGo.AddComponent<RectTransform>();
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = Vector2.one;
            fillRect.offsetMin = new Vector2(4f, 4f);
            fillRect.offsetMax = new Vector2(-4f, -4f);
            var fill = fillGo.AddComponent<Image>();
            fill.sprite = White();
            fill.type = Image.Type.Filled;
            fill.fillMethod = Image.FillMethod.Horizontal;
            fill.color = new Color(0.2f, 0.95f, 0.75f);
            fill.fillAmount = 0f;

            var bar = canvasGo.AddComponent<ScanLoadBar>();
            bar.fill = fill;
            root.SetActive(false);
            return bar;
        }

        public void Set(float amount)
        {
            if (fill == null)
                return;
            fill.transform.parent.gameObject.SetActive(true);
            fill.fillAmount = Mathf.Clamp01(amount);
        }

        public void Hide()
        {
            if (fill != null)
                fill.transform.parent.gameObject.SetActive(false);
        }

        static Sprite White()
        {
            var tex = Texture2D.whiteTexture;
            return Sprite.Create(tex, new Rect(0f, 0f, tex.width, tex.height), new Vector2(0.5f, 0.5f));
        }
    }
}
