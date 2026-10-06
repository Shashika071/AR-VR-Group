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

        void Awake()
        {
            if (titleText != null)
                titleText.text = "Reef Explorer — The Missing Survey";

            if (bodyText != null)
            {
                bodyText.text =
                    "You are a new marine research diver. Survey three reef zones, scan clownfish, turtle and ray, " +
                    "collect a water sample, return the bottle, then submit your dive log.\n\n" +
                    "Baseline comparison uses simulated educational data only.";
            }

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
                submitButton.interactable = next == MissionState.SubmitLog;

            if (resultsText != null && next == MissionState.Credits)
            {
                resultsText.text +=
                    "\n\nCredits: Student team project. Unity URP + XR Interaction Toolkit + OpenXR. " +
                    "Environment and animals use coursework primitives.";
            }
        }

        void OnSubmitted()
        {
            if (resultsText == null || MissionController.Instance == null)
                return;

            var comparison = MissionController.Instance.LastComparison;
            if (comparison == null)
                return;

            resultsText.text =
                $"<b>Survey Results (simulated)</b>\n{comparison.plainLanguageSummary}\n\n" +
                $"Saved dive log:\n{MissionController.Instance.LastSavedPath}\n\n{comparison.disclaimer}";
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
