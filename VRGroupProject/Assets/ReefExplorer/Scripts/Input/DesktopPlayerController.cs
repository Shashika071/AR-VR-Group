using ReefExplorer.Core;
using ReefExplorer.Interaction;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace ReefExplorer.Input
{
    /// <summary>
    /// Keyboard/mouse fallback that can complete every mission task without a headset.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public sealed class DesktopPlayerController : MonoBehaviour
    {
        [SerializeField] Transform cameraTransform;
        [SerializeField] float moveSpeed = 3.2f;
        [SerializeField] float lookSensitivity = 0.18f;
        [SerializeField] float interactRange = 6f;
        [SerializeField] float grabRadius = 3.5f;
        [SerializeField] Key interactKey = Key.E;
        [SerializeField] Key dropKey = Key.Q;
        [SerializeField] Key pauseKey = Key.Escape;

        CharacterController character;
        float pitch;
        bool lookEnabled;
        Rigidbody heldBody;
        Transform heldTransform;
        ScannerTool heldScanner;
        readonly Vector3 heldLocalOffset = new Vector3(0.28f, -0.18f, 0.45f);
        readonly Vector3 heldScale = new Vector3(0.65f, 0.65f, 0.65f);
        Vector3 heldOriginalScale = Vector3.one;
        string lookHint = "Right Click = toggle look | WASD move | E grab | Q drop";

        public bool IsActiveController => isActiveAndEnabled;

        void Awake()
        {
            character = GetComponent<CharacterController>();
            if (cameraTransform == null)
            {
                var cam = GetComponentInChildren<Camera>();
                if (cam != null)
                    cameraTransform = cam.transform;
            }
        }

        void Update()
        {
            // If the player never pressed Desktop, still allow control when this object is the active one.
            EnsureDesktopModeIfNeeded();

            if (MissionController.Instance != null &&
                MissionController.Instance.PlayMode == PlayModeType.XR)
                return;

            if (Keyboard.current != null && Keyboard.current[pauseKey].wasPressedThisFrame)
            {
                if (MissionController.Instance != null &&
                    MissionController.Instance.State == MissionState.Paused)
                    MissionController.Instance.Resume();
                else
                    MissionController.Instance?.Pause();
            }

            if (MissionController.Instance != null &&
                MissionController.Instance.State == MissionState.Paused)
                return;

            Look();
            Move();
            HandleInteract();
            UpdateHeldObject();
            TrackTutorialMove();
            UpdateLookHint();
        }

        void EnsureDesktopModeIfNeeded()
        {
            if (MissionController.Instance == null)
                return;
            if (MissionController.Instance.PlayMode != PlayModeType.Unselected)
                return;
            if (!isActiveAndEnabled)
                return;

            // Auto-select Desktop as soon as the player tries to move or grab.
            // faceBoard:false — never snap look while WASD (especially D) is held.
            var trying =
                (Keyboard.current != null &&
                 (Keyboard.current.wKey.isPressed || Keyboard.current.aKey.isPressed ||
                  Keyboard.current.sKey.isPressed || Keyboard.current.dKey.isPressed ||
                  Keyboard.current[interactKey].wasPressedThisFrame)) ||
                (Mouse.current != null && Mouse.current.rightButton.isPressed);

            if (trying)
                FindAnyObjectByType<PlayerModeSelector>()?.ChooseDesktop(faceBoard: false);
        }

        void Look()
        {
            if (Mouse.current == null || cameraTransform == null)
                return;

            // Right Click once = look around freely (no need to keep holding).
            // Right Click again = stop looking / free the mouse for UI.
            if (Mouse.current.rightButton.wasPressedThisFrame)
                lookEnabled = !lookEnabled;

            if (lookEnabled)
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;

                var delta = Mouse.current.delta.ReadValue() * lookSensitivity;
                pitch = Mathf.Clamp(pitch - delta.y, -80f, 80f);
                transform.Rotate(0f, delta.x, 0f, Space.World);
                cameraTransform.localRotation = Quaternion.Euler(pitch, 0f, 0f);
            }
            else
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
        }

        void Move()
        {
            if (Keyboard.current == null)
                return;

            var input = Vector2.zero;
            if (Keyboard.current.wKey.isPressed) input.y += 1f;
            if (Keyboard.current.sKey.isPressed) input.y -= 1f;
            if (Keyboard.current.dKey.isPressed) input.x += 1f;
            if (Keyboard.current.aKey.isPressed) input.x -= 1f;
            if (input.sqrMagnitude < 0.01f)
                return;

            var move = (transform.right * input.x + transform.forward * input.y).normalized;
            character.SimpleMove(move * moveSpeed);
        }

        void HandleInteract()
        {
            if (cameraTransform == null)
                return;

            if (Keyboard.current != null && Keyboard.current[dropKey].wasPressedThisFrame)
            {
                if (heldTransform != null)
                {
                    DropHeld();
                    MissionEvents.RaiseFeedback("Dropped.");
                }
                return;
            }

            var activate = Mouse.current != null && Mouse.current.leftButton.isPressed;
            if (heldScanner != null)
                heldScanner.DesktopSetActivated(activate);

            if (activate &&
                heldTransform != null &&
                MissionController.Instance != null &&
                MissionController.Instance.State == MissionState.TutorialActivate)
            {
                MissionController.Instance.NotifyTutorialStep("activate");
            }

            // Hotkeys: 1 practice tool, 2 scanner, 3 bottle, 4 power cell
            if (Keyboard.current != null)
            {
                if (Keyboard.current.digit1Key.wasPressedThisFrame || Keyboard.current.numpad1Key.wasPressedThisFrame)
                {
                    if (TryPickupByName("PracticeBuoy")) return;
                }
                if (Keyboard.current.digit2Key.wasPressedThisFrame || Keyboard.current.numpad2Key.wasPressedThisFrame)
                {
                    if (TryPickupByName("Scanner")) return;
                }
                if (Keyboard.current.digit3Key.wasPressedThisFrame || Keyboard.current.numpad3Key.wasPressedThisFrame)
                {
                    if (TryPickupByName("SampleBottle")) return;
                }
                if (Keyboard.current.digit4Key.wasPressedThisFrame || Keyboard.current.numpad4Key.wasPressedThisFrame)
                {
                    if (TryPickupByName("PowerCell")) return;
                }
            }

            var pressedInteract = Keyboard.current != null &&
                                  (Keyboard.current[interactKey].wasPressedThisFrame ||
                                   Keyboard.current.fKey.wasPressedThisFrame);
            if (!pressedInteract)
                return;

            // Fill bottle if aiming at / standing in sample zone while holding bottle.
            if (heldTransform != null &&
                Physics.Raycast(cameraTransform.position, cameraTransform.forward, out var zoneHit, interactRange,
                    ~0, QueryTriggerInteraction.Collide))
            {
                var zone = zoneHit.collider.GetComponentInParent<SampleZone>();
                if (zone != null)
                {
                    zone.TryFill();
                    return;
                }
            }

            if (heldTransform != null)
            {
                MissionEvents.RaiseFeedback("Already holding something. Press Q to drop first.");
                return;
            }

            if (TryPickupNearest())
                return;

            MissionEvents.RaiseFeedback("Nothing to grab nearby. Walk closer to practice tool / scanner / bottle / power cell, look at it, press E.");
        }

        bool TryPickupByName(string objectName)
        {
            if (heldTransform != null)
            {
                MissionEvents.RaiseFeedback("Drop with Q first.");
                return false;
            }

            var go = GameObject.Find(objectName);
            if (go == null)
            {
                MissionEvents.RaiseFeedback($"Could not find {objectName}.");
                return false;
            }

            var body = go.GetComponent<Rigidbody>();
            var grab = go.GetComponent<XRGrabInteractable>();
            if (body == null || grab == null)
            {
                MissionEvents.RaiseFeedback($"{objectName} is not grabbable.");
                return false;
            }

            // Pull it nearby if it fell far away.
            if (Vector3.Distance(transform.position, go.transform.position) > 8f)
            {
                go.transform.position = transform.position + transform.forward * 1.2f + Vector3.up * 1.1f;
                RigidbodyUtil.Stop(body);
            }

            Pickup(body);
            return true;
        }

        bool TryPickupNearest()
        {
            // 1) Prefer what the camera is pointing at.
            if (Physics.SphereCast(cameraTransform.position, 0.25f, cameraTransform.forward, out var hit,
                    interactRange, ~0, QueryTriggerInteraction.Ignore))
            {
                if (TryPickupFromCollider(hit.collider))
                    return true;
            }

            // 2) Fallback: nearest grabbable around the player (beginner friendly).
            var hits = Physics.OverlapSphere(transform.position + Vector3.up * 1.2f, grabRadius, ~0,
                QueryTriggerInteraction.Ignore);

            Rigidbody best = null;
            var bestDist = float.MaxValue;
            foreach (var col in hits)
            {
                var grab = col.GetComponentInParent<XRGrabInteractable>();
                if (grab == null)
                    continue;
                var body = grab.GetComponent<Rigidbody>();
                if (body == null)
                    continue;

                var dist = Vector3.Distance(cameraTransform.position, grab.transform.position);
                if (dist < bestDist)
                {
                    bestDist = dist;
                    best = body;
                }
            }

            if (best != null)
            {
                Pickup(best);
                return true;
            }

            return false;
        }

        bool TryPickupFromCollider(Collider col)
        {
            var grab = col.GetComponentInParent<XRGrabInteractable>();
            if (grab == null)
                return false;
            var body = grab.GetComponent<Rigidbody>();
            if (body == null)
                return false;
            Pickup(body);
            return true;
        }

        void Pickup(Rigidbody body)
        {
            heldBody = body;
            heldTransform = body.transform;
            heldOriginalScale = heldTransform.localScale;
            // Smaller in first-person view so tools don't fill the whole screen.
            if (heldTransform.GetComponent<ScannerTool>() != null ||
                heldTransform.GetComponent<SampleBottle>() != null)
            {
                heldTransform.localScale = Vector3.Scale(heldOriginalScale, heldScale);
            }

            heldScanner = heldTransform.GetComponent<ScannerTool>();
            body.useGravity = true;
            RigidbodyUtil.Stop(body);
            body.isKinematic = true;
            body.detectCollisions = false;

            var bottle = heldTransform.GetComponent<SampleBottle>();
            if (bottle != null)
                MissionController.Instance?.NotifyToolPicked("bottle");
            if (heldScanner != null)
                MissionController.Instance?.NotifyToolPicked("scanner");

            var cell = heldTransform.GetComponent<PowerCell>();
            if (cell != null)
                cell.MarkHeldDesktop(true);

            if (MissionController.Instance != null &&
                MissionController.Instance.State == MissionState.TutorialGrab)
            {
                MissionController.Instance.NotifyTutorialStep("grab");
            }

            MissionEvents.RaiseFeedback($"Picked up {heldTransform.name}. Press Q to drop.");
        }

        void DropHeld()
        {
            if (heldBody == null)
                return;

            heldBody.detectCollisions = true;
            heldScanner?.DesktopSetActivated(false);
            if (heldTransform != null)
            {
                heldTransform.localScale = heldOriginalScale;
                heldTransform.GetComponent<PowerCell>()?.MarkHeldDesktop(false);
            }

            // If near the station table, put the item on the table. Otherwise normal drop.
            var tablePoint = new Vector3(0f, 1.14f, -1.7f);
            var dropPos = cameraTransform != null
                ? cameraTransform.position + cameraTransform.forward * 0.9f
                : transform.position + transform.forward * 0.9f;
            dropPos.y = Mathf.Max(dropPos.y, 1.0f);

            var flatDrop = dropPos;
            flatDrop.y = tablePoint.y;
            var onTable = Vector3.Distance(flatDrop, tablePoint) <= 1.8f;

            if (onTable)
            {
                heldTransform.position = new Vector3(dropPos.x, 1.14f, dropPos.z);
                heldTransform.rotation = Quaternion.identity;
                RigidbodyUtil.ParkKinematic(heldBody);
                MissionEvents.RaiseFeedback($"Placed {heldTransform.name} on the table.");
            }
            else
            {
                heldTransform.position = dropPos;
                heldBody.isKinematic = false;
                heldBody.useGravity = true;
                RigidbodyUtil.Stop(heldBody);
                MissionEvents.RaiseFeedback($"Dropped {heldTransform.name}.");
            }

            var snap = heldTransform.GetComponent<TableDropSnap>();
            snap?.SnapIfOverTable();

            heldBody = null;
            heldTransform = null;
            heldScanner = null;
        }

        void UpdateHeldObject()
        {
            if (heldTransform == null || cameraTransform == null)
                return;

            var target = cameraTransform.TransformPoint(heldLocalOffset);
            heldTransform.SetPositionAndRotation(target, cameraTransform.rotation);
        }

        void UpdateLookHint()
        {
            if (heldTransform != null)
            {
                lookHint = $"Holding: {heldTransform.name} | Q drop | Left Click use scanner";
                return;
            }

            if (cameraTransform != null &&
                Physics.SphereCast(cameraTransform.position, 0.2f, cameraTransform.forward, out var hit,
                    interactRange, ~0, QueryTriggerInteraction.Ignore) &&
                hit.collider.GetComponentInParent<XRGrabInteractable>() != null)
            {
                lookHint = $"Look at: {hit.collider.transform.root.name} | Press E to grab";
            }
            else
            {
                lookHint = lookEnabled
                    ? "LOOK ON — move mouse to look | Right Click again to stop | E grab | 1/2/3 tools"
                    : "Right Click once to look around | WASD move | E grab | 1/2/3 tools";
            }
        }

        Vector3 lastPos;
        void TrackTutorialMove()
        {
            if (lastPos == Vector3.zero)
                lastPos = transform.position;

            if (Vector3.Distance(lastPos, transform.position) > 1.0f)
            {
                MissionController.Instance?.NotifyTutorialStep("move");
                lastPos = transform.position;
            }
        }

        void OnGUI()
        {
            if (MissionController.Instance != null &&
                MissionController.Instance.PlayMode == PlayModeType.XR)
                return;

            var state = MissionController.Instance != null
                ? MissionController.Instance.State
                : MissionState.ModeSelect;
            // Hide clutter while reading the briefing board.
            if (state is MissionState.Boot or MissionState.ModeSelect or MissionState.Briefing)
                return;

            // Simple crosshair
            var cx = Screen.width * 0.5f;
            var cy = Screen.height * 0.5f;
            GUI.color = Color.white;
            GUI.DrawTexture(new Rect(cx - 8f, cy - 1f, 16f, 2f), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(cx - 1f, cy - 8f, 2f, 16f), Texture2D.whiteTexture);

            var style = new GUIStyle(GUI.skin.box)
            {
                fontSize = 16,
                alignment = TextAnchor.MiddleCenter,
                wordWrap = true
            };
            style.normal.textColor = Color.white;
            GUI.Box(new Rect(Screen.width * 0.5f - 280f, Screen.height - 70f, 560f, 50f), lookHint, style);
        }
    }
}
