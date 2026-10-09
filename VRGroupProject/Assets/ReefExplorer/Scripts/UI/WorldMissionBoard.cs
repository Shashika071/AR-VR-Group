using ReefExplorer.Core;
using ReefExplorer.Input;
using UnityEngine;
using UnityEngine.UI;

namespace ReefExplorer.UI
{
    public sealed class WorldMissionBoard : MonoBehaviour
    {
        [SerializeField] Text titleText;
        [SerializeField] Text bodyText;
        [SerializeField] Button startButton;
        [SerializeField] Button xrButton;
        [SerializeField] Button desktopButton;
        [SerializeField] Button submitButton;
        [SerializeField] Button creditsButton;
        [SerializeField] Button restartButton;
        [SerializeField] Button quitButton;
        [SerializeField] PlayerModeSelector modeSelector;
        [SerializeField] Text resultsText;
        [SerializeField] GameObject briefingPanel;
        [SerializeField] GameObject resultsPanel;

        void Awake()
        {
            BuildIntroductionPanel();

            if (briefingPanel != null)
                briefingPanel.SetActive(true);
            if (resultsPanel != null)
                resultsPanel.SetActive(false);
            if (submitButton != null)
                submitButton.gameObject.SetActive(false);

            Wire(submitButton, () => MissionController.Instance?.TrySubmit());
        }

        void BuildIntroductionPanel()
        {
            HideLegacyButton(startButton);
            HideLegacyButton(xrButton);
            HideLegacyButton(desktopButton);
            HideLegacyButton(creditsButton);
            HideLegacyButton(restartButton);
            HideLegacyButton(quitButton);

            if (briefingPanel == null)
                return;

            if (titleText != null)
            {
                titleText.text = "REEF RESCUE";
                titleText.fontSize = 52;
                titleText.fontStyle = FontStyle.Bold;
                titleText.color = new Color(0.55f, 0.96f, 0.93f);
                titleText.alignment = TextAnchor.MiddleCenter;
                titleText.rectTransform.anchoredPosition = new Vector2(0f, 276f);
                titleText.rectTransform.sizeDelta = new Vector2(960f, 66f);
            }

            if (bodyText != null)
                bodyText.gameObject.SetActive(false);

            CreatePanelLine(briefingPanel.transform, "TopAccent", new Vector2(0f, 236f), new Vector2(760f, 3f),
                new Color(0.18f, 0.83f, 0.78f, 0.9f));
            CreateText(briefingPanel.transform, "Subtitle", "THE SILENT SIGNAL", 18, FontStyle.Bold,
                new Color(0.96f, 0.73f, 0.34f), TextAnchor.MiddleCenter,
                new Vector2(0f, 218f), new Vector2(960f, 28f));
            CreateText(briefingPanel.transform, "Welcome",
                "A calm underwater field mission to restore a monitoring buoy and protect the reef.",
                22, FontStyle.Normal, new Color(0.85f, 0.91f, 0.93f), TextAnchor.MiddleCenter,
                new Vector2(0f, 170f), new Vector2(900f, 40f));

            CreateCard(briefingPanel.transform, "StepOne", "01", "RESTORE", "Power the silent reef buoy", new Vector2(-260f, 80f));
            CreateCard(briefingPanel.transform, "StepTwo", "02", "SURVEY", "Record wildlife and water evidence", new Vector2(0f, 80f));
            CreateCard(briefingPanel.transform, "StepThree", "03", "PROTECT", "Choose a site for coral recovery", new Vector2(260f, 80f));

            CreateText(briefingPanel.transform, "Controls",
                "DESKTOP  WASD move   •   Right-click look   •   E interact\n" +
                "VR / SIMULATOR  Space + mouse aim   •   G grab   •   Trigger activate",
                16, FontStyle.Normal, new Color(0.64f, 0.75f, 0.78f), TextAnchor.MiddleCenter,
                new Vector2(0f, -40f), new Vector2(900f, 54f));
            CreateText(briefingPanel.transform, "Footer",
                "Your observations become evidence for a real conservation decision.",
                17, FontStyle.Italic, new Color(0.55f, 0.7f, 0.72f), TextAnchor.MiddleCenter,
                new Vector2(0f, -126f), new Vector2(900f, 30f));

            var begin = CreatePrimaryButton(briefingPanel.transform, "BeginDive", "BEGIN DIVE", new Vector2(0f, -218f));
            begin.onClick.AddListener(() =>
            {
                modeSelector?.ChooseDesktop();
                MissionController.Instance?.StartDive();
            });
        }

        static void HideLegacyButton(Button button)
        {
            if (button != null)
                button.gameObject.SetActive(false);
        }

        static void CreatePanelLine(Transform parent, string name, Vector2 position, Vector2 size, Color color)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var image = go.AddComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
            var rect = go.GetComponent<RectTransform>();
            rect.sizeDelta = size;
            rect.anchoredPosition = position;
        }

