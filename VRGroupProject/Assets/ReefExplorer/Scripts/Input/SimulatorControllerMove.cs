using System;
using System.Collections.Generic;
using System.Reflection;
using ReefExplorer.Core;
using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Simulation;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Gravity;

namespace ReefExplorer.Input
{
    /// <summary>
    /// Desktop-style controls for VR mode without a headset (XR Device Simulator).
    /// WASD move, Right Click look on/off, E or G grab/drop, Q drop, hold Left Click to use, Space jump.
    /// </summary>
    // Runs after the XR Device Simulator (-29991) and before the XR Interaction Manager (-105),
    // so interactors see this frame's simulated hand pose and buttons.
    [DefaultExecutionOrder(-200)]
    public sealed class SimulatorControllerMove : MonoBehaviour
    {
        const BindingFlags Members = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        const float MoveSpeed = 3.2f;
        const float LookSensitivity = 0.18f;
        const float AimRange = 8f;
        const float MinAimDistance = 0.6f;
        const float EmptyGrabTimeout = 0.25f;

        static readonly Vector3 LeftSlot = new(-0.2f, -0.28f, 0.32f);
        static readonly Vector3 RightSlot = new(0.18f, -0.18f, 0.35f);

        XRDeviceSimulator simulator;
        Type simType;
        object lifecycleManager;
        XROrigin origin;
        CharacterController body;
        GravityProvider gravity;
        Camera cam;
        NearFarInteractor rightInteractor;
        readonly List<Canvas> hiddenSimulatorCanvases = new();
        readonly RaycastHit[] aimHits = new RaycastHit[16];

        bool controlling;
        bool lookEnabled;
        bool gripHeld;
        float gripPressedAt;
        float yaw;
        float pitch;
        float verticalVelocity = -2f;
        Vector3 lastTutorialPos;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            if (FindAnyObjectByType<SimulatorControllerMove>() != null)
                return;

            new GameObject("SimulatorControllerMove").AddComponent<SimulatorControllerMove>();
        }

        void Update()
        {
            var shouldControl = ShouldControl();
            if (shouldControl != controlling)
            {
                if (shouldControl)
                    TakeControl();
                else
                    ReleaseControl();
            }

            if (!controlling)
                return;

            if (IsGameplayFrozen())
            {
                ShowCursor();
                gripHeld = false;
                WriteAndApply(Quaternion.Euler(pitch, yaw, 0f), false);
                return;
            }

            ReadButtons();
            Look();
            var headRot = Quaternion.Euler(pitch, yaw, 0f);
            Move();
            WriteAndApply(headRot, Mouse.current != null && Mouse.current.leftButton.isPressed);
            TrackTutorialMove();
        }

        void OnDisable()
        {
            if (controlling)
                ReleaseControl();
        }

        bool ShouldControl()
        {
            if (MissionController.Instance == null || MissionController.Instance.PlayMode != PlayModeType.XR)
                return false;
            if (MissionController.Instance.State is MissionState.Boot or MissionState.ModeSelect)
                return false;

            // A real headset drives its own pose and buttons.
            if (XRSettings.isDeviceActive)
                return false;

            if (simulator == null || !simulator.isActiveAndEnabled)
                simulator = FindAnyObjectByType<XRDeviceSimulator>();
            if (simulator == null || !simulator.isActiveAndEnabled)
                return false;

            var activeOrigin = FindAnyObjectByType<XROrigin>();
            return activeOrigin != null && activeOrigin.isActiveAndEnabled;
        }

        static bool IsGameplayFrozen()
        {
            return MissionController.Instance.State == MissionState.Paused ||
                   ReefExplorer.Environment.OxygenMeter.Failed ||
                   ReefExplorer.Environment.SubmarineDrive.IsDriving;
        }

