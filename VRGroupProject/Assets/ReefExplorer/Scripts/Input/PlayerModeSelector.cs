using ReefExplorer.Core;
using UnityEngine;
using UnityEngine.XR;
using UnityEngine.UI;
using Unity.XR.CoreUtils;
using UnityEngine.XR.Interaction.Toolkit.UI;

namespace ReefExplorer.Input
{
    /// <summary>
    /// Ensures XR and Desktop controllers are mutually exclusive (one camera / AudioListener).
    /// Keeps a camera active during mode select so the Game view is never black.
    /// </summary>
    public sealed class PlayerModeSelector : MonoBehaviour
    {
        [SerializeField] GameObject xrOriginRoot;
        [SerializeField] GameObject desktopPlayerRoot;
        [SerializeField] bool preferXrWhenDevicePresent = true;

        void Awake()
        {
            ResolveRoots();

            // Desktop camera stays on for the menu so "No cameras rendering" cannot happen.
            // Gameplay input still waits until ChooseDesktop / ChooseXr sets PlayMode.
            SetRoots(false, true);
            FaceMissionBoard(desktopPlayerRoot);
            EnsureMainCameraTag(desktopPlayerRoot);
            BindWorldCanvasCamera();
            SetSimulatorVisible(false);
        }

        void OnEnable()
        {
            MissionEvents.MissionRestarted += OnMissionRestarted;
        }

        void OnDisable()
        {
            MissionEvents.MissionRestarted -= OnMissionRestarted;
        }

        public void ChooseXr()
        {
            SetRoots(true, false);
            FaceMissionBoard(xrOriginRoot);
            EnsureMainCameraTag(xrOriginRoot);
            BindWorldCanvasCamera();
            SetSimulatorVisible(true);
            MissionController.Instance?.SelectPlayMode(PlayModeType.XR);
            MissionEvents.RaiseFeedback("VR / Simulator mode selected. Click Start Dive.");
        }

        public void ChooseDesktop()
        {
            ChooseDesktop(faceBoard: true);
        }

        /// <param name="faceBoard">
        /// Only true for the menu button / first pick. Never true while WASD is moving —
        /// facing the board snaps yaw and feels like a "reset" on D.
        /// </param>
        public void ChooseDesktop(bool faceBoard)
        {
            var alreadyDesktop =
                MissionController.Instance != null &&
                MissionController.Instance.PlayMode == PlayModeType.Desktop;

            SetRoots(false, true);
            EnsureMainCameraTag(desktopPlayerRoot);
            BindWorldCanvasCamera();
            SetSimulatorVisible(false);

            // Never re-aim the player if Desktop is already active.
            if (!alreadyDesktop && faceBoard)
                FaceMissionBoard(desktopPlayerRoot);

            MissionController.Instance?.SelectPlayMode(PlayModeType.Desktop);

            if (!alreadyDesktop)
                MissionEvents.RaiseFeedback("Desktop mode selected. Click Start Dive.");
        }

        public void AutoDetectOrShowChooser()
        {
            if (preferXrWhenDevicePresent && IsXrDevicePresent())
                ChooseXr();
            else
                ShowModeSelectCamera();
        }

        void OnMissionRestarted()
        {
            ShowModeSelectCamera();
        }

        void ShowModeSelectCamera()
        {
            SetRoots(false, true);
            FaceMissionBoard(desktopPlayerRoot);
            EnsureMainCameraTag(desktopPlayerRoot);
            BindWorldCanvasCamera();
        }

        void ResolveRoots()
        {
            if (xrOriginRoot == null)
            {
                var origin = FindAnyObjectByType<XROrigin>(FindObjectsInactive.Include);
                if (origin != null)
                    xrOriginRoot = origin.gameObject;
            }

            if (desktopPlayerRoot == null)
            {
                var desktop = FindAnyObjectByType<DesktopPlayerController>(FindObjectsInactive.Include);
                if (desktop != null)
                    desktopPlayerRoot = desktop.gameObject;
            }
        }

        void SetRoots(bool xr, bool desktop)
        {
            if (xrOriginRoot != null)
                xrOriginRoot.SetActive(xr);
            if (desktopPlayerRoot != null)
                desktopPlayerRoot.SetActive(desktop);
        }

        static void FaceMissionBoard(GameObject root)
        {
            if (root == null)
                return;

            var board = GameObject.Find("MissionCanvas");
            if (board == null)
                board = GameObject.Find("MissionBoard");
            if (board == null)
                return;

            var pos = root.transform.position;
            var target = board.transform.position;
            var dir = target - pos;
            dir.y = 0f;
            if (dir.sqrMagnitude < 0.001f)
                return;

            root.transform.rotation = Quaternion.LookRotation(dir.normalized, Vector3.up);
        }

        static void EnsureMainCameraTag(GameObject root)
        {
            if (root == null)
                return;

            var cams = root.GetComponentsInChildren<Camera>(true);
            if (cams.Length == 0)
                return;

            // Prefer an enabled camera; fall back to the first one.
            Camera chosen = null;
            foreach (var cam in cams)
            {
                if (cam.enabled)
                {
                    chosen = cam;
                    break;
                }
            }

            chosen ??= cams[0];
            chosen.tag = "MainCamera";
            chosen.enabled = true;

            if (chosen.GetComponent<AudioListener>() == null)
                chosen.gameObject.AddComponent<AudioListener>();
        }

        void BindWorldCanvasCamera()
        {
            var main = Camera.main;
            if (main == null)
                return;

            var usingXr = xrOriginRoot != null && xrOriginRoot.activeInHierarchy;
            var canvases = FindObjectsByType<Canvas>(FindObjectsInactive.Include);
            foreach (var canvas in canvases)
            {
                if (canvas.renderMode != RenderMode.WorldSpace)
                    continue;

                canvas.worldCamera = main;

                // Desktop: GraphicRaycaster only. XR: enable tracked raycaster for controller UI.
                var tracked = canvas.GetComponent<TrackedDeviceGraphicRaycaster>();
                var graphic = canvas.GetComponent<GraphicRaycaster>();
                if (tracked != null)
                    tracked.enabled = usingXr;
                if (graphic != null)
                    graphic.enabled = true;
            }
        }

        static bool IsXrDevicePresent()
        {
            var devices = new System.Collections.Generic.List<InputDevice>();
            InputDevices.GetDevicesWithCharacteristics(
                InputDeviceCharacteristics.HeadMounted,
                devices);
            return devices.Count > 0;
        }

        static void SetSimulatorVisible(bool visible)
        {
            // Hide XR Device Simulator overlay/objects while using desktop mode.
            foreach (var mb in FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include))
            {
                if (mb == null)
                    continue;
                var typeName = mb.GetType().Name;
                if (typeName.Contains("XRDeviceSimulator") || typeName.Contains("XRInteractionSimulator"))
                    mb.gameObject.SetActive(visible);
            }
        }
    }
}
