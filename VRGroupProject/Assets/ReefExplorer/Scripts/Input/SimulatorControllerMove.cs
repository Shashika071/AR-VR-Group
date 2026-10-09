using System.Reflection;
using ReefExplorer.Core;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Simulation;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Gravity;

namespace ReefExplorer.Input
{
    /// <summary>
    /// Editor VR hands. 1 = left, 2 = right, 3 = both.
    /// Mouse slides the chosen hand. Left click or G grabs.
    /// Hold the right mouse button to look.
    /// </summary>
    [DefaultExecutionOrder(-200)]
    public sealed class SimulatorControllerMove : MonoBehaviour
    {
        enum HandChoice
        {
            Left,
            Right,
            Both
        }

        const float Slide = 0.0022f;
        static readonly Vector3 LeftSlot = new(-0.16f, -0.12f, 0.4f);
        static readonly Vector3 RightSlot = new(0.16f, -0.12f, 0.4f);

        XRDeviceSimulator simulator;
        HandChoice choice = HandChoice.Both;
        Vector2 leftOffset;
        Vector2 rightOffset;
        float verticalVelocity = -2f;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            if (FindAnyObjectByType<SimulatorControllerMove>() != null)
                return;

            var go = new GameObject("SimulatorControllerMove");
            go.AddComponent<SimulatorControllerMove>();
        }

        void Update()
        {
            if (!IsXrDiving() || ReefExplorer.Environment.OxygenMeter.Failed || ReefExplorer.Environment.SubmarineDrive.IsDriving)
            {
                ShowCursor();
                leftOffset = Vector2.zero;
                rightOffset = Vector2.zero;
                return;
            }

            if (simulator == null || !simulator.isActiveAndEnabled)
                simulator = FindAnyObjectByType<XRDeviceSimulator>();

            ReadChoice();

            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;

            var mouse = Mouse.current;
            var keyboard = Keyboard.current;
            var looking = mouse != null && mouse.rightButton.isPressed;
            var grabbing = (mouse != null && mouse.leftButton.isPressed) ||
                           (keyboard != null && keyboard.gKey.isPressed);

            if (simulator != null)
                SetField(simulator, "m_GripInput", grabbing);

            if (!looking && simulator != null && mouse != null)
            {
                var delta = mouse.delta.ReadValue();
                SetField(simulator, "m_MouseDeltaInput", Vector2.zero);
                SlideChosenHands(delta);
            }

            PoseHands(grabbing);
            if (simulator != null)
                ApplyControllers(simulator);
        }

        void LateUpdate()
        {
            if (!IsXrDiving() || simulator == null || !simulator.isActiveAndEnabled)
                return;

            var origin = GameObject.Find("XR Origin (XR Rig)");
            if (origin != null)
                ShowControllerModels(origin);

            var grabbing = (Mouse.current != null && Mouse.current.leftButton.isPressed) ||
                           (Keyboard.current != null && Keyboard.current.gKey.isPressed);
            PoseHands(grabbing);
            ApplyControllers(simulator);
            ApplyJump();
        }

        void OnGUI()
        {
            if (!IsXrDiving())
                return;

            const float width = 150f;
            var x = (Screen.width - width * 3f - 16f) * 0.5f;
            var y = Screen.height - 48f;

            if (ChoiceButton(new Rect(x, y, width, 34f), "Left  (1)", choice == HandChoice.Left))
                choice = HandChoice.Left;
            if (ChoiceButton(new Rect(x + width + 8f, y, width, 34f), "Right  (2)", choice == HandChoice.Right))
                choice = HandChoice.Right;
            if (ChoiceButton(new Rect(x + (width + 8f) * 2f, y, width, 34f), "Both  (3)", choice == HandChoice.Both))
                choice = HandChoice.Both;
        }

        static bool ChoiceButton(Rect rect, string label, bool selected)
        {
            var previous = GUI.backgroundColor;
            GUI.backgroundColor = selected ? new Color(0.2f, 0.75f, 0.85f) : new Color(0.15f, 0.2f, 0.24f);
            var pressed = GUI.Button(rect, label);
            GUI.backgroundColor = previous;
            return pressed;
        }

        void ApplyJump()
        {
            var origin = GameObject.Find("XR Origin (XR Rig)");
            if (origin == null)
                return;

            var body = origin.GetComponent<CharacterController>();
            if (body == null)
                return;

            var gravity = origin.GetComponentInChildren<GravityProvider>(true);
            var wantJump = Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame;
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
            body.Move(Vector3.up * verticalVelocity * Time.deltaTime);
        }

        static bool OnFloor(CharacterController body)
        {
            var origin = body.transform.position + Vector3.up * 0.25f;
            return Physics.SphereCast(origin, 0.2f, Vector3.down, out _, 0.45f, ~0, QueryTriggerInteraction.Ignore);
        }

