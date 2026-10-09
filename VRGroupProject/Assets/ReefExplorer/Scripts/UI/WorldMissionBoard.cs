using ReefExplorer.Core;
using ReefExplorer.Input;
using UnityEngine;
using UnityEngine.InputSystem;
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
                titleText.text = "<color=#62E6E1>REEF RESCUE</color>\n<color=#F4FBFF>THE SILENT SIGNAL</color>";
                titleText.fontSize = 42;
                titleText.color = new Color(0.96f, 0.99f, 1f);
                titleText.lineSpacing = 0.85f;
            }

            if (bodyText != null)
            {
                bodyText.text =
                    "<color=#62E6E1>FIELD BRIEF // CONSERVATION OPERATION</color>\n\n" +
                    "The Reef Seven monitoring buoy has gone silent. Restore its signal, " +
                    "collect reliable evidence, and help the team choose a safe coral-restoration site.\n\n" +
                    "<color=#F4FBFF><b>YOUR OBJECTIVES</b></color>\n" +
                    "<color=#A9D7E8>01</color> Restore the monitoring buoy with the power cell\n" +
                    "<color=#A9D7E8>02</color> Survey Coral Garden, Seagrass Crossing and Sandy Passage\n" +
                    "<color=#A9D7E8>03</color> Scan wildlife, coral, hazards and remove rubbish\n" +
                    "<color=#A9D7E8>04</color> Collect and analyse water samples from every site\n" +
                    "<color=#A9D7E8>05</color> Recommend a restoration site and place the marker\n" +
                    "<color=#A9D7E8>06</color> Submit the final survey report\n\n" +
                    "<color=#F4FBFF><b>STARTING THE DIVE</b></color>\n" +
                    "<color=#A9D7E8>[D]</color> Desktop keyboard and mouse    " +
                    "<color=#A9D7E8>[V]</color> VR / Simulator    " +
                    "<color=#A9D7E8>[ENTER]</color> Begin mission\n\n" +
                    "<color=#91AFC0>Desktop: WASD move • Right-click look • E grab • Left-click scan\n" +
                    "VR / Simulator: Space + mouse aim • G grab • Click activate</color>";
                bodyText.fontSize = 22;
                bodyText.color = new Color(0.88f, 0.95f, 0.98f);
                bodyText.lineSpacing = 1.05f;
                bodyText.alignment = TextAnchor.UpperLeft;
                bodyText.horizontalOverflow = HorizontalWrapMode.Wrap;
                bodyText.verticalOverflow = VerticalWrapMode.Overflow;

                var bodyRect = bodyText.rectTransform;
                bodyRect.anchoredPosition = new Vector2(0f, 45f);
                bodyRect.sizeDelta = new Vector2(900f, 500f);
            }

            if (titleText != null)
            {
                var titleRect = titleText.rectTransform;
                titleRect.anchoredPosition = new Vector2(0f, 270f);
                titleRect.sizeDelta = new Vector2(960f, 100f);
            }

            StyleBriefingPanel();
            HideIntroButtons();

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

        void Update()
        {
            if (MissionController.Instance == null ||
                MissionController.Instance.State != MissionState.Briefing ||
                Keyboard.current == null)
                return;

            if (Keyboard.current.dKey.wasPressedThisFrame)
                modeSelector?.ChooseDesktop();
            else if (Keyboard.current.vKey.wasPressedThisFrame)
                modeSelector?.ChooseXr();
            else if (Keyboard.current.enterKey.wasPressedThisFrame ||
                     Keyboard.current.numpadEnterKey.wasPressedThisFrame)
                MissionController.Instance.StartDive();
        }

        void HideIntroButtons()
        {
            startButton?.gameObject.SetActive(false);
            xrButton?.gameObject.SetActive(false);
            desktopButton?.gameObject.SetActive(false);
            creditsButton?.gameObject.SetActive(false);
            restartButton?.gameObject.SetActive(false);
            quitButton?.gameObject.SetActive(false);
        }

        void StyleBriefingPanel()
        {
            if (briefingPanel == null)
                return;

            var image = briefingPanel.GetComponent<Image>();
            if (image != null)
                image.color = new Color(0.025f, 0.075f, 0.12f, 0.985f);

            var outline = briefingPanel.GetComponent<Outline>();
            if (outline == null)
                outline = briefingPanel.AddComponent<Outline>();
            outline.effectColor = new Color(0.18f, 0.78f, 0.84f, 0.7f);
            outline.effectDistance = new Vector2(2f, -2f);

            CreateAccent("TopAccent", new Vector2(0f, 326f), new Vector2(860f, 5f),
                new Color(0.25f, 0.9f, 0.88f, 0.95f));
            CreateAccent("SideAccent", new Vector2(-478f, 0f), new Vector2(5f, 570f),
                new Color(0.2f, 0.64f, 0.78f, 0.75f));
        }

        void CreateAccent(string name, Vector2 position, Vector2 size, Color color)
        {
            if (briefingPanel.transform.Find(name) != null)
                return;

            var accent = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            accent.transform.SetParent(briefingPanel.transform, false);
            accent.transform.SetAsFirstSibling();

            var rect = accent.GetComponent<RectTransform>();
            rect.anchoredPosition = position;
            rect.sizeDelta = size;

            var image = accent.GetComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
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
