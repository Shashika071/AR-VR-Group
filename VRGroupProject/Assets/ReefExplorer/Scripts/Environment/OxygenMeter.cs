using ReefExplorer.Core;
using UnityEngine;
using UnityEngine.UI;

namespace ReefExplorer.Environment
{
    /// <summary>
    /// Oxygen bar. One refill tank at the station. Press E beside it to fill.
    /// </summary>
    public sealed class OxygenMeter : MonoBehaviour
    {
        const float MaxAir = 100f;
        const string TankName = "OxygenRefill_Station";
        static OxygenMeter instance;
        public static bool Failed => instance != null && instance.failed;
        Image fill;
        Text label;
        GameObject failPanel;
        float air = MaxAir;
        bool failed;
        bool refillPrompted;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            if (GameObject.Find("ResearchStation") == null)
                return;
            if (FindAnyObjectByType<OxygenMeter>() != null)
                return;
            var host = new GameObject("OxygenMeter");
            host.AddComponent<OxygenMeter>();
            host.AddComponent<VisibleObjectiveTrim>();
        }

        void Awake() => instance = this;

        void OnDestroy()
        {
            if (instance == this)
                instance = null;
        }

        void OnEnable() => MissionEvents.MissionRestarted += OnRestarted;
        void OnDisable() => MissionEvents.MissionRestarted -= OnRestarted;

        void Start()
        {
            BuildBar();
            BuildFailPanel();
            BuildTank(TankName, new Vector3(-2.3f, 0.15f, 1.1f));
            var extra = GameObject.Find("OxygenRefill_Buoy");
            if (extra != null)
                Destroy(extra);
        }

        void OnRestarted()
        {
            air = MaxAir;
            failed = false;
            refillPrompted = false;
            if (failPanel != null)
                failPanel.SetActive(false);
        }

        public static bool TryRefill(Vector3 from)
        {
            if (instance == null || instance.failed)
                return false;
            if (!instance.NearTank(from))
                return false;
            if (instance.air >= MaxAir - 0.5f)
                return false;

            instance.air = MaxAir;
            instance.refillPrompted = false;
            MissionEvents.RaiseFeedback("Oxygen refilled.");
            return true;
        }

        void Update()
        {
            if (failed)
                return;

            var paused = MissionController.Instance != null &&
                         MissionController.Instance.State == MissionState.Paused;
            var player = GameObject.FindGameObjectWithTag("Player");
            var from = player != null ? player.transform.position : Vector3.zero;
            var inTank = player != null && NearTank(from);
            if (!paused)
            {
                air = Mathf.Max(0f, air - 0.45f * Time.deltaTime);
                if (air <= 0f)
                    FailMission();
            }

            if (!inTank)
                refillPrompted = false;
            else if (air < MaxAir - 0.5f && !refillPrompted)
            {
                refillPrompted = true;
                MissionEvents.RaiseFeedback("Press E to refill oxygen.");
            }

            if (fill != null)
            {
                fill.fillAmount = air / MaxAir;
                fill.color = air < 25f
                    ? new Color(1f, 0.35f, 0.25f)
                    : new Color(0.35f, 0.9f, 1f);
            }

            if (label != null)
                label.text = "OXYGEN  " + Mathf.CeilToInt(air);
        }

        bool NearTank(Vector3 from)
        {
            var tank = GameObject.Find(TankName);
            if (tank == null)
                return false;
            return Vector3.Distance(from, tank.transform.position) <= 2f;
        }

        void FailMission()
        {
            failed = true;
            air = 0f;
            Time.timeScale = 0f;
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
            if (failPanel != null)
                failPanel.SetActive(true);
            if (fill != null)
            {
                fill.fillAmount = 0f;
                fill.color = new Color(1f, 0.35f, 0.25f);
            }

            if (label != null)
                label.text = "OXYGEN  0";
        }

