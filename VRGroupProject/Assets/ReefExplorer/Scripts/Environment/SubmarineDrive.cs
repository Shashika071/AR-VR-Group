using ReefExplorer.Core;
using ReefExplorer.Interaction;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ReefExplorer.Environment
{
    /// <summary>
    /// Drive the submarine. The camera stays outside and follows the craft.
    /// </summary>
    public sealed class SubmarineDrive : MonoBehaviour
    {
        public static bool IsDriving { get; private set; }

        Transform player;
        Transform cameraTransform;
        Transform cameraParent;
        Vector3 cameraLocalPos;
        Quaternion cameraLocalRot;
        CharacterController body;
        Vector3 driveNose = Vector3.forward;
        bool boarded;
        bool prompted;
        float boardLockedUntil;
        float ignoreExitUntil;
        Renderer[] hiddenPlayer;
        Transform window;
        AudioSource motor;
        static AudioClip motorClip;
        static Renderer[] rockCache;
        static float rockCacheUntil;

        void OnEnable() => MissionEvents.MissionRestarted += OnRestart;
        void OnDisable() => MissionEvents.MissionRestarted -= OnRestart;

        public void PrepareCockpit()
        {
            driveNose = NoseDirection(HullBounds());
            if (window != null)
                window.gameObject.SetActive(false);
        }

        public static bool TryBoard(Transform player, CharacterController body, Transform cameraTransform)
        {
            var craft = GameObject.Find("StationDiveCraft");
            if (craft == null || player == null)
                return false;

            var drive = craft.GetComponent<SubmarineDrive>();
            if (drive == null || Time.time < drive.boardLockedUntil)
                return false;
            if (!IsNearCraft(player.position) && !IsNearCraft(cameraTransform != null ? cameraTransform.position : player.position))
                return false;

            if (!VehiclePowerPack.IsInstalled)
            {
                MissionEvents.RaiseFeedback("Set the craft battery on the vehicle first.");
                return true;
            }

            drive.Board(player, body, cameraTransform != null ? cameraTransform : Camera.main != null ? Camera.main.transform : null);
            return true;
        }

        void Board(Transform diver, CharacterController controller, Transform cam)
        {
            boarded = true;
            IsDriving = true;
            ignoreExitUntil = Time.time + 0.45f;
            player = diver;
            body = controller;
            cameraTransform = cam;
            driveNose = NoseDirection(HullBounds());
            if (body != null)
                body.enabled = false;

            hiddenPlayer = player.GetComponentsInChildren<Renderer>();
            foreach (var renderer in hiddenPlayer)
            {
                if (renderer != null && renderer.transform.IsChildOf(transform))
                    continue;
                if (renderer != null)
                    renderer.enabled = false;
            }

            if (cameraTransform != null)
            {
                cameraParent = cameraTransform.parent;
                cameraLocalPos = cameraTransform.localPosition;
                cameraLocalRot = cameraTransform.localRotation;
                cameraTransform.SetParent(null, true);
            }

            Follow();
            StartMotor();
            MissionEvents.RaiseFeedback("Driving the submarine. WASD move, Space up, Ctrl down, T exit.");
        }

        void Update()
        {
            if (!boarded)
            {
                PromptNearby();
                return;
            }

            var keyboard = Keyboard.current;
            if (keyboard == null)
                return;

            if (Time.time >= ignoreExitUntil && keyboard.tKey.wasPressedThisFrame)
            {
                Exit();
                return;
            }

            var turn = 0f;
            if (keyboard.aKey.isPressed) turn -= 1f;
            if (keyboard.dKey.isPressed) turn += 1f;
            var thrust = 0f;
            if (keyboard.wKey.isPressed) thrust += 1f;
            if (keyboard.sKey.isPressed) thrust -= 1f;
            var lift = 0f;
            if (keyboard.spaceKey.isPressed) lift += 1f;
            if (keyboard.leftCtrlKey.isPressed) lift -= 1f;

            var yaw = turn * 55f * Time.deltaTime;
            transform.Rotate(0f, yaw, 0f, Space.World);
            driveNose = Quaternion.Euler(0f, yaw, 0f) * driveNose;
            driveNose.y = 0f;
            if (driveNose.sqrMagnitude < 0.001f)
                driveNose = Vector3.forward;
            driveNose.Normalize();

            var pos = transform.position + driveNose * (thrust * 7f * Time.deltaTime);
            pos.y = Mathf.Max(0.35f, pos.y + lift * 3.4f * Time.deltaTime);
            pos = ClearOfRocks(transform.position, pos);
            transform.position = pos;
            if (motor != null)
            {
                var moving = Mathf.Abs(thrust) + Mathf.Abs(lift) > 0.01f;
                motor.volume = moving ? 1f : 0.72f;
                motor.pitch = moving ? 1.12f : 0.9f;
            }
        }

        void LateUpdate()
        {
            if (boarded)
                Follow();
        }

        void Follow()
        {
            var bounds = HullBounds();
            var back = -driveNose;
            back.y = 0f;
            if (back.sqrMagnitude < 0.001f)
                back = -Vector3.forward;
            back.Normalize();
            var distance = Mathf.Max(bounds.extents.x, bounds.extents.z) + 1.5f;
            var camPos = bounds.center + back * distance + Vector3.up * 1.25f;
            var look = Quaternion.LookRotation(bounds.center + Vector3.up * 0.3f - camPos, Vector3.up);
            if (cameraTransform != null)
                cameraTransform.SetPositionAndRotation(camPos, look);
            if (player != null)
                player.position = bounds.center;
        }

        void Exit()
        {
            boarded = false;
            IsDriving = false;
            boardLockedUntil = Time.time + 0.35f;
            if (cameraTransform != null)
            {
                cameraTransform.SetParent(cameraParent, false);
                cameraTransform.localPosition = cameraLocalPos;
                cameraTransform.localRotation = cameraLocalRot;
            }

            if (hiddenPlayer != null)
            {
                foreach (var renderer in hiddenPlayer)
                {
                    if (renderer != null)
                        renderer.enabled = true;
                }
            }

            var bounds = HullBounds();
            if (player != null)
            {
                var outPos = bounds.center + Vector3.right * (bounds.extents.x + 1.4f);
                outPos.y = 0.2f;
                player.position = outPos;
            }

            if (body != null)
                body.enabled = true;
            if (motor != null)
                motor.Stop();
            if (window != null)
                window.gameObject.SetActive(false);

            MissionEvents.RaiseFeedback("You left the submarine.");
        }

        void OnRestart()
        {
            if (boarded)
                Exit();
        }

        void StartMotor()
        {
            if (motor == null)
            {
                motor = gameObject.AddComponent<AudioSource>();
                motor.playOnAwake = false;
                motor.loop = true;
                motor.spatialBlend = 0f;
                motor.dopplerLevel = 0f;
                motor.clip = MotorClip();
            }

            motor.volume = 0.72f;
            motor.pitch = 0.9f;
            if (!motor.isPlaying)
                motor.Play();
        }

        static AudioClip MotorClip()
        {
            if (motorClip != null)
                return motorClip;

            const int rate = 22050;
            const float seconds = 1f;
            var count = (int)(rate * seconds);
            var clip = AudioClip.Create("SubmarineMotor", count, 1, rate, false);
            var data = new float[count];
            for (var i = 0; i < count; i++)
            {
                var t = i / (float)rate;
                var hum = Mathf.Sin(2f * Mathf.PI * 58f * t);
                var low = Mathf.Sin(2f * Mathf.PI * 29f * t) * 0.7f;
                var buzz = Mathf.Sin(2f * Mathf.PI * 116f * t) * 0.22f;
                data[i] = Mathf.Clamp((hum + low + buzz) * 0.72f, -1f, 1f);
            }

            clip.SetData(data, 0);
            motorClip = clip;
            return clip;
        }

        Vector3 ClearOfRocks(Vector3 from, Vector3 to)
        {
            if (OverlapsRock(from))
                from = PushOutOfRocks(from);

            if (!OverlapsRock(to))
                return to;

            var xOnly = from;
            xOnly.x = to.x;
            xOnly.y = to.y;
            if (!OverlapsRock(xOnly))
                return xOnly;

            var zOnly = from;
            zOnly.z = to.z;
            zOnly.y = to.y;
            if (!OverlapsRock(zOnly))
                return zOnly;

            var yOnly = from;
            yOnly.y = to.y;
            return OverlapsRock(yOnly) ? from : yOnly;
        }

        Vector3 PushOutOfRocks(Vector3 craftPos)
        {
            var pos = craftPos;
            for (var step = 0; step < 6 && OverlapsRock(pos); step++)
            {
                var hull = ShiftedHull(pos);
                var away = Vector3.zero;
                foreach (var renderer in rockCache)
                {
                    if (!RockHits(renderer, hull))
                        continue;
                    var push = hull.center - renderer.bounds.center;
                    push.y = 0f;
                    if (push.sqrMagnitude < 0.01f)
                        push = Vector3.forward;
                    away += push.normalized;
                }

                if (away.sqrMagnitude < 0.01f)
                    break;
                pos += away.normalized * 0.45f;
                pos.y = Mathf.Max(0.35f, pos.y);
            }

            return pos;
        }

        bool OverlapsRock(Vector3 craftPos)
        {
            CacheRocks();
            if (rockCache == null || rockCache.Length == 0)
                return false;

            var hull = ShiftedHull(craftPos);
            foreach (var renderer in rockCache)
            {
                if (RockHits(renderer, hull))
                    return true;
            }

            return false;
        }

        Bounds ShiftedHull(Vector3 craftPos)
        {
            var hull = HullBounds();
            var center = hull.center + (craftPos - transform.position);
            var ext = hull.extents;
            ext.x = Mathf.Max(0.55f, ext.x * 0.92f);
            ext.z = Mathf.Max(0.55f, ext.z * 0.92f);
            ext.y = Mathf.Max(0.4f, ext.y * 0.85f);
            return new Bounds(center, ext * 2f);
        }

        static bool RockHits(Renderer renderer, Bounds hull)
        {
            return renderer != null && renderer.enabled && renderer.gameObject.activeInHierarchy &&
                   hull.Intersects(renderer.bounds);
        }

        void CacheRocks()
        {
            if (rockCache != null && Time.time < rockCacheUntil)
                return;

            rockCacheUntil = Time.time + 3f;
            var list = new System.Collections.Generic.List<Renderer>();
            foreach (var renderer in FindObjectsByType<Renderer>(FindObjectsSortMode.None))
            {
                if (renderer == null || renderer.transform.IsChildOf(transform))
                    continue;
                if (!NamedRock(renderer.transform))
                    continue;
                list.Add(renderer);
            }

            rockCache = list.ToArray();
        }

        static bool NamedRock(Transform t)
        {
            while (t != null)
            {
                if (t.name.IndexOf("Rock", System.StringComparison.OrdinalIgnoreCase) >= 0)
                    return true;
                t = t.parent;
            }

            return false;
        }

        void PromptNearby()
        {
            var diver = ActivePlayer();
            if (diver == null || IsDriving)
                return;
            var near = IsNearCraft(diver.position);
            if (!near)
            {
                prompted = false;
                return;
            }

            if (prompted || !VehiclePowerPack.IsInstalled)
                return;
            prompted = true;
            MissionEvents.RaiseFeedback("Press T to enter the submarine.");
        }

        public static bool IsNearCraft(Vector3 point)
        {
            var craft = GameObject.Find("StationDiveCraft");
            var drive = craft != null ? craft.GetComponent<SubmarineDrive>() : null;
            if (drive == null)
                return false;
            var closest = drive.HullBounds().ClosestPoint(point);
            return Vector3.Distance(point, closest) <= 5f;
        }

        static Transform ActivePlayer()
        {
            var desktop = GameObject.Find("DesktopPlayer");
            if (desktop != null && desktop.activeInHierarchy)
                return desktop.transform;
            var xr = GameObject.Find("XR Origin (XR Rig)");
            if (xr != null && xr.activeInHierarchy)
                return xr.transform;
            return null;
        }

        void ShowBothSides()
        {
            foreach (var renderer in GetComponentsInChildren<Renderer>(true))
            {
                if (renderer == null || renderer.gameObject.name == "CockpitFrame")
                    continue;
                var mats = renderer.materials;
                for (var i = 0; i < mats.Length; i++)
                {
                    var mat = mats[i];
                    if (mat == null)
                        continue;
                    if (mat.HasProperty("_Cull"))
                        mat.SetInt("_Cull", (int)UnityEngine.Rendering.CullMode.Off);
                }

                renderer.materials = mats;
            }
        }

        Bounds HullBounds()
        {
            var renderers = GetComponentsInChildren<Renderer>();
            Bounds bounds = default;
            var found = false;
            foreach (var renderer in renderers)
            {
                if (renderer == null || renderer.gameObject.name == "CockpitFrame")
                    continue;
                if (!found)
                {
                    bounds = renderer.bounds;
                    found = true;
                }
                else
                {
                    bounds.Encapsulate(renderer.bounds);
                }
            }

            return found ? bounds : new Bounds(transform.position, Vector3.one * 2f);
        }

        static Vector3 NoseDirection(Bounds bounds)
        {
            var along = bounds.size.x >= bounds.size.z ? Vector3.right : Vector3.forward;
            if (Vector3.Dot(-along, Vector3.forward) > Vector3.Dot(along, Vector3.forward))
                along = -along;
            along.y = 0f;
            return along.sqrMagnitude > 0.001f ? along.normalized : Vector3.forward;
        }

        static void BuildWindow(Transform parent)
        {
            Frame(parent, new Vector3(0f, 0.42f, 0.7f), new Vector3(1.4f, 0.07f, 0.05f));
            Frame(parent, new Vector3(0f, -0.28f, 0.7f), new Vector3(1.4f, 0.1f, 0.06f));
            Frame(parent, new Vector3(-0.68f, 0.08f, 0.7f), new Vector3(0.07f, 0.72f, 0.05f));
            Frame(parent, new Vector3(0.68f, 0.08f, 0.7f), new Vector3(0.07f, 0.72f, 0.05f));
        }

        static void Frame(Transform parent, Vector3 localPos, Vector3 scale)
        {
            var bar = GameObject.CreatePrimitive(PrimitiveType.Cube);
            bar.name = "CockpitFrame";
            bar.transform.SetParent(parent, false);
            bar.transform.localPosition = localPos;
            bar.transform.localScale = scale;
            var col = bar.GetComponent<Collider>();
            if (col != null)
                Destroy(col);
            var renderer = bar.GetComponent<Renderer>();
            if (renderer == null)
                return;
            var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            if (shader == null)
                return;
            var mat = new Material(shader);
            var color = new Color(0.04f, 0.06f, 0.08f);
            if (mat.HasProperty("_BaseColor"))
                mat.SetColor("_BaseColor", color);
            if (mat.HasProperty("_Cull"))
                mat.SetInt("_Cull", (int)UnityEngine.Rendering.CullMode.Off);
            mat.color = color;
            renderer.sharedMaterial = mat;
        }
    }
}
