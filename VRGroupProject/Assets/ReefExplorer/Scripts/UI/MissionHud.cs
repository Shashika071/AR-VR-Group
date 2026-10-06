using ReefExplorer.Core;
using ReefExplorer.Survey;
using UnityEngine;
using UnityEngine.UI;

namespace ReefExplorer.UI
{
    public sealed class MissionHud : MonoBehaviour
    {
        [SerializeField] Text objectiveText;
        [SerializeField] Text feedbackText;
        [SerializeField] Text progressText;
        [SerializeField] Text controlsText;
        [SerializeField] float feedbackSeconds = 3.5f;

        float feedbackUntil;

        void OnEnable()
        {
            MissionEvents.ObjectiveChanged += OnObjective;
            MissionEvents.FeedbackRequested += OnFeedback;
            MissionEvents.StateChanged += OnState;
            MissionEvents.AnimalScanned += OnAnimal;
            MissionEvents.SampleCollected += OnSample;
            MissionEvents.BottleReturned += RefreshProgress;
            MissionEvents.SurveySubmitted += RefreshProgress;
        }

        void OnDisable()
        {
            MissionEvents.ObjectiveChanged -= OnObjective;
            MissionEvents.FeedbackRequested -= OnFeedback;
            MissionEvents.StateChanged -= OnState;
            MissionEvents.AnimalScanned -= OnAnimal;
            MissionEvents.SampleCollected -= OnSample;
            MissionEvents.BottleReturned -= RefreshProgress;
            MissionEvents.SurveySubmitted -= RefreshProgress;
        }

        void Update()
        {
            if (feedbackText != null && Time.unscaledTime > feedbackUntil && feedbackText.text.Length > 0)
                feedbackText.text = string.Empty;
        }

        void OnObjective(string text)
        {
            if (objectiveText != null)
                objectiveText.text = text;
            RefreshProgress();
            RefreshControls();
        }

        void OnFeedback(string text)
        {
            if (feedbackText != null)
                feedbackText.text = text;
            feedbackUntil = Time.unscaledTime + feedbackSeconds;
        }

        void OnState(MissionState _, MissionState __) => RefreshProgress();

        void OnAnimal(SpeciesObservation _) => RefreshProgress();

        void OnSample() => RefreshProgress();

        void RefreshProgress()
        {
            if (progressText == null || MissionController.Instance == null)
                return;

            var log = MissionController.Instance.DiveLog;
            var species = MissionController.Instance.RequiredSpecies.Count;
            var found = 0;
            foreach (var required in MissionController.Instance.RequiredSpecies)
            {
                if (required == null) continue;
                if (log.observations.Exists(o => o.speciesId == required.SpeciesId))
                    found++;
            }

            progressText.text =
                $"Species {found}/{species}  |  Zones {log.visitedZones.Count}/{MissionController.Instance.RequiredZones.Count}  |  " +
                $"Sample {(log.waterSampleCollected ? "Yes" : "No")}  |  Bottle {(MissionController.Instance.BottleReturned ? "Returned" : "Out")}";
        }

        void RefreshControls()
        {
            if (controlsText == null || MissionController.Instance == null)
                return;

            controlsText.text = MissionController.Instance.PlayMode switch
            {
                PlayModeType.XR =>
                    "VR / Simulator: Teleport + snap turn | Hold Space/Shift to move controllers | G = Grab | Mouse Click = Activate",
                PlayModeType.Desktop =>
                    "Desktop: WASD move | Right Mouse look | E / Left Click pick up | Left Click hold activate scanner | Q drop | Esc pause",
                _ => "Choose VR or Desktop to see controls."
            };
        }
    }
}