        void BuildFailPanel()
        {
            var root = new GameObject("OxygenFail", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            root.transform.SetParent(transform, false);
            var canvas = root.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 200;
            var scaler = root.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            failPanel = root;

            var dim = NewImage(root.transform, "Dim", new Color(0.02f, 0.08f, 0.12f, 0.82f));
            Stretch(dim.rectTransform);

            var card = NewImage(root.transform, "Card", new Color(0.05f, 0.16f, 0.22f, 0.96f));
            card.rectTransform.sizeDelta = new Vector2(640f, 320f);
            card.rectTransform.anchoredPosition = Vector2.zero;

            var title = NewText(card.transform, "Mission failed", 42, FontStyle.Bold, new Color(1f, 0.45f, 0.35f));
            title.rectTransform.anchoredPosition = new Vector2(0f, 70f);
            title.rectTransform.sizeDelta = new Vector2(580f, 70f);

            var body = NewText(card.transform, "Oxygen empty.", 28, FontStyle.Normal, Color.white);
            body.rectTransform.anchoredPosition = new Vector2(0f, 10f);
            body.rectTransform.sizeDelta = new Vector2(580f, 50f);

            var restart = NewButton(card.transform, "Restart", new Vector2(-130f, -90f), new Color(0.2f, 0.55f, 0.45f));
            restart.onClick.AddListener(() =>
            {
                Time.timeScale = 1f;
                if (MissionController.Instance != null)
                    MissionController.Instance.RestartMission();
                else
                    OnRestarted();
            });

            var quit = NewButton(card.transform, "Quit", new Vector2(130f, -90f), new Color(0.55f, 0.22f, 0.2f));
            quit.onClick.AddListener(() =>
            {
                if (MissionController.Instance != null)
                    MissionController.Instance.QuitApplication();
                else
                    Application.Quit();
            });

            root.SetActive(false);
        }

        static Image NewImage(Transform parent, string name, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(parent, false);
            var image = go.GetComponent<Image>();
            image.sprite = Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f));
            image.color = color;
            return image;
        }

        static Text NewText(Transform parent, string message, int size, FontStyle style, Color color)
        {
            var go = new GameObject("Text", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            go.transform.SetParent(parent, false);
            var text = go.GetComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.text = message;
            text.fontSize = size;
            text.fontStyle = style;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = color;
            return text;
        }

        static Button NewButton(Transform parent, string caption, Vector2 pos, Color color)
        {
            var image = NewImage(parent, caption, color);
            image.rectTransform.sizeDelta = new Vector2(220f, 64f);
            image.rectTransform.anchoredPosition = pos;
            var button = image.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            var label = NewText(image.transform, caption, 28, FontStyle.Bold, Color.white);
            Stretch(label.rectTransform);
            return button;
        }

        static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        void BuildBar()
        {
            var canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 60;
            var scaler = gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);

            var root = new GameObject("OxygenBar");
            root.transform.SetParent(transform, false);
            var rect = root.AddComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = new Vector2(0f, -28f);
            rect.sizeDelta = new Vector2(420f, 36f);
            var back = root.AddComponent<Image>();
            back.color = new Color(0.02f, 0.08f, 0.1f, 0.9f);
            back.sprite = White();

            var fillGo = new GameObject("Fill");
            fillGo.transform.SetParent(root.transform, false);
            var fillRect = fillGo.AddComponent<RectTransform>();
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = Vector2.one;
            fillRect.offsetMin = new Vector2(3f, 3f);
            fillRect.offsetMax = new Vector2(-3f, -3f);
            fill = fillGo.AddComponent<Image>();
            fill.sprite = White();
            fill.type = Image.Type.Filled;
            fill.fillMethod = Image.FillMethod.Horizontal;
            fill.color = new Color(0.35f, 0.9f, 1f);
            fill.fillAmount = 1f;

            var textGo = new GameObject("Label");
            textGo.transform.SetParent(root.transform, false);
            var textRect = textGo.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;
            label = textGo.AddComponent<Text>();
            label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            label.fontSize = 16;
            label.alignment = TextAnchor.MiddleCenter;
            label.color = Color.white;
            label.text = "OXYGEN  100";
        }

        static void BuildTank(string name, Vector3 local)
        {
            if (GameObject.Find(name) != null)
                return;
            var station = GameObject.Find("ResearchStation");
            var spot = name.Contains("Station") && station != null
                ? station.transform.TransformPoint(local)
                : local;

            var tank = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            tank.name = name;
            tank.transform.position = spot + Vector3.up * 0.45f;
            tank.transform.localScale = new Vector3(0.28f, 0.45f, 0.28f);
            var renderer = tank.GetComponent<Renderer>();
            if (renderer != null)
                renderer.sharedMaterial = Paint(new Color(0.25f, 0.75f, 1f));

            var lightGo = new GameObject("Glow");
            lightGo.transform.SetParent(tank.transform, false);
            var light = lightGo.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = new Color(0.4f, 0.85f, 1f);
            light.intensity = 1.6f;
            light.range = 3.5f;
            light.shadows = LightShadows.None;

            var ring = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            ring.name = "Ring";
            ring.transform.SetParent(tank.transform, false);
            ring.transform.localPosition = new Vector3(0f, -0.85f, 0f);
            ring.transform.localScale = new Vector3(2.2f, 0.04f, 2.2f);
            var col = ring.GetComponent<Collider>();
            if (col != null)
                Destroy(col);
            var ringRenderer = ring.GetComponent<Renderer>();
            if (ringRenderer != null)
                ringRenderer.sharedMaterial = Paint(new Color(0.45f, 0.95f, 1f));
        }

        static Material Paint(Color color)
        {
            var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            var mat = new Material(shader);
            if (mat.HasProperty("_BaseColor"))
                mat.SetColor("_BaseColor", color);
            if (mat.HasProperty("_EmissionColor"))
            {
                mat.EnableKeyword("_EMISSION");
                mat.SetColor("_EmissionColor", color * 0.4f);
            }
            mat.color = color;
            return mat;
        }

        static Sprite White()
        {
            var tex = Texture2D.whiteTexture;
            return Sprite.Create(tex, new Rect(0f, 0f, tex.width, tex.height), new Vector2(0.5f, 0.5f));
        }
    }
}
