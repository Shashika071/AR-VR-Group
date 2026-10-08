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
        [SerializeField] GameObject hudRoot;
        [SerializeField] float feedbackSeconds = 3.5f;

        float feedbackUntil;

        void Awake()
        {
            if (hudRoot == null && objectiveText != null)
                hudRoot = objectiveText.transform.parent != null
                    ? objectiveText.transform.parent.gameObject
                    : null;
            SetHudVisible(false);
        }

        void OnEnable()
        {
            MissionEvents.ObjectiveChanged += OnObjective;
            MissionEvents.FeedbackRequested += OnFeedback;
            MissionEvents.StateChanged += OnState;
            MissionEvents.AnimalScanned += OnAnimal;
            MissionEvents.SampleCollected += OnSample;
            MissionEvents.BottleReturned += RefreshProgress;
            MissionEvents.BuoyRestored += RefreshProgress;
            MissionEvents.SurveySubmitted += RefreshProgress;

            if (MissionController.Instance != null)
                OnState(MissionState.Boot, MissionController.Instance.State);
        }

        void OnDisable()
        {
            MissionEvents.ObjectiveChanged -= OnObjective;
            MissionEvents.FeedbackRequested -= OnFeedback;
            MissionEvents.StateChanged -= OnState;
            MissionEvents.AnimalScanned -= OnAnimal;
            MissionEvents.SampleCollected -= OnSample;
            MissionEvents.BottleReturned -= RefreshProgress;
            MissionEvents.BuoyRestored -= RefreshProgress;
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

        void OnState(MissionState _, MissionState next)
        {
            // XR / simulator keeps a clear view: no objective bar over the controllers.
            var xr = MissionController.Instance != null &&
                     MissionController.Instance.PlayMode == PlayModeType.XR;
            var show = !xr && next is MissionState.TutorialMove or MissionState.TutorialGrab or MissionState.TutorialActivate
                or MissionState.GatherTools or MissionState.RepairBuoy or MissionState.SurveyAnimals
                or MissionState.CollectSample or MissionState.ReturnToStation or MissionState.ReturnBottle
                or MissionState.SubmitLog or MissionState.Results or MissionState.Credits or MissionState.Complete
                or MissionState.Paused;
            SetHudVisible(show);
            RefreshProgress();
            RefreshControls();
        }

        void SetHudVisible(bool visible)
        {
            if (hudRoot != null)
                hudRoot.SetActive(visible);
        }

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
                $"Buoy {(MissionController.Instance.BuoyRestored ? "OK" : "Silent")}  |  " +
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
                    "VR: mouse looks up/down | Hold Space, mouse up raises controller | G grab",
                PlayModeType.Desktop =>
                    "Desktop: WASD | Space jump | Right Mouse look | E grab | Click scanner | Q drop | Esc pause",
                _ => string.Empty
            };
        }
    }
}