        void ReadChoice()
        {
            var keyboard = Keyboard.current;
            if (keyboard == null)
                return;

            if (keyboard.digit1Key.wasPressedThisFrame || keyboard.numpad1Key.wasPressedThisFrame)
                choice = HandChoice.Left;
            else if (keyboard.digit2Key.wasPressedThisFrame || keyboard.numpad2Key.wasPressedThisFrame)
                choice = HandChoice.Right;
            else if (keyboard.digit3Key.wasPressedThisFrame || keyboard.numpad3Key.wasPressedThisFrame)
                choice = HandChoice.Both;
        }

        void SlideChosenHands(Vector2 delta)
        {
            var step = delta * Slide;
            if (choice is HandChoice.Left or HandChoice.Both)
            {
                leftOffset.x = Mathf.Clamp(leftOffset.x + step.x, -0.4f, 0.4f);
                leftOffset.y = Mathf.Clamp(leftOffset.y + step.y, -0.4f, 0.4f);
            }

            if (choice is HandChoice.Right or HandChoice.Both)
            {
                rightOffset.x = Mathf.Clamp(rightOffset.x + step.x, -0.4f, 0.4f);
                rightOffset.y = Mathf.Clamp(rightOffset.y + step.y, -0.4f, 0.4f);
            }
        }

        void PoseHands(bool grabbing)
        {
            if (simulator == null || !TryReadHmd(simulator, out var headPos, out var headRot))
                return;

            var grabLeft = grabbing && choice is HandChoice.Left or HandChoice.Both;
            var grabRight = grabbing && choice is HandChoice.Right or HandChoice.Both;

            // Locked to the view, like a gun: walk and turn, and the hands stay in the center.
            var leftPos = headPos + headRot * (LeftSlot + new Vector3(leftOffset.x, leftOffset.y, 0f));
            var rightPos = headPos + headRot * (RightSlot + new Vector3(rightOffset.x, rightOffset.y, 0f));
            WriteController(simulator, "m_LeftControllerState", leftPos, headRot, grabLeft);
            WriteController(simulator, "m_RightControllerState", rightPos, headRot, grabRight);
        }

        static bool TryReadHmd(XRDeviceSimulator sim, out Vector3 position, out Quaternion rotation)
        {
            position = default;
            rotation = Quaternion.identity;
            const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
            var field = sim.GetType().GetField("m_HMDState", flags);
            if (field == null)
                return false;

            object state = field.GetValue(sim);
            var stateType = state.GetType();
            var posField = stateType.GetField("centerEyePosition");
            var rotField = stateType.GetField("centerEyeRotation");
            if (posField == null || rotField == null)
                return false;

            position = (Vector3)posField.GetValue(state);
            rotation = (Quaternion)rotField.GetValue(state);
            return true;
        }

        static bool IsXrDiving()
        {
            var origin = GameObject.Find("XR Origin (XR Rig)");
            if (origin == null || !origin.activeInHierarchy || MissionController.Instance == null)
                return false;

            if (MissionController.Instance.PlayMode != PlayModeType.XR)
                return false;

            var state = MissionController.Instance.State;
            return state is not (MissionState.Boot or MissionState.ModeSelect or MissionState.Briefing or MissionState.Paused);
        }

        static void ShowCursor()
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        static void ShowControllerModels(GameObject origin)
        {
            foreach (var t in origin.GetComponentsInChildren<Transform>(true))
            {
                if (t.name.Contains("Controller Visual") || t.name == "Left Controller" || t.name == "Right Controller")
                    t.gameObject.SetActive(true);
            }
        }

        static void WriteController(XRDeviceSimulator sim, string stateField, Vector3 position, Quaternion rotation, bool grab)
        {
            const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
            var field = sim.GetType().GetField(stateField, flags);
            if (field == null)
                return;

            object state = field.GetValue(sim);
            var stateType = state.GetType();
            stateType.GetField("devicePosition")?.SetValue(state, position);
            stateType.GetField("deviceRotation")?.SetValue(state, rotation);
            stateType.GetField("isTracked")?.SetValue(state, true);
            stateType.GetField("trackingState")?.SetValue(state, (int)(InputTrackingState.Position | InputTrackingState.Rotation));

            var buttonsField = stateType.GetField("buttons");
            var buttons = buttonsField != null ? (ushort)buttonsField.GetValue(state) : (ushort)0;
            var gripBit = (ushort)(1 << (int)ControllerButton.GripButton);
            if (grab)
                buttons |= gripBit;
            else
                buttons &= (ushort)~gripBit;

            buttonsField?.SetValue(state, buttons);
            stateType.GetField("grip")?.SetValue(state, grab ? 1f : 0f);
            field.SetValue(sim, state);
        }

        static void ApplyControllers(XRDeviceSimulator sim)
        {
            const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
            var type = sim.GetType();
            var manager = type.GetField("m_DeviceLifecycleManager", flags)?.GetValue(sim);
            if (manager == null)
                return;

            var left = type.GetField("m_LeftControllerState", flags)?.GetValue(sim);
            var right = type.GetField("m_RightControllerState", flags)?.GetValue(sim);
            manager.GetType().GetMethod("ApplyControllerState", flags)?.Invoke(manager, new[] { left, right });
        }

        static void SetField(object target, string name, object value)
        {
            target.GetType()
                .GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)
                ?.SetValue(target, value);
        }
    }
}
