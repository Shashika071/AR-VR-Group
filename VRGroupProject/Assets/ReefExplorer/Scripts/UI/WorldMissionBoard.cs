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
            if (titleText != null)
            {
                titleText.text = "REEF RESCUE";
                titleText.fontSize = 40;
                titleText.fontStyle = FontStyle.Bold;
                titleText.color = new Color(0.55f, 0.96f, 0.92f);
            }

            if (bodyText != null)
            {
                bodyText.gameObject.SetActive(false);
            }

            DisableLegacyIntroButtons();
            BuildIntroductionLayout();

            if (briefingPanel != null)
                briefingPanel.SetActive(true);
            if (resultsPanel != null)
                resultsPanel.SetActive(false);
            if (submitButton != null)
                submitButton.gameObject.SetActive(false);

            Wire(startButton, () => MissionController.Instance?.StartDive());
            Wire(xrButton, () => modeSelector?.ChooseXr());
            Wire(desktopButton, () => modeSelector?.ChooseDesktop());
            Wire(submitButton, () => MissionController.Instance?.TrySubmit());
            Wire(creditsButton, () => MissionController.Instance?.ShowCredits());
            Wire(restartButton, () => MissionController.Instance?.RestartMission());
            Wire(quitButton, () => MissionController.Instance?.QuitApplication());
        }

        void DisableLegacyIntroButtons()
        {
            SetInactive(xrButton);
            SetInactive(desktopButton);
            SetInactive(startButton);
            SetInactive(creditsButton);
            SetInactive(restartButton);
            SetInactive(quitButton);
        }

        static void SetInactive(Button button)
        {
            if (button != null)
                button.gameObject.SetActive(false);
        }

        void BuildIntroductionLayout()
        {
            if (titleText == null || titleText.transform.parent == null)
                return;

            var parent = titleText.transform.parent;
            var panelImage = parent.GetComponent<Image>();
            if (panelImage != null)
                panelImage.color = new Color(0.025f, 0.075f, 0.13f, 0.98f);

            AddRule(parent, new Vector2(0f, 270f), new Color(0.96f, 0.78f, 0.38f, 0.9f));
            AddText(parent, "Subtitle", "THE SILENT SIGNAL", 24, FontStyle.Bold,
                new Color(0.96f, 0.78f, 0.38f), new Vector2(0f, 245f), new Vector2(960f, 36f),
                TextAnchor.MiddleCenter);
            AddText(parent, "Intro", "A guided conservation dive to restore the reef monitoring network.",
                22, FontStyle.Normal, new Color(0.82f, 0.9f, 0.93f), new Vector2(0f, 205f),
                new Vector2(960f, 34f), TextAnchor.MiddleCenter);

            AddMissionCard(parent, "01  RESTORE", "Power the silent buoy\nand recover the baseline survey.", new Vector2(-325f, 55f),
                new Color(0.1f, 0.38f, 0.46f, 0.95f));
            AddMissionCard(parent, "02  SURVEY", "Scan wildlife, water,\ncoral and reef hazards.", new Vector2(0f, 55f),
                new Color(0.12f, 0.3f, 0.42f, 0.95f));
            AddMissionCard(parent, "03  PROTECT", "Compare the evidence\nand recommend a trial site.", new Vector2(325f, 55f),
                new Color(0.16f, 0.34f, 0.3f, 0.95f));

            AddText(parent, "Instructions", "V  VR / SIMULATOR     ENTER  BEGIN DIVE     WASD  DESKTOP MODE",
                22, FontStyle.Bold, new Color(0.55f, 0.96f, 0.92f), new Vector2(0f, -105f),
                new Vector2(960f, 38f), TextAnchor.MiddleCenter);
            AddText(parent, "Controls", "Desktop: Right Mouse look  |  E grab  |  Left Click scan\n" +
                "VR / Simulator: Space + mouse aim  |  G grab  |  Click activate",
                22, FontStyle.Normal, new Color(0.78f, 0.86f, 0.89f), new Vector2(0f, -175f),
                new Vector2(960f, 68f), TextAnchor.MiddleCenter);
            AddRule(parent, new Vector2(0f, -225f), new Color(0.25f, 0.65f, 0.68f, 0.65f));
        }

        static void AddRule(Transform parent, Vector2 position, Color color)
        {
            var go = new GameObject("AccentRule", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(840f, 3f);
            rect.anchoredPosition = position;
            go.GetComponent<Image>().color = color;
        }

        static void AddMissionCard(Transform parent, string heading, string description, Vector2 position, Color color)
        {
            var card = new GameObject(heading.Replace(" ", "_"), typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            card.transform.SetParent(parent, false);
            var cardRect = card.GetComponent<RectTransform>();
            cardRect.sizeDelta = new Vector2(290f, 180f);
            cardRect.anchoredPosition = position;
            card.GetComponent<Image>().color = color;

            AddText(card.transform, "Heading", heading, 24, FontStyle.Bold,
                new Color(1f, 0.82f, 0.42f), new Vector2(0f, 52f), new Vector2(270f, 38f),
                TextAnchor.MiddleCenter);
            AddText(card.transform, "Description", description, 22, FontStyle.Normal,
                Color.white, new Vector2(0f, -18f), new Vector2(260f, 92f), TextAnchor.MiddleCenter);
        }

        static Text AddText(Transform parent, string name, string value, int fontSize, FontStyle style,
            Color color, Vector2 position, Vector2 size, TextAnchor alignment)
        {
            var go = new GameObject(name, typeof(RectTransform));
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
                    sb.AppendLine($"  Coral: {(site.coralScanned ? site.currentCoralCondition : "Not scanned")}");
                    sb.AppendLine($"  Water: {(site.sampleAnalysed ? site.waterClarity + " | " + site.temperature.ToString("0.0") + " C | " + site.temperatureSuitability : "Not tested")}");
                    sb.AppendLine($"  Rubbish: {site.rubbishRemoved}/{site.initialRubbish} in the bin");
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
