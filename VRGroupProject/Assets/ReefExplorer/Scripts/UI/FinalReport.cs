using System.Text;
using ReefExplorer.Core;
using ReefExplorer.Survey;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace ReefExplorer.UI
{
    /// <summary>
    /// Full-screen mission report. Appears when every survey task is finished.
    /// </summary>
    public sealed class FinalReport : MonoBehaviour
    {
        GameObject root;
        Text body;
        RectTransform content;
        bool open;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            if (GameObject.Find("ResearchStation") == null)
                return;
            if (FindAnyObjectByType<FinalReport>() != null)
                return;

            var host = new GameObject("FinalReport");
            host.AddComponent<FinalReport>();
        }

        void OnEnable()
        {
            MissionEvents.SurveySubmitted += Show;
            MissionEvents.MissionRestarted += Hide;
        }

        void OnDisable()
        {
            MissionEvents.SurveySubmitted -= Show;
            MissionEvents.MissionRestarted -= Hide;
        }

        void Update()
        {
            if (!open || Keyboard.current == null)
                return;

            if (Keyboard.current.enterKey.wasPressedThisFrame || Keyboard.current.numpadEnterKey.wasPressedThisFrame)
                Hide();
        }

        void Show()
        {
            var mc = MissionController.Instance;
            if (mc == null)
                return;

            EnsureUi();
            body.text = Build(mc);
            Canvas.ForceUpdateCanvases();
            var lines = string.IsNullOrEmpty(body.text) ? 1 : body.text.Split('\n').Length;
            var height = Mathf.Max(body.preferredHeight + 28f, lines * 26f);
            content.sizeDelta = new Vector2(0f, height);
            var bodyRect = body.rectTransform;
            bodyRect.sizeDelta = new Vector2(bodyRect.sizeDelta.x, height);
            root.SetActive(true);
            open = true;
        }

        void Hide()
        {
            open = false;
            if (root != null)
                root.SetActive(false);
        }

        static string Build(MissionController mc)
        {
            var comparison = mc.LastComparison;
            var log = mc.DiveLog;
            var sb = new StringBuilder();

            sb.AppendLine("The survey is finished. This is the full report for the conservation team.");
            sb.AppendLine();
            sb.AppendLine("MONITORING BUOY");
            sb.AppendLine(mc.BuoyRestored ? "Restored. The earlier survey is available." : "Still silent.");
            sb.AppendLine();

            if (mc.Sites != null)
            {
                foreach (var site in mc.Sites)
                {
                    if (site == null)
                        continue;

                    var progress = mc.GetSiteProgress(site.SiteId);
                    var sample = log.perSiteSamples.Find(s =>
                        s != null && string.Equals(s.siteId, site.SiteId, System.StringComparison.OrdinalIgnoreCase));
                    var rubbish = 0;
                    foreach (var piece in log.rubbishCollected)
                    {
                        if (piece != null && string.Equals(piece.siteId, site.SiteId, System.StringComparison.OrdinalIgnoreCase))
                            rubbish++;
                    }

                    sb.AppendLine(site.DisplayName.ToUpperInvariant());
                    var coral = mc.LatestCoralCondition(site.SiteId);
                    sb.AppendLine("Coral now: " + (string.IsNullOrEmpty(coral) ? "Not scanned" : coral));
                    sb.AppendLine("Coral before: " + (mc.BuoyRestored ? site.BaselineCoralCondition : "Baseline locked"));
                    if (progress.animalScanned && site.TargetAnimal != null)
                        sb.AppendLine("Animal: " + site.TargetAnimal.DisplayName + ". " + site.AnimalObservationNote);
                    else
                        sb.AppendLine("Animal: Not scanned");

                    if (sample != null && sample.analysed && site.WaterReadings != null)
                    {
                        var water = site.WaterReadings;
                        sb.AppendLine("Water: " + water.waterClarity
                                      + ", " + water.temperatureCelsius.ToString("0.0") + " C, pH "
                                      + water.pH.ToString("0.0")
                                      + ", current " + water.currentStrength);
                        sb.AppendLine(water.temperatureSuitability);
                    }
                    else if (sample != null && sample.collected)
                    {
                        sb.AppendLine("Water: Collected, not tested");
                    }
                    else
                    {
                        sb.AppendLine("Water: Not collected");
                    }

                    sb.AppendLine("Rubbish removed: " + rubbish + " / " + site.InitialRubbishCount);
                    if (site.HasHazard)
                    {
                        var flagged = log.hazardsFlagged.Exists(h =>
                            h != null && string.Equals(h.siteId, site.SiteId, System.StringComparison.OrdinalIgnoreCase));
                        sb.AppendLine("Hazard: " + site.HazardType + (flagged ? " — cleared" : " — still there"));
                    }
                    else
                    {
                        sb.AppendLine("Hazard: None");
                    }

                    sb.AppendLine(site.SuitableForRestoration
                        ? "Trial site: Suitable. " + site.SuitabilityReason
                        : "Trial site: Not suitable. " + site.UnsuitableReason);
                    sb.AppendLine();
                }
            }

            var chosen = SiteName(mc, log.recommendedSiteId);
            sb.AppendLine("RECOMMENDATION");
            sb.AppendLine(string.IsNullOrEmpty(chosen) ? "No site chosen." : chosen);
            if (!string.IsNullOrEmpty(log.recommendationReason))
                sb.AppendLine(log.recommendationReason);
            sb.AppendLine(log.markerPlaced ? "Marker: Planted at " + SiteName(mc, log.markerSiteId) + "." : "Marker: Not planted.");
            sb.AppendLine();

            if (comparison != null && comparison.rows != null && comparison.rows.Count > 0)
            {
                sb.AppendLine("SPECIES COMPARED WITH THE EARLIER SURVEY");
                foreach (var row in comparison.rows)
                {
                    if (row == null)
                        continue;
                    var change = row.difference > 0 ? "+" + row.difference : row.difference.ToString();
                    sb.AppendLine(row.displayName + " at " + ZoneName(mc, row.zoneId)
                                  + ": was " + row.baselineCount + ", now " + row.currentCount
                                  + " (" + change + ")");
                }

                sb.AppendLine();
            }

            if (comparison != null && !string.IsNullOrEmpty(comparison.plainLanguageSummary))
            {
                sb.AppendLine("SUMMARY");
                sb.AppendLine(comparison.plainLanguageSummary.Replace("\n", " ").Trim());
                sb.AppendLine();
            }

            if (comparison != null && !string.IsNullOrEmpty(comparison.disclaimer))
                sb.AppendLine(comparison.disclaimer);
            if (!string.IsNullOrEmpty(mc.LastSavedPath))
                sb.AppendLine("Saved: " + mc.LastSavedPath);

            return sb.ToString().Trim();
        }

        static string SiteName(MissionController mc, string siteId)
        {
            if (mc.Sites == null || string.IsNullOrEmpty(siteId))
                return siteId ?? "";
            foreach (var site in mc.Sites)
            {
                if (site != null && string.Equals(site.SiteId, siteId, System.StringComparison.OrdinalIgnoreCase))
                    return site.DisplayName;
            }

            return siteId;
        }

        static string ZoneName(MissionController mc, string zoneId)
        {
            if (string.IsNullOrEmpty(zoneId) || zoneId == "all")
                return "all sites";
            if (mc.Sites != null)
            {
                foreach (var site in mc.Sites)
                {
                    if (site != null && string.Equals(site.ZoneId, zoneId, System.StringComparison.OrdinalIgnoreCase))
                        return site.DisplayName;
                }
            }

            return zoneId;
        }

        void EnsureUi()
        {
            if (root != null)
                return;

            var canvasGo = new GameObject("FinalReportCanvas");
            canvasGo.transform.SetParent(transform, false);
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 120;
            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            canvasGo.AddComponent<GraphicRaycaster>();

            root = new GameObject("Panel");
            root.transform.SetParent(canvasGo.transform, false);
            var panel = root.AddComponent<RectTransform>();
            panel.anchorMin = panel.anchorMax = new Vector2(0.5f, 0.5f);
            panel.pivot = new Vector2(0.5f, 0.5f);
            panel.sizeDelta = new Vector2(920f, 760f);
            var back = root.AddComponent<Image>();
            back.color = new Color(0.02f, 0.08f, 0.12f, 0.96f);
            back.sprite = White();

            var title = AddText(root.transform, "Title", "MISSION COMPLETE", 34, FontStyle.Bold,
                new Color(0.55f, 0.96f, 0.75f), new Vector2(0f, 330f), new Vector2(860f, 48f), TextAnchor.MiddleCenter);
            title.horizontalOverflow = HorizontalWrapMode.Overflow;

            AddText(root.transform, "Close", "Press Enter to close", 18, FontStyle.Normal,
                new Color(0.75f, 0.88f, 0.9f), new Vector2(0f, -340f), new Vector2(860f, 28f), TextAnchor.MiddleCenter);

            var scrollGo = new GameObject("Scroll");
            scrollGo.transform.SetParent(root.transform, false);
            var scrollRect = scrollGo.AddComponent<RectTransform>();
            scrollRect.anchorMin = scrollRect.anchorMax = new Vector2(0.5f, 0.5f);
            scrollRect.pivot = new Vector2(0.5f, 0.5f);
            scrollRect.sizeDelta = new Vector2(860f, 600f);
            scrollRect.anchoredPosition = new Vector2(0f, -8f);
            var scroll = scrollGo.AddComponent<ScrollRect>();
            scroll.horizontal = false;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 28f;

            var viewportGo = new GameObject("Viewport");
            viewportGo.transform.SetParent(scrollGo.transform, false);
            var viewport = viewportGo.AddComponent<RectTransform>();
            viewport.anchorMin = Vector2.zero;
            viewport.anchorMax = Vector2.one;
            viewport.offsetMin = viewport.offsetMax = Vector2.zero;
            viewportGo.AddComponent<Image>().color = new Color(1f, 1f, 1f, 0.01f);
            viewportGo.GetComponent<Image>().sprite = White();
            viewportGo.AddComponent<Mask>().showMaskGraphic = false;

            var contentGo = new GameObject("Content");
            contentGo.transform.SetParent(viewportGo.transform, false);
            content = contentGo.AddComponent<RectTransform>();
            content.anchorMin = new Vector2(0f, 1f);
            content.anchorMax = new Vector2(1f, 1f);
            content.pivot = new Vector2(0.5f, 1f);
            content.anchoredPosition = Vector2.zero;
            content.sizeDelta = new Vector2(0f, 600f);

            body = AddText(contentGo.transform, "Body", "", 20, FontStyle.Normal, Color.white,
                Vector2.zero, new Vector2(820f, 600f), TextAnchor.UpperLeft);
            var bodyRect = body.rectTransform;
            bodyRect.anchorMin = new Vector2(0f, 1f);
            bodyRect.anchorMax = new Vector2(1f, 1f);
            bodyRect.pivot = new Vector2(0.5f, 1f);
            bodyRect.anchoredPosition = new Vector2(0f, -8f);
            bodyRect.sizeDelta = new Vector2(-32f, 600f);
            body.horizontalOverflow = HorizontalWrapMode.Wrap;
            body.verticalOverflow = VerticalWrapMode.Overflow;

            scroll.viewport = viewport;
            scroll.content = content;
            root.SetActive(false);
        }

        static Text AddText(Transform parent, string name, string value, int size, FontStyle style,
            Color color, Vector2 position, Vector2 box, TextAnchor alignment)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var text = go.AddComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = size;
            text.fontStyle = style;
            text.color = color;
            text.alignment = alignment;
            text.text = value;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            var rect = text.rectTransform;
            rect.sizeDelta = box;
            rect.anchoredPosition = position;
            return text;
        }

        static Sprite White()
        {
            var tex = Texture2D.whiteTexture;
            return Sprite.Create(tex, new Rect(0f, 0f, tex.width, tex.height), new Vector2(0.5f, 0.5f));
        }
    }
}
