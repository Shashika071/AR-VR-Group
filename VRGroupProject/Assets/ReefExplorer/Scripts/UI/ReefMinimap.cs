using ReefExplorer.Core;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace ReefExplorer.UI
{
    /// <summary>
    /// Round green radar in the corner. M pauses and opens a large copy with names.
    /// </summary>
    public sealed class ReefMinimap : MonoBehaviour
    {
        const float WorldMinX = -13f;
        const float WorldMaxX = 13f;
        const float WorldMinZ = -2f;
        const float WorldMaxZ = 22f;

        public static bool ShowingBigMap { get; private set; }

        RectTransform cornerMap;
        RectTransform cornerSweep;
        RectTransform cornerPlayer;
        RectTransform bigMap;
        RectTransform bigSweep;
        RectTransform bigPlayer;
        GameObject bigRoot;
        Sprite circleSprite;
        Sprite radarSprite;
        Sprite sweepSprite;
        Font labelFont;
        bool pausedForMap;
        float sweepAngle;

        readonly System.Collections.Generic.List<RectTransform> cornerDots = new();
        readonly System.Collections.Generic.List<RectTransform> bigDots = new();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            if (GameObject.Find("ResearchStation") == null)
                return;
            if (FindAnyObjectByType<ReefMinimap>() != null)
                return;

            var host = new GameObject("ReefMinimap");
            host.AddComponent<ReefMinimap>();
        }

        void Start()
        {
            circleSprite = CircleSprite(64);
            radarSprite = RadarSprite(256);
            sweepSprite = SweepSprite(256);
            labelFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            BuildCorner();
            BuildBig();
        }

        void Update()
        {
            if (Keyboard.current != null && Keyboard.current.mKey.wasPressedThisFrame)
                ToggleBigMap();

            if (ShowingBigMap && MissionController.Instance != null &&
                MissionController.Instance.State != MissionState.Paused)
            {
                ShowingBigMap = false;
                pausedForMap = false;
                if (bigRoot != null)
                    bigRoot.SetActive(false);
            }
        }

        void LateUpdate()
        {
            sweepAngle = (sweepAngle + 42f * Time.unscaledDeltaTime) % 360f;
            var player = FindPlayer();
            if (player == null)
                return;

            var cam = Camera.main;
            var yaw = cam != null ? cam.transform.eulerAngles.y : player.eulerAngles.y;
            Place(cornerMap, cornerSweep, cornerPlayer, player.position, yaw, cornerDots);
            if (ShowingBigMap)
                Place(bigMap, bigSweep, bigPlayer, player.position, yaw, bigDots);
        }

        void ToggleBigMap()
        {
            if (bigRoot == null)
                return;

            if (ShowingBigMap)
            {
                ShowingBigMap = false;
                bigRoot.SetActive(false);
                if (pausedForMap)
                {
                    pausedForMap = false;
                    MissionController.Instance?.Resume();
                }

                FindAnyObjectByType<PauseMenuController>()?.RefreshPauseVisual();
                return;
            }

            ShowingBigMap = true;
            bigRoot.SetActive(true);
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;

            var mission = MissionController.Instance;
            if (mission != null && mission.State != MissionState.Paused)
            {
                mission.Pause();
                pausedForMap = mission.State == MissionState.Paused;
            }

            FindAnyObjectByType<PauseMenuController>()?.RefreshPauseVisual();
        }

        void BuildCorner()
        {
            var canvas = NewCanvas("ReefRadarCanvas", 40);
            var frame = NewRadar(canvas.transform, "CornerRadar", new Vector2(1f, 1f), new Vector2(1f, 1f),
                new Vector2(-131f, -131f), 230f);
            cornerMap = frame.map;
            cornerSweep = frame.sweep;
            cornerPlayer = frame.player;
            AddSites(frame.map, frame.dots, cornerDots, false);
        }

        void BuildBig()
        {
            var canvas = NewCanvas("ReefBigMapCanvas", 60);
            bigRoot = canvas.gameObject;
            var dim = NewImage(canvas.transform, "Dim", BoxSprite(), new Color(0f, 0.02f, 0f, 0.72f));
            Stretch(dim.rectTransform);

            var frame = NewRadar(canvas.transform, "BigRadar", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(-120f, 0f), 560f);
            bigMap = frame.map;
            bigSweep = frame.sweep;
            bigPlayer = frame.player;
            AddSites(frame.map, frame.dots, bigDots, true);
            BuildLegend(canvas.transform);
            bigRoot.SetActive(false);
        }

        void BuildLegend(Transform canvas)
        {
            var panel = NewImage(canvas, "Legend", BoxSprite(), new Color(0f, 0.08f, 0.02f, 0.9f));
            var rect = panel.rectTransform;
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0f, 0.5f);
            rect.anchoredPosition = new Vector2(180f, 0f);
            rect.sizeDelta = new Vector2(300f, 540f);

            AddText(rect, "REEF MAP", 26, new Vector2(0f, 230f), new Vector2(270f, 36f), Color.white);
            var rows = Palette();
            for (var i = 0; i < rows.Length; i++)
            {
                var y = 180f - i * 34f;
                var swatch = NewImage(rect, "Swatch" + i, circleSprite, rows[i].color);
                swatch.rectTransform.anchoredPosition = new Vector2(-90f, y);
                swatch.rectTransform.sizeDelta = new Vector2(22f, 22f);
                AddText(rect, rows[i].name, 20, new Vector2(28f, y), new Vector2(180f, 28f), rows[i].color);
            }

            AddText(rect, "M  close", 16, new Vector2(0f, -240f), new Vector2(220f, 24f), new Color(0.5f, 0.9f, 0.5f));
        }

        void AddSites(RectTransform map, RectTransform dots, System.Collections.Generic.List<RectTransform> list, bool named)
        {
            Track(dots, list, "ResearchStation", "Station", SpotColor.Station, 12f, named);
            Track(dots, list, "MonitoringBuoy", "Buoy", SpotColor.Buoy, 12f, named);
            Track(dots, list, "Zone_Coral", "Coral", SpotColor.Coral, 12f, named);
            Track(dots, list, "Zone_Turtle", "Toxin", SpotColor.Toxin, 16f, named);
            Track(dots, list, "Zone_Ray", "Starfish", SpotColor.Starfish, 12f, named);
            var samples = FindObjectsByType<ReefExplorer.Interaction.SampleZone>(FindObjectsSortMode.None);
            for (var i = 0; i < samples.Length; i++)
                Track(dots, list, samples[i].gameObject, "Sample", SpotColor.Sample, 10f, named);
            for (var i = 0; i < 3; i++)
                Track(dots, list, "DeadFish_" + i, "Dead", SpotColor.Dead, 10f, named);
            Track(dots, list, "PowerCell", "Buoy battery", SpotColor.BuoyBattery, 11f, named);
            Track(dots, list, "VehiclePowerPack_1", "Craft battery", SpotColor.CraftBattery, 11f, named);
        }

        void Track(RectTransform parent, System.Collections.Generic.List<RectTransform> list, string objectName, string caption, Color color, float size, bool named)
        {
            Track(parent, list, null, objectName, caption, color, size, named);
        }

        void Track(RectTransform parent, System.Collections.Generic.List<RectTransform> list, GameObject target, string caption, Color color, float size, bool named)
        {
            Track(parent, list, target, target != null ? target.name : caption, caption, color, size, named);
        }

        void Track(RectTransform parent, System.Collections.Generic.List<RectTransform> list, GameObject target, string lookupName, string caption, Color color, float size, bool named)
        {
            var go = new GameObject("Dot_" + lookupName);
            go.transform.SetParent(parent, false);
            var rect = go.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(size, size);
            var image = go.AddComponent<Image>();
            image.sprite = circleSprite;
            image.color = color;
            var blip = go.AddComponent<MapBlip>();
            blip.target = target != null ? target.transform : null;
            blip.lookupName = lookupName;
            if (named)
            {
                var text = AddText(rect, caption, 14, new Vector2(36f, 0f), new Vector2(90f, 18f), color);
                text.alignment = TextAnchor.MiddleLeft;
            }

            list.Add(rect);
            go.SetActive(false);
        }

        void Place(RectTransform map, RectTransform sweep, RectTransform player, Vector3 world, float yaw, System.Collections.Generic.List<RectTransform> dots)
        {
            if (map == null)
                return;

            var half = map.sizeDelta.x * 0.5f - 18f;
            if (sweep != null)
                sweep.localRotation = Quaternion.Euler(0f, 0f, -sweepAngle);
            if (player != null)
            {
                player.anchoredPosition = ToMap(world, half);
                player.localRotation = Quaternion.Euler(0f, 0f, -yaw);
            }

            foreach (var dot in dots)
            {
                if (dot == null)
                    continue;
                var blip = dot.GetComponent<MapBlip>();
                if (blip != null && blip.target == null && !string.IsNullOrEmpty(blip.lookupName))
                {
                    var found = GameObject.Find(blip.lookupName);
                    if (found != null)
                        blip.target = found.transform;
                }

                if (blip != null && blip.target != null && !blip.target.gameObject.activeInHierarchy)
                    blip.target = null;

                if (blip == null || blip.target == null)
                {
                    dot.gameObject.SetActive(false);
                    continue;
                }

                dot.gameObject.SetActive(true);
                dot.anchoredPosition = ToMap(blip.target.position, half);
            }
        }

        struct RadarParts
        {
            public RectTransform map;
            public RectTransform sweep;
            public RectTransform player;
            public RectTransform dots;
        }

        RadarParts NewRadar(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax, Vector2 pos, float size)
        {
            var root = new GameObject(name);
            root.transform.SetParent(parent, false);
            var rootRect = root.AddComponent<RectTransform>();
            rootRect.anchorMin = anchorMin;
            rootRect.anchorMax = anchorMax;
            rootRect.pivot = new Vector2(0.5f, 0.5f);
            rootRect.anchoredPosition = pos;
            rootRect.sizeDelta = new Vector2(size, size);

            var disc = NewImage(rootRect, "Disc", radarSprite, Color.white);
            Stretch(disc.rectTransform);

            var maskGo = new GameObject("Mask");
            maskGo.transform.SetParent(rootRect, false);
            var maskRect = maskGo.AddComponent<RectTransform>();
            Stretch(maskRect);
            var maskImage = maskGo.AddComponent<Image>();
            maskImage.sprite = circleSprite;
            maskImage.color = Color.white;
            maskGo.AddComponent<Mask>().showMaskGraphic = false;

            var sweep = NewImage(maskRect, "Sweep", sweepSprite, new Color(0.4f, 1f, 0.45f, 0.55f));
            Stretch(sweep.rectTransform);

            var dots = new GameObject("Dots");
            dots.transform.SetParent(maskRect, false);
            var dotsRect = dots.AddComponent<RectTransform>();
            Stretch(dotsRect);

            var player = NewImage(dotsRect, "Player", circleSprite, SpotColor.You);
            player.rectTransform.sizeDelta = new Vector2(size * 0.045f, size * 0.045f);
            var bracket = NewImage(player.rectTransform, "Bracket", circleSprite, new Color(0.8f, 1f, 0.8f, 0.95f));
            bracket.rectTransform.sizeDelta = new Vector2(size * 0.07f, size * 0.07f);
            bracket.color = new Color(0.85f, 1f, 0.85f, 0.15f);

            AddDegrees(rootRect, size, size < 300f ? 9 : 14);

            return new RadarParts
            {
                map = rootRect,
                sweep = sweep.rectTransform,
                player = player.rectTransform,
                dots = dotsRect
            };
        }

        void AddDegrees(RectTransform radar, float size, int fontSize)
        {
            var radius = size * 0.44f;
            for (var deg = 0; deg < 360; deg += 30)
            {
                var rad = deg * Mathf.Deg2Rad;
                var p = new Vector2(Mathf.Sin(rad), Mathf.Cos(rad)) * radius;
                AddText(radar, deg.ToString(), fontSize, p, new Vector2(40f, 18f), new Color(0.55f, 1f, 0.6f, 0.95f));
            }
        }

        static Sprite BoxSprite()
        {
            return Sprite.Create(Texture2D.whiteTexture, new Rect(0f, 0f, 4f, 4f), new Vector2(0.5f, 0.5f));
        }

        static Canvas NewCanvas(string name, int sort)
        {
            var go = new GameObject(name);
            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = sort;
            var scaler = go.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            go.AddComponent<GraphicRaycaster>();
            return canvas;
        }

        Image NewImage(Transform parent, string name, Sprite sprite, Color color)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.AddComponent<RectTransform>();
            var image = go.AddComponent<Image>();
            image.sprite = sprite;
            image.color = color;
            return image;
        }

        Text AddText(RectTransform parent, string value, int size, Vector2 pos, Vector2 box, Color color)
        {
            var go = new GameObject("Label");
            go.transform.SetParent(parent, false);
            var rect = go.AddComponent<RectTransform>();
            rect.anchoredPosition = pos;
            rect.sizeDelta = box;
            var text = go.AddComponent<Text>();
            text.font = labelFont;
            text.text = value;
            text.fontSize = size;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = color;
            text.raycastTarget = false;
            return text;
        }

        static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        static Vector2 ToMap(Vector3 world, float half)
        {
            var u = Mathf.InverseLerp(WorldMinX, WorldMaxX, world.x);
            var v = Mathf.InverseLerp(WorldMinZ, WorldMaxZ, world.z);
            return new Vector2(Mathf.Lerp(-half, half, u), Mathf.Lerp(-half, half, v));
        }

        static Transform FindPlayer()
        {
            var xr = GameObject.Find("XR Origin (XR Rig)");
            if (xr != null && xr.activeInHierarchy)
                return xr.transform;
            var desktop = GameObject.Find("DesktopPlayer");
            if (desktop != null && desktop.activeInHierarchy)
                return desktop.transform;
            return Camera.main != null ? Camera.main.transform : null;
        }

        static Sprite CircleSprite(int size)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            var c = (size - 1) * 0.5f;
            for (var y = 0; y < size; y++)
            {
                for (var x = 0; x < size; x++)
                {
                    var d = Vector2.Distance(new Vector2(x, y), new Vector2(c, c)) / c;
                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, d <= 1f ? 1f : 0f));
                }
            }

            tex.Apply();
            return Sprite.Create(tex, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f));
        }

        static Sprite RadarSprite(int size)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            var c = (size - 1) * 0.5f;
            for (var y = 0; y < size; y++)
            {
                for (var x = 0; x < size; x++)
                {
                    var dx = (x - c) / c;
                    var dy = (y - c) / c;
                    var r = Mathf.Sqrt(dx * dx + dy * dy);
                    if (r > 1f)
                    {
                        tex.SetPixel(x, y, Color.clear);
                        continue;
                    }

                    var glow = 0.08f;
                    glow = Mathf.Max(glow, Ring(r, 0.33f, 0.018f));
                    glow = Mathf.Max(glow, Ring(r, 0.66f, 0.018f));
                    glow = Mathf.Max(glow, Ring(r, 0.97f, 0.02f));
                    var ang = Mathf.Atan2(dx, dy) * Mathf.Rad2Deg;
                    if (ang < 0f)
                        ang += 360f;
                    var spoke = Spoke(ang, 30f);
                    var grid = 0f;
                    if (Mathf.Repeat(dx + 1f, 0.25f) < 0.018f || Mathf.Repeat(dy + 1f, 0.25f) < 0.018f)
                        grid = 0.18f;
                    var v = Mathf.Clamp01(glow + spoke + grid);
                    tex.SetPixel(x, y, new Color(0.15f * v, 0.85f * v + 0.05f, 0.25f * v, 0.92f));
                }
            }

            tex.Apply();
            return Sprite.Create(tex, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f));
        }

        static Sprite SweepSprite(int size)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            var c = (size - 1) * 0.5f;
            for (var y = 0; y < size; y++)
            {
                for (var x = 0; x < size; x++)
                {
                    var dx = (x - c) / c;
                    var dy = (y - c) / c;
                    var r = Mathf.Sqrt(dx * dx + dy * dy);
                    var ang = Mathf.Atan2(dx, dy) * Mathf.Rad2Deg;
                    if (ang < 0f)
                        ang += 360f;
                    var inWedge = r <= 1f && ang <= 28f;
                    var a = inWedge ? (1f - ang / 28f) * 0.55f : 0f;
                    tex.SetPixel(x, y, new Color(0.4f, 1f, 0.45f, a));
                }
            }

            tex.Apply();
            return Sprite.Create(tex, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f));
        }

        static float Ring(float r, float radius, float width)
        {
            var d = Mathf.Abs(r - radius);
            return d < width ? 1f : 0f;
        }

        static (string name, Color color)[] Palette()
        {
            return new (string, Color)[]
            {
                ("You", SpotColor.You),
                ("Station", SpotColor.Station),
                ("Buoy", SpotColor.Buoy),
                ("Coral garden", SpotColor.Coral),
                ("Toxin air", SpotColor.Toxin),
                ("Starfish ledge", SpotColor.Starfish),
                ("Water sample", SpotColor.Sample),
                ("Dead fish", SpotColor.Dead),
                ("Buoy battery", SpotColor.BuoyBattery),
                ("Craft battery", SpotColor.CraftBattery),
            };
        }

        static class SpotColor
        {
            public static readonly Color You = Color.white;
            public static readonly Color Station = new(0.25f, 0.55f, 1f);
            public static readonly Color Buoy = new(1f, 0.85f, 0.1f);
            public static readonly Color Coral = new(1f, 0.25f, 0.65f);
            public static readonly Color Toxin = new(0.15f, 1f, 0.2f);
            public static readonly Color Starfish = new(0.72f, 0.28f, 1f);
            public static readonly Color Sample = new(0.15f, 0.85f, 1f);
            public static readonly Color Dead = new(0.95f, 0.15f, 0.15f);
            public static readonly Color BuoyBattery = new(0.55f, 0.9f, 1f);
            public static readonly Color CraftBattery = new(1f, 0.42f, 0.05f);
        }

        sealed class MapBlip : MonoBehaviour
        {
            public Transform target;
            public string lookupName;
        }

        static float Spoke(float angle, float step)
        {
            var m = angle % step;
            var d = Mathf.Min(m, step - m);
            return d < 1.4f ? 0.9f : 0f;
        }
    }
}