        static Text CreateText(Transform parent, string name, string value, int fontSize, FontStyle style,
            Color color, TextAnchor alignment, Vector2 position, Vector2 size)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var text = go.AddComponent<Text>();
            text.text = value;
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = fontSize;
            text.fontStyle = style;
            text.color = color;
            text.alignment = alignment;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            var rect = text.rectTransform;
            rect.sizeDelta = size;
            rect.anchoredPosition = position;
            return text;
        }

        static void CreateCard(Transform parent, string name, string number, string heading, string detail, Vector2 position)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var image = go.AddComponent<Image>();
            image.color = new Color(0.04f, 0.18f, 0.24f, 0.92f);
            image.raycastTarget = false;
            var rect = go.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(220f, 116f);
            rect.anchoredPosition = position;

            CreateText(go.transform, "Number", number, 18, FontStyle.Bold,
                new Color(0.96f, 0.73f, 0.34f), TextAnchor.MiddleCenter,
                new Vector2(0f, 38f), new Vector2(200f, 24f));
            CreateText(go.transform, "Heading", heading, 20, FontStyle.Bold,
                new Color(0.55f, 0.96f, 0.93f), TextAnchor.MiddleCenter,
                new Vector2(0f, 10f), new Vector2(200f, 28f));
            CreateText(go.transform, "Detail", detail, 15, FontStyle.Normal,
                new Color(0.78f, 0.87f, 0.88f), TextAnchor.MiddleCenter,
                new Vector2(0f, -23f), new Vector2(190f, 38f));
        }

        static Button CreatePrimaryButton(Transform parent, string name, string label, Vector2 position)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var image = go.AddComponent<Image>();
            image.color = new Color(0.12f, 0.7f, 0.68f, 1f);
            var button = go.AddComponent<Button>();
            var colors = button.colors;
            colors.normalColor = new Color(0.12f, 0.7f, 0.68f, 1f);
            colors.highlightedColor = new Color(0.24f, 0.9f, 0.84f, 1f);
            colors.pressedColor = new Color(0.08f, 0.48f, 0.5f, 1f);
            colors.selectedColor = colors.highlightedColor;
            button.colors = colors;

            var rect = go.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(300f, 62f);
            rect.anchoredPosition = position;
            CreateText(go.transform, "Label", label, 22, FontStyle.Bold, Color.white,
                TextAnchor.MiddleCenter, Vector2.zero, new Vector2(300f, 62f));
            return button;
        }

        void OnEnable()
        {
            MissionEvents.StateChanged += OnState;
            MissionEvents.SurveySubmitted += OnSubmitted;
        }

        void OnDisable()
        {
            MissionEvents.StateChanged -= OnState;
            MissionEvents.SurveySubmitted -= OnSubmitted;
        }

        void OnState(MissionState _, MissionState next)
        {
            if (submitButton != null)
            {
                var showSubmit = next == MissionState.SubmitLog || next == MissionState.Results;
                submitButton.gameObject.SetActive(showSubmit);
                submitButton.interactable = next == MissionState.SubmitLog;
            }

            if (next >= MissionState.AnalyseSamples)
            {
                if (briefingPanel != null)
                    briefingPanel.SetActive(false);
            }

            if (next == MissionState.Results || next == MissionState.Credits)
            {
                if (resultsPanel != null)
                    resultsPanel.SetActive(true);
            }

            if (resultsText != null && next == MissionState.Credits)
            {
                resultsText.text +=
                    "\n\nCredits: Student team · Unity URP · XR Interaction Toolkit · OpenXR.\n" +
                    "See Docs/ASSET_CREDITS.md and Docs/AI_ASSISTANCE.md.";
            }
        }

        void OnSubmitted()
        {
            if (resultsText == null || MissionController.Instance == null)
                return;

            var comparison = MissionController.Instance.LastComparison;
            if (comparison == null)
                return;

            var mc = MissionController.Instance;
            if (briefingPanel != null)
                briefingPanel.SetActive(false);
            if (resultsPanel != null)
                resultsPanel.SetActive(true);

            var sb = new System.Text.StringBuilder();
            sb.AppendLine("<b>REEF RESCUE — MISSION REPORT</b>");
            sb.AppendLine("Your evidence helps the conservation team plan coral restoration.\n");

            sb.AppendLine($"Buoy Restored: {(mc.BuoyRestored ? "✓ Yes" : "✗ No")}");
            sb.AppendLine($"Species Observed: {comparison.observedRequiredSpeciesCount}/{comparison.requiredSpeciesCount}");
            sb.AppendLine($"Rubbish Collected: {comparison.totalRubbishCollected}");
            sb.AppendLine($"Hazards Flagged: {comparison.totalHazardsFlagged}");
            sb.AppendLine();

            if (comparison.siteComparisons != null && comparison.siteComparisons.Count > 0)
            {
                sb.AppendLine("<b>SITE COMPARISON:</b>");
                foreach (var site in comparison.siteComparisons)
                {
                    var status = site.observationComplete ? "✓ Complete" : "Incomplete";
                    sb.AppendLine($"\n<b>{site.displayName}</b> [{status}]");
                    sb.AppendLine($"  Coral: {site.currentCoralCondition}");
                    sb.AppendLine($"  Water: {site.waterClarity} | {site.temperature:F1}°C ({site.temperatureSuitability})");
                    sb.AppendLine($"  Rubbish: {site.rubbishRemoved}/{site.initialRubbish} removed");
                    if (site.hasHazard)
                        sb.AppendLine($"  Hazard: {site.hazardType} {(site.hazardFlagged ? "(Flagged ✓)" : "(Not flagged)")}");
                }
                sb.AppendLine();
            }

            if (!string.IsNullOrEmpty(comparison.recommendedSiteId))
            {
                sb.AppendLine($"<b>RECOMMENDED SITE:</b> {comparison.recommendedSiteId}");
                sb.AppendLine($"Reason: {comparison.recommendationExplanation}");
                sb.AppendLine();
            }

            sb.AppendLine($"{comparison.disclaimer}");
            if (!string.IsNullOrEmpty(mc.LastSavedPath))
                sb.AppendLine($"\nSaved: {mc.LastSavedPath}");

            resultsText.text = sb.ToString();
        }

        static void Wire(Button button, UnityEngine.Events.UnityAction action)
        {
            if (button == null)
                return;
            button.onClick.RemoveListener(action);
            button.onClick.AddListener(action);
        }
    }
}
