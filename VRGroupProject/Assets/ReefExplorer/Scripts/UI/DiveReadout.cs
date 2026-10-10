using ReefExplorer.Core;
using UnityEngine;
using UnityEngine.UI;

namespace ReefExplorer.UI
{
    /// <summary>
    /// Large result card for coral scans, animal scans, and the water test.
    /// </summary>
    public sealed class DiveReadout : MonoBehaviour
    {
        static DiveReadout instance;

        public static void ResetStatic() => instance = null;
        RectTransform card;
        Text title;
        Text body;
        float hideAt;

        public static void Show(string heading, string text, float seconds)
        {
            Ensure();
            if (instance == null)
                return;
            instance.title.text = heading;
            instance.body.text = text ?? "";
            instance.FitCard();
            instance.hideAt = Time.unscaledTime + seconds;
            instance.gameObject.SetActive(true);
        }

        public static void ShowWaterTest()
        {
            var mc = MissionController.Instance;
            if (mc == null || mc.Sites == null)
                return;

            ReefExplorer.Environment.VisibleObjectiveTrim.Apply();
            var text = "";
            foreach (var site in mc.Sites)
            {
                if (site == null || !mc.SiteHasSampleZone(site.SiteId))
                    continue;
                if (text.Length > 0)
                    text += "\n";
                var water = site.WaterReadings;
                text += site.DisplayName + "\n";
                if (water != null)
                {
                    text += "Clarity  " + water.waterClarity
                            + "    Temp  " + water.temperatureCelsius.ToString("0.0") + " C"
                            + "    pH  " + water.pH.ToString("0.0") + "\n";
                    text += water.temperatureSuitability + "\n";
                }

                text += site.SuitableForRestoration
                    ? "Suitable for a coral trial"
                    : "Not suitable — " + site.UnsuitableReason;
            }

            Show("WATER TEST", text.Trim(), 16f);
        }

        static void Ensure()
        {
            if (instance != null)
                return;
            var host = new GameObject("DiveReadout");
            host.AddComponent<DiveReadout>();
        }

        void Awake()
        {
            instance = this;
            var canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 85;
            var scaler = gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);

            var panel = new GameObject("Card");
            panel.transform.SetParent(transform, false);
            card = panel.AddComponent<RectTransform>();
            card.anchorMin = card.anchorMax = new Vector2(0.5f, 0.5f);
            card.sizeDelta = new Vector2(520f, 150f);
            var back = panel.AddComponent<Image>();
            back.color = new Color(0.02f, 0.1f, 0.14f, 0.94f);
            var tex = Texture2D.whiteTexture;
            back.sprite = Sprite.Create(tex, new Rect(0f, 0f, tex.width, tex.height), new Vector2(0.5f, 0.5f));

            title = MakeText(panel.transform, "Title", 26, Vector2.zero, new Vector2(480f, 34f), new Color(0.6f, 0.95f, 1f));
            body = MakeText(panel.transform, "Body", 20, Vector2.zero, new Vector2(480f, 80f), Color.white);
            body.alignment = TextAnchor.UpperLeft;
            gameObject.SetActive(false);
        }

        void FitCard()
        {
            var copy = body.text ?? "";
            var lines = string.IsNullOrEmpty(copy) ? 1 : copy.Split('\n').Length;
            var longest = title.text != null ? title.text.Length : 8;
            foreach (var line in copy.Split('\n'))
            {
                if (line.Length > longest)
                    longest = line.Length;
            }

            var width = Mathf.Clamp(longest * 12.4f + 56f, 360f, 820f);
            var titleH = 34f;
            var bodyH = lines * 26f;
            var height = 16f + titleH + 6f + bodyH + 16f;
            card.sizeDelta = new Vector2(width, height);

            var titleRect = title.rectTransform;
            titleRect.sizeDelta = new Vector2(width - 40f, titleH);
            titleRect.anchoredPosition = new Vector2(0f, height * 0.5f - 16f - titleH * 0.5f);

            var bodyRect = body.rectTransform;
            bodyRect.sizeDelta = new Vector2(width - 48f, bodyH);
            bodyRect.anchoredPosition = new Vector2(0f, height * 0.5f - 16f - titleH - 6f - bodyH * 0.5f);
        }

        void Update()
        {
            if (Time.unscaledTime >= hideAt)
                gameObject.SetActive(false);
        }

        static Text MakeText(Transform parent, string name, int size, Vector2 pos, Vector2 box, Color color)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rect = go.AddComponent<RectTransform>();
            rect.anchoredPosition = pos;
            rect.sizeDelta = box;
            var text = go.AddComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = size;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = color;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            return text;
        }
    }
}
