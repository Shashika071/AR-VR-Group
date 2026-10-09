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
                titleText.text = "Reef Rescue — A Safe Place to Grow";

            if (bodyText != null)
            {
                bodyText.text =
                    "Welcome, diver. You are helping a conservation team select\n" +
                    "a coral-restoration trial site.\n\n" +
                    "<b>Your Mission:</b>\n" +
                    "1) Pick up the SCANNER and BOTTLE from the console\n" +
                    "2) Follow the path to the buoy — insert the power cell\n" +
                    "3) Visit all 3 sites (Coral Garden, Seagrass Crossing, Sandy Passage)\n" +
                    "   At each site:\n" +
                    "   • SCAN the coral survey point (aim scanner + click/trigger)\n" +
                    "   • SCAN the animal\n" +
                    "   • COLLECT a water sample (bring bottle to sample zone + press E/trigger)\n" +
                    "   • PICK UP rubbish (grab it)\n" +
                    "   • SCAN any hazards (do NOT touch — just scan)\n" +
                    "4) Return to station — place bottle in SAMPLE ANALYSER\n" +
                    "5) Review the COMPARISON BOARD and recommend a site\n" +
                    "6) Take the MARKER to the chosen site\n" +
                    "7) Submit your report\n\n" +
                    "<b>Controls:</b>\n" +
                    "Desktop: WASD move | Right-click look | E grab | Left-click scanner\n" +
                    "VR/Sim: Hold Space + mouse to aim | G grab | Click to activate\n\n" +
                    "Press VR/Simulator or Desktop, then Start Mission.";
            }

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
