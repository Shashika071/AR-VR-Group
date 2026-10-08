using ReefExplorer.Audio;
using ReefExplorer.Core;
using UnityEngine;
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
        [SerializeField] float scanDuration = 1.25f;
        [SerializeField] LayerMask hitMask = ~0;
        [SerializeField] LineRenderer beam;

        XRGrabInteractable grab;
        float scanProgress;
        IScannable currentTarget;
        bool activateHeld;
        bool desktopActivate;
        bool warnedInvalid;

        public float ScanProgress01 => Mathf.Clamp01(scanProgress / Mathf.Max(0.01f, scanDuration));
        public bool IsHeld => grab != null && grab.isSelected;
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

            if (Physics.Raycast(rayOrigin.position, rayOrigin.forward, out var hit, range, hitMask,
                    QueryTriggerInteraction.Ignore))
            {
                var scannable = hit.collider.GetComponentInParent<IScannable>();
                var valid = scannable != null && !scannable.IsScanned;
                SetBeam(true, valid, hit.point);

                if (!valid)
                {
                    if (!warnedInvalid)
                    {
                        warnedInvalid = true;
                        if (scannable != null && scannable.IsScanned)
                            MissionEvents.RaiseFeedback("Already scanned.");
                        else
                            MissionEvents.RaiseFeedback("Aim at a survey point, animal, or hazard.");
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

                if (scanProgress >= scanDuration)
                {
                    if (currentTarget.TryScan())
                        GameAudio.PlayScannerSuccess(transform.position);
                    StopScan();
                }
            }
            else
            {
                SetBeam(true, false, rayOrigin.position + rayOrigin.forward * range);
                StopScan();
            }
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
        }
    }
}
