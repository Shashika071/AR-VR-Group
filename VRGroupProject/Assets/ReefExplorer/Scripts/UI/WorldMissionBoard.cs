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
                titleText.text = "Reef Rescue — The Silent Signal";

            if (bodyText != null)
            {
                bodyText.text =
                    "Welcome, diver. Reef Buoy Seven has gone silent.\n\n" +
                    "Goals:\n" +
                    "1) Restore the buoy (insert the power cell)\n" +
                    "2) Record wildlife (clownfish, turtle, ray)\n" +
                    "3) Collect and return a water sample\n\n" +
                    "Choose Desktop or VR, then Start Dive.\n" +
                    "Baseline survey data is simulated for learning.";
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

            if (next == MissionState.Results || next == MissionState.Credits)
            {
                if (briefingPanel != null)
                    briefingPanel.SetActive(false);
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

            resultsText.text =
                "<b>First mission complete</b>\n" +
                "Your observations help the research team decide what to investigate next.\n\n" +
                $"Buoy restored: {(mc.BuoyRestored ? "Yes" : "No")}\n" +
                $"Species: {comparison.observedRequiredSpeciesCount}/{comparison.requiredSpeciesCount}\n" +
                $"Water sample returned: {(mc.BottleReturned ? "Yes" : "No")}\n" +
                $"Zone coverage: {comparison.zoneCoveragePercent:0}%\n\n" +
                $"<b>Survey comparison (simulated)</b>\n{comparison.plainLanguageSummary}\n\n" +
                $"Saved:\n{mc.LastSavedPath}\n\n{comparison.disclaimer}";
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