        void TakeControl()
        {
            controlling = true;
            simType = simulator.GetType();
            lifecycleManager = simType.GetField("m_DeviceLifecycleManager", Members)?.GetValue(simulator);

            origin = FindAnyObjectByType<XROrigin>();
            body = origin.GetComponent<CharacterController>();
            gravity = origin.GetComponentInChildren<GravityProvider>(true);
            cam = origin.Camera;
            rightInteractor = null;
            foreach (var interactor in origin.GetComponentsInChildren<NearFarInteractor>(true))
            {
                if (interactor.handedness == InteractorHandedness.Right)
                    rightInteractor = interactor;
            }

            // The simulator's own bindings (Space, Shift, 1-3, WASD head drift, mouse) clash with ours.
            simulator.deviceSimulatorActionAsset?.Disable();
            simulator.controllerActionAsset?.Disable();
            simulator.handActionAsset?.Disable();
            ClearSimulatorInputs();
            HideSimulatorHelpPanel(true);
            ShowControllerModels();

            yaw = 0f;
            pitch = 0f;
            gripHeld = false;
            lookEnabled = true;
            verticalVelocity = -2f;
            lastTutorialPos = origin.transform.position;
        }

        void ReleaseControl()
        {
            controlling = false;
            gripHeld = false;
            if (simulator != null)
            {
                simulator.deviceSimulatorActionAsset?.Enable();
                simulator.controllerActionAsset?.Enable();
                simulator.handActionAsset?.Enable();
            }

            HideSimulatorHelpPanel(false);
            ShowCursor();
        }

        void ClearSimulatorInputs()
        {
            SetField("m_MouseDeltaInput", Vector2.zero);
            SetField("m_MouseScrollInput", Vector2.zero);
            SetField("m_KeyboardXTranslateInput", 0f);
            SetField("m_KeyboardYTranslateInput", 0f);
            SetField("m_KeyboardZTranslateInput", 0f);
            SetField("m_Axis2DInput", Vector2.zero);
            SetField("m_GripInput", false);
            SetField("m_TriggerInput", false);

            var target = simType.GetField("m_TargetedDeviceInput", Members);
            if (target != null)
                target.SetValue(simulator, Enum.Parse(target.FieldType, "FPS"));
        }

