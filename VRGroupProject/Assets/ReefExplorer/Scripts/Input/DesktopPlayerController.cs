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
        [SerializeField] float jumpSpeed = 5.5f;
        [SerializeField] float gravity = -18f;
        [SerializeField] float interactRange = 6f;
        [SerializeField] float grabRadius = 3.5f;
        [SerializeField] Key interactKey = Key.E;
        [SerializeField] Key dropKey = Key.Q;
        [SerializeField] Key pauseKey = Key.Escape;
        [SerializeField] Key jumpKey = Key.Space;

        CharacterController character;
        float pitch;
        float verticalVelocity;
        bool lookEnabled;
        Rigidbody heldBody;
        Transform heldTransform;

        public Transform HeldObject => heldTransform;
        ScannerTool heldScanner;
        readonly Vector3 heldLocalOffset = new Vector3(0.28f, -0.18f, 0.45f);
        readonly Vector3 heldScale = new Vector3(0.65f, 0.65f, 0.65f);
        Vector3 heldOriginalScale = Vector3.one;

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

            // Allow WASD whenever this DesktopPlayer is active (do not freeze if PlayMode says XR).
            if (!isActiveAndEnabled)
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

            if (ReefExplorer.Environment.OxygenMeter.Failed)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
                return;
            }

            if (ReefExplorer.Environment.SubmarineDrive.IsDriving)
                return;

            Look();
            Move();
            HandleInteract();
            UpdateHeldObject();
            TrackTutorialMove();
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
                  Keyboard.current[jumpKey].wasPressedThisFrame ||
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
            if (character == null)
                return;

            var input = Vector2.zero;
            if (Keyboard.current != null)
            {
                if (Keyboard.current.wKey.isPressed) input.y += 1f;
                if (Keyboard.current.sKey.isPressed) input.y -= 1f;
                if (Keyboard.current.dKey.isPressed) input.x += 1f;
                if (Keyboard.current.aKey.isPressed) input.x -= 1f;
            }

            var grounded = character.isGrounded;
            if (grounded && verticalVelocity < 0f)
                verticalVelocity = -2f;

            if (Keyboard.current != null &&
                Keyboard.current[jumpKey].wasPressedThisFrame &&
                grounded)
            {
                verticalVelocity = jumpSpeed;
            }

            verticalVelocity += gravity * Time.deltaTime;

            var planar = Vector3.zero;
            if (input.sqrMagnitude > 0.01f)
                planar = (transform.right * input.x + transform.forward * input.y).normalized * moveSpeed;

            var motion = planar;
            motion.y = verticalVelocity;
            character.Move(motion * Time.deltaTime);
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

            // Hotkeys: 2 scanner, 3 bottle, 4 power cell
            if (Keyboard.current != null)
            {
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

            if (Keyboard.current != null && Keyboard.current.tKey.wasPressedThisFrame && heldTransform == null)
            {
                if (ReefExplorer.Environment.SubmarineDrive.TryBoard(transform, character, cameraTransform))
                    return;
            }

            var pressedInteract = Keyboard.current != null &&
                                  (Keyboard.current[interactKey].wasPressedThisFrame ||
                                   Keyboard.current.fKey.wasPressedThisFrame);
            if (!pressedInteract)
                return;

            if (ReefExplorer.Environment.OxygenMeter.TryRefill(transform.position))
                return;

            var heldBottle = heldTransform != null ? heldTransform.GetComponent<SampleBottle>() : null;
            if (heldBottle != null)
            {
                var zone = FindSampleZone();
                if (zone != null)
                {
                    zone.BeginSample(heldBottle);
                    return;
                }

                if (SampleReturnBox.IsNear(transform.position))
                {
                    if (SampleReturnBox.TryDeposit(heldBottle))
                    {
                        heldTransform.localScale = heldOriginalScale;
                        ReleaseHoldKeepPlaced();
                    }
                    return;
                }

                if (MissionController.Instance != null &&
                    MissionController.Instance.HasAllWaterSamples() &&
                    !SampleReturnBox.IsDeposited)
                {
                    MissionEvents.RaiseFeedback("Take the bottle to the box on the table and press E.");
                    return;
                }
            }

            if (heldTransform != null && heldTransform.GetComponent<RubbishItem>() != null)
            {
                var from = cameraTransform != null ? cameraTransform.position : transform.position;
                if (RubbishBin.TryDrop(heldTransform.GetComponent<RubbishItem>(), from))
                {
                    ReleaseHoldKeepPlaced();
                    return;
                }

                MissionEvents.RaiseFeedback("Carry the rubbish to the yellow bin at the station and press E.");
                return;
            }

            if (heldTransform != null && heldTransform.GetComponent<ToxinDisposalTool>() != null)
            {
                var origin = cameraTransform != null ? cameraTransform.position : transform.position;
                if (ToxinDisposalTool.TryUse(origin))
                    return;

                MissionEvents.RaiseFeedback("Stand in the green cloud and press E.");
                return;
            }

            if (heldTransform != null && heldTransform.GetComponent<PowerCell>() != null)
            {
                PlaceHeldInFront();
                return;
            }

            if (heldTransform != null && heldTransform.GetComponent<VehiclePowerPack>() != null)
            {
                if (TrySetOnVehicle(heldTransform))
                {
                    ReleaseHoldKeepPlaced();
                    MissionEvents.RaiseFeedback("Dive vehicle started.");
                    return;
                }

                MissionEvents.RaiseFeedback("Stand by the dive vehicle and press E to set the battery on it.");
                return;
            }

            var marker = heldTransform != null ? heldTransform.GetComponent<RecommendationMarker>() : null;
            if (marker != null)
            {
                var from = cameraTransform != null ? cameraTransform.position : transform.position;
                if (MarkerHolder.TryPlaceNearest(from, marker))
                {
                    ReleaseHoldKeepPlaced();
                    return;
                }

                MissionEvents.RaiseFeedback("Carry the marker to the holder at your chosen site and press E.");
                return;
            }

            if (heldTransform != null)
            {
                MissionEvents.RaiseFeedback("Already holding something. Press Q to drop first.");
                return;
            }

            if (TryPickupNearest())
                return;

            MissionEvents.RaiseFeedback("Nothing to grab nearby. Walk closer to a tool on the table, look at it, and press E.");
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
            if (Physics.SphereCast(cameraTransform.position, 0.08f, cameraTransform.forward, out var hit,
                    interactRange, ~0, QueryTriggerInteraction.Ignore))
            {
                if (TryPickupFromCollider(hit.collider))
                    return true;
            }

            // 2) Fallback: nearest grabbable around the player (beginner friendly).
            var hits = Physics.OverlapSphere(transform.position + Vector3.up * 1.2f, grabRadius, ~0,
                QueryTriggerInteraction.Ignore);

            Rigidbody best = null;
            var bestDist = 0.45f;
            foreach (var col in hits)
            {
                var grab = col.GetComponentInParent<XRGrabInteractable>();
                if (grab == null)
                    continue;
                var body = grab.GetComponent<Rigidbody>();
                if (body == null)
                    continue;

                var to = grab.transform.position - cameraTransform.position;
                var along = Vector3.Dot(to, cameraTransform.forward);
                if (along < 0.3f || along > interactRange)
                    continue;
                var side = Vector3.Distance(cameraTransform.position + cameraTransform.forward * along, grab.transform.position);
                if (side < bestDist)
                {
                    bestDist = side;
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
            if (heldTransform.GetComponent<VehiclePowerPack>() != null)
            {
                heldTransform.SetParent(null, true);
                VehiclePowerPack.ShowVehicleHint(true);
            }
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
            {
                MissionController.Instance?.NotifyToolPicked("bottle");
                if (SampleReturnBox.IsReady && !SampleReturnBox.IsDeposited)
                    SampleReturnBox.ShowHint(true);
            }
            if (heldTransform.GetComponent<ToxinDisposalTool>() != null)
                ToxinDisposalTool.ShowHint(true);
            if (heldScanner != null)
            {
                heldScanner.DesktopSetHeld(true);
                MissionController.Instance?.NotifyToolPicked("scanner");
            }
            if (heldTransform.GetComponent<RubbishItem>() != null)
                RubbishBin.ShowHint(true);

            var cell = heldTransform.GetComponent<PowerCell>();
            if (cell != null)
                cell.MarkHeldDesktop(true);

            if (MissionController.Instance != null &&
                MissionController.Instance.State == MissionState.TutorialGrab)
            {
                MissionController.Instance.NotifyTutorialStep("grab");
            }

            if (heldTransform.GetComponent<ToxinDisposalTool>() != null)
                MissionEvents.RaiseFeedback("Disposal tool. Press E in the green cloud.");
            else if (heldScanner != null)
                MissionEvents.RaiseFeedback("Hold the left mouse button and aim at coral or a fish.");
            else if (heldTransform.GetComponent<RubbishItem>() != null)
                MissionEvents.RaiseFeedback("Carry this rubbish to the yellow bin at the station.");
            else if (heldTransform.GetComponent<RecommendationMarker>() != null)
                MissionEvents.RaiseFeedback("Recommendation marker. Carry it to the holder at your chosen site and press E.");
            else if (cell == null && heldTransform.GetComponent<VehiclePowerPack>() == null)
                MissionEvents.RaiseFeedback($"Picked up {heldTransform.name}. Press Q to drop.");
            else if (cell == null)
                MissionEvents.RaiseFeedback("Craft battery. Press E at the dive vehicle to set it on the craft.");
        }

        bool TrySetOnVehicle(Transform pack)
        {
            var craft = GameObject.Find("StationDiveCraft");
            if (craft == null || pack == null)
                return false;

            var origin = cameraTransform != null ? cameraTransform.position : transform.position;
            if (!ReefExplorer.Environment.SubmarineDrive.IsNearCraft(origin) &&
                !ReefExplorer.Environment.SubmarineDrive.IsNearCraft(transform.position))
                return false;

            var slot = craft.transform.Find("VehicleBatterySlot_1");
            if (slot == null)
                return false;

            pack.SetParent(slot, false);
            pack.localPosition = Vector3.zero;
            pack.localRotation = Quaternion.identity;
            pack.gameObject.SetActive(false);
            VehiclePowerPack.MarkInstalled();
            VehiclePowerPack.ShowVehicleHint(false);
            VehiclePowerPack.PlayStart(craft.transform.position);
            return true;
        }

        void ReleaseHoldKeepPlaced()
        {
            if (heldBody != null)
            {
                heldBody.detectCollisions = true;
                RigidbodyUtil.ParkKinematic(heldBody);
            }

            heldBody = null;
            heldTransform = null;
            heldScanner = null;
        }

        void PlaceHeldInFront()
        {
            if (heldTransform == null || cameraTransform == null)
                return;

            var dropPos = cameraTransform.position + cameraTransform.forward * 1.1f;
            if (Physics.Raycast(cameraTransform.position, cameraTransform.forward, out var hit, 6f,
                    ~0, QueryTriggerInteraction.Ignore))
                dropPos = hit.point + hit.normal * 0.12f;
            dropPos.y = Mathf.Max(dropPos.y, 0.12f);

            heldTransform.GetComponent<PowerCell>()?.MarkHeldDesktop(false);
            if (heldBody != null)
            {
                heldBody.detectCollisions = true;
                RigidbodyUtil.ParkKinematic(heldBody);
            }

            heldTransform.SetPositionAndRotation(dropPos, Quaternion.identity);
            heldBody = null;
            heldTransform = null;
            heldScanner = null;
        }

        SampleZone FindSampleZone()
        {
            if (cameraTransform != null &&
                Physics.Raycast(cameraTransform.position, cameraTransform.forward, out var hit, interactRange,
                    ~0, QueryTriggerInteraction.Collide))
            {
                var aimed = hit.collider.GetComponentInParent<SampleZone>();
                if (aimed != null)
                    return aimed;
            }

            var zones = FindObjectsByType<SampleZone>(FindObjectsSortMode.None);
            SampleZone best = null;
            var bestDist = 2.6f;
            var origin = transform.position;
            foreach (var zone in zones)
            {
                if (zone == null)
                    continue;
                var dist = Vector3.Distance(origin, zone.transform.position);
                if (dist <= bestDist)
                {
                    bestDist = dist;
                    best = zone;
                }
            }

            return best;
        }

        BuoyPowerSocket FindNearbyBuoySocket(float radius)
        {
            var sockets = FindObjectsByType<BuoyPowerSocket>(FindObjectsSortMode.None);
            BuoyPowerSocket best = null;
            var bestDist = radius;
            var origin = cameraTransform != null ? cameraTransform.position : transform.position;
            foreach (var socket in sockets)
            {
                if (socket == null)
                    continue;
                var dist = Vector3.Distance(origin, socket.transform.position);
                if (dist <= bestDist)
                {
                    bestDist = dist;
                    best = socket;
                }
            }

            return best;
        }

        void ReleaseHold()
        {
            if (heldTransform != null)
                heldTransform.GetComponent<PowerCell>()?.MarkHeldDesktop(false);
            if (heldBody != null)
            {
                heldBody.detectCollisions = true;
                RigidbodyUtil.ParkKinematic(heldBody);
            }

            if (heldScanner != null)
                heldScanner.DesktopSetHeld(false);
            RubbishBin.ShowHint(false);
            heldBody = null;
            heldTransform = null;
            heldScanner = null;
        }

        void DropHeld()
        {
            if (heldBody == null)
                return;

            heldBody.detectCollisions = true;
            heldScanner?.DesktopSetActivated(false);
            heldScanner?.DesktopSetHeld(false);
            RubbishBin.ShowHint(false);
            if (heldTransform != null)
            {
                heldTransform.localScale = heldOriginalScale;
                heldTransform.GetComponent<PowerCell>()?.MarkHeldDesktop(false);
                if (heldTransform.GetComponent<VehiclePowerPack>() != null)
                    VehiclePowerPack.ShowVehicleHint(false);
                ToxinDisposalTool.ShowHint(false);
                SampleReturnBox.ShowHint(false);
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
            if (state is MissionState.Boot or MissionState.ModeSelect or MissionState.Briefing or MissionState.Paused)
                return;

            // Simple crosshair
            var cx = Screen.width * 0.5f;
            var cy = Screen.height * 0.5f;
            GUI.color = Color.white;
            GUI.DrawTexture(new Rect(cx - 8f, cy - 1f, 16f, 2f), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(cx - 1f, cy - 8f, 2f, 16f), Texture2D.whiteTexture);

        }
    }
}