        void HideSimulatorHelpPanel(bool hide)
        {
            if (!hide)
            {
                foreach (var canvas in hiddenSimulatorCanvases)
                {
                    if (canvas != null)
                        canvas.enabled = true;
                }

                hiddenSimulatorCanvases.Clear();
                return;
            }

            // Its panel lists the simulator's default keys, which no longer apply here.
            foreach (var mb in FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include))
            {
                if (mb == null || mb.GetType().Name != "XRDeviceSimulatorUI")
                    continue;
                foreach (var canvas in mb.GetComponentsInChildren<Canvas>(true))
                {
                    if (!canvas.enabled)
                        continue;
                    canvas.enabled = false;
                    hiddenSimulatorCanvases.Add(canvas);
                }
            }
        }

        void ShowControllerModels()
        {
            foreach (var t in origin.GetComponentsInChildren<Transform>(true))
            {
                if (t.name.Contains("Controller Visual") || t.name == "Left Controller" || t.name == "Right Controller")
                    t.gameObject.SetActive(true);
            }
        }

        void ReadButtons()
        {
            var keyboard = Keyboard.current;
            if (keyboard == null)
                return;

            if (keyboard.eKey.wasPressedThisFrame || keyboard.gKey.wasPressedThisFrame)
            {
                gripHeld = !gripHeld;
                gripPressedAt = Time.unscaledTime;
            }

            if (keyboard.qKey.wasPressedThisFrame)
                gripHeld = false;

            // Grab pressed with nothing under the crosshair: open the hand again instead of staying closed.
            if (gripHeld && rightInteractor != null && !rightInteractor.hasSelection &&
                Time.unscaledTime - gripPressedAt > EmptyGrabTimeout)
            {
                gripHeld = false;
                MissionEvents.RaiseFeedback("Nothing to grab. Aim the crosshair at a tool, then press E.");
            }
        }

        void Look()
        {
            var mouse = Mouse.current;
            if (mouse == null)
                return;

            if (mouse.rightButton.wasPressedThisFrame)
                lookEnabled = !lookEnabled;

            if (!lookEnabled)
            {
                ShowCursor();
                return;
            }

            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            var delta = mouse.delta.ReadValue() * LookSensitivity;
            yaw += delta.x;
            pitch = Mathf.Clamp(pitch - delta.y, -80f, 80f);
        }

        void Move()
        {
            if (body == null || !body.enabled || cam == null)
                return;

            var input = Vector2.zero;
            var keyboard = Keyboard.current;
            if (keyboard != null)
            {
                if (keyboard.wKey.isPressed) input.y += 1f;
                if (keyboard.sKey.isPressed) input.y -= 1f;
                if (keyboard.dKey.isPressed) input.x += 1f;
                if (keyboard.aKey.isPressed) input.x -= 1f;
            }

            if (input.sqrMagnitude > 0.01f)
            {
                var forward = Vector3.ProjectOnPlane(cam.transform.forward, Vector3.up).normalized;
                var right = Vector3.ProjectOnPlane(cam.transform.right, Vector3.up).normalized;
                var planar = (right * input.x + forward * input.y).normalized * MoveSpeed;
                body.Move(planar * Time.deltaTime);
            }

            ApplyJump(keyboard != null && keyboard.spaceKey.wasPressedThisFrame);
        }

        void ApplyJump(bool wantJump)
        {
            var grounded = body.isGrounded || OnFloor(body);

            if (wantJump && grounded && verticalVelocity <= 0.1f)
            {
                verticalVelocity = 5.5f;
                if (gravity != null)
                    gravity.useGravity = false;
            }
            else if (grounded && verticalVelocity <= 0f)
            {
                verticalVelocity = -2f;
                if (gravity != null && !gravity.useGravity)
                    gravity.useGravity = true;
                return;
            }

            if (gravity != null && gravity.useGravity)
                return;

            verticalVelocity += -18f * Time.deltaTime;
            body.Move(Vector3.up * (verticalVelocity * Time.deltaTime));
        }

        static bool OnFloor(CharacterController controller)
        {
            var start = controller.transform.position + Vector3.up * 0.25f;
            return Physics.SphereCast(start, 0.2f, Vector3.down, out _, 0.45f, ~0, QueryTriggerInteraction.Ignore);
        }

        void WriteAndApply(Quaternion headRot, bool trigger)
        {
            if (lifecycleManager == null)
                return;

            var hmdField = simType.GetField("m_HMDState", Members);
            if (hmdField == null)
                return;

            var hmd = hmdField.GetValue(simulator);
            var hmdType = hmd.GetType();
            var headPos = (Vector3)hmdType.GetField("centerEyePosition").GetValue(hmd);
            hmdType.GetField("centerEyeRotation").SetValue(hmd, headRot);
            hmdType.GetField("deviceRotation").SetValue(hmd, headRot);
            hmdField.SetValue(simulator, hmd);
            // Keeps the simulator's own update from snapping the head back next frame.
            SetField("m_CenterEyeEuler", new Vector3(pitch, yaw, 0f));

            var leftPos = headPos + headRot * LeftSlot;
            var rightPos = headPos + headRot * RightSlot;
            var rightRot = AimRotation(headPos, headRot, rightPos);

            WriteController("m_LeftControllerState", leftPos, headRot, false, false);
            WriteController("m_RightControllerState", rightPos, rightRot, gripHeld, trigger);

            var managerType = lifecycleManager.GetType();
            managerType.GetMethod("ApplyHMDState", Members)?.Invoke(lifecycleManager, new[] { hmd });
            managerType.GetMethod("ApplyControllerState", Members)?.Invoke(lifecycleManager, new[]
            {
                simType.GetField("m_LeftControllerState", Members)?.GetValue(simulator),
                simType.GetField("m_RightControllerState", Members)?.GetValue(simulator)
            });
        }

        /// <summary>
        /// Points the right hand at whatever sits under the screen-centre crosshair,
        /// so the grab ray and the scanner beam land where the player is looking.
        /// </summary>
        Quaternion AimRotation(Vector3 headPos, Quaternion headRot, Vector3 handPos)
        {
            var trackingSpace = cam != null ? cam.transform.parent : null;
            if (trackingSpace == null)
                return headRot;

            var worldHead = trackingSpace.TransformPoint(headPos);
            var worldForward = trackingSpace.rotation * (headRot * Vector3.forward);
            var aimPoint = worldHead + worldForward * AimRange;

            var count = Physics.RaycastNonAlloc(worldHead, worldForward, aimHits, AimRange, ~0,
                QueryTriggerInteraction.Ignore);
            var best = AimRange;
            for (var i = 0; i < count; i++)
            {
                var hit = aimHits[i];
                if (hit.distance >= best || IsOwnCollider(hit.collider))
                    continue;
                best = hit.distance;
                aimPoint = hit.point;
            }

            if (best < MinAimDistance)
                aimPoint = worldHead + worldForward * MinAimDistance;

            var localTarget = trackingSpace.InverseTransformPoint(aimPoint);
            var direction = localTarget - handPos;
            return direction.sqrMagnitude > 0.0001f
                ? Quaternion.LookRotation(direction, headRot * Vector3.up)
                : headRot;
        }

        bool IsOwnCollider(Collider col)
        {
            if (col.transform.IsChildOf(origin.transform))
                return true;
            if (rightInteractor == null)
                return false;
            foreach (var held in rightInteractor.interactablesSelected)
            {
                if (held != null && col.transform.IsChildOf(held.transform))
                    return true;
            }

            return false;
        }

        void WriteController(string stateField, Vector3 position, Quaternion rotation, bool grip, bool trigger)
        {
            var field = simType.GetField(stateField, Members);
            if (field == null)
                return;

            var state = field.GetValue(simulator);
            var stateType = state.GetType();
            stateType.GetField("devicePosition")?.SetValue(state, position);
            stateType.GetField("deviceRotation")?.SetValue(state, rotation);
            stateType.GetField("isTracked")?.SetValue(state, true);
            stateType.GetField("trackingState")?.SetValue(state, (int)(InputTrackingState.Position | InputTrackingState.Rotation));
            stateType.GetField("primary2DAxis")?.SetValue(state, Vector2.zero);
            stateType.GetField("secondary2DAxis")?.SetValue(state, Vector2.zero);

            var buttonsField = stateType.GetField("buttons");
            var buttons = buttonsField != null ? (ushort)buttonsField.GetValue(state) : (ushort)0;
            buttons = WithBit(buttons, ControllerButton.GripButton, grip);
            buttons = WithBit(buttons, ControllerButton.TriggerButton, trigger);
            buttonsField?.SetValue(state, buttons);
            stateType.GetField("grip")?.SetValue(state, grip ? 1f : 0f);
            stateType.GetField("trigger")?.SetValue(state, trigger ? 1f : 0f);
            field.SetValue(simulator, state);
        }

        static ushort WithBit(ushort buttons, ControllerButton button, bool on)
        {
            var bit = (ushort)(1 << (int)button);
            return on ? (ushort)(buttons | bit) : (ushort)(buttons & ~bit);
        }

        void SetField(string name, object value)
        {
            simType.GetField(name, Members)?.SetValue(simulator, value);
        }

        void TrackTutorialMove()
        {
            if (origin == null || MissionController.Instance.State != MissionState.TutorialMove)
                return;

            if (Vector3.Distance(lastTutorialPos, origin.transform.position) > 1f)
            {
                MissionController.Instance.NotifyTutorialStep("move");
                lastTutorialPos = origin.transform.position;
            }
        }

        static void ShowCursor()
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        void OnGUI()
        {
            if (!controlling || IsGameplayFrozen())
                return;

            var cx = Screen.width * 0.5f;
            var cy = Screen.height * 0.5f;
            GUI.color = gripHeld ? new Color(0.3f, 1f, 0.5f) : Color.white;
            GUI.DrawTexture(new Rect(cx - 8f, cy - 1f, 16f, 2f), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(cx - 1f, cy - 8f, 2f, 16f), Texture2D.whiteTexture);
            GUI.color = Color.white;

            const string help = "VR Simulator   WASD move  |  Right Click look on/off  |  E / G grab or drop  |  " +
                                "Hold Left Click use tool  |  Space jump";
            var style = new GUIStyle(GUI.skin.box) { fontSize = 14, alignment = TextAnchor.MiddleCenter };
            GUI.Box(new Rect(cx - 360f, Screen.height - 40f, 720f, 30f), help, style);
        }
    }
}