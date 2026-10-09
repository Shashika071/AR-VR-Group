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
        const float MessageSeconds = 5f;

        float feedbackUntil;

        void Awake()
        {
            if (hudRoot == null && objectiveText != null)
                hudRoot = objectiveText.transform.parent != null
                    ? objectiveText.transform.parent.gameObject
                    : null;

            if (objectiveText != null)
                objectiveText.gameObject.SetActive(false);
            if (progressText != null)
            {
                progressText.text = string.Empty;
                progressText.gameObject.SetActive(false);
            }
            if (controlsText != null)
            {
                controlsText.text = string.Empty;
                controlsText.gameObject.SetActive(false);
            }

            if (hudRoot != null)
            {
                var panel = hudRoot.GetComponent<RectTransform>();
                if (panel != null)
                {
                    panel.anchorMin = new Vector2(0f, 1f);
                    panel.anchorMax = new Vector2(0f, 1f);
                    panel.pivot = new Vector2(0f, 1f);
                    panel.anchoredPosition = new Vector2(12f, -12f);
                    panel.sizeDelta = new Vector2(340f, 26f);
                }
            }

            if (feedbackText != null)
            {
                var line = feedbackText.rectTransform;
                line.anchorMin = new Vector2(0f, 0.5f);
                line.anchorMax = new Vector2(0f, 0.5f);
                line.pivot = new Vector2(0f, 0.5f);
                line.anchoredPosition = new Vector2(8f, 0f);
                line.sizeDelta = new Vector2(324f, 22f);
                feedbackText.fontSize = 14;
                feedbackText.alignment = TextAnchor.MiddleLeft;
                feedbackText.horizontalOverflow = HorizontalWrapMode.Wrap;
                feedbackText.verticalOverflow = VerticalWrapMode.Overflow;
            }

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
            MissionEvents.CoralScanned += OnStringEvent;
            MissionEvents.RubbishCollected += OnStringEvent;
            MissionEvents.HazardFlagged += OnStringEvent;
            MissionEvents.WaterSampleAnalysed += OnStringEvent;
            MissionEvents.MarkerPlaced += RefreshProgress;

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
            MissionEvents.CoralScanned -= OnStringEvent;
            MissionEvents.RubbishCollected -= OnStringEvent;
            MissionEvents.HazardFlagged -= OnStringEvent;
            MissionEvents.WaterSampleAnalysed -= OnStringEvent;
            MissionEvents.MarkerPlaced -= RefreshProgress;
        }

        void Update()
        {
            if (Time.unscaledTime <= feedbackUntil)
                return;
            if (feedbackText != null && feedbackText.text.Length > 0)
                feedbackText.text = string.Empty;
            SetHudVisible(false);
        }

        void OnObjective(string text)
        {
            ShowOneLine(text);
        }

        void OnFeedback(string text) => ShowOneLine(text);

        void ShowOneLine(string text)
        {
            var line = OneLine(text);
            if (string.IsNullOrEmpty(line) || feedbackText == null)
                return;
            feedbackText.text = line;
            var width = Mathf.Clamp(line.Length * 8.2f + 20f, 140f, 560f);
            if (hudRoot != null)
            {
                var panel = hudRoot.GetComponent<RectTransform>();
                if (panel != null)
                    panel.sizeDelta = new Vector2(width, 28f);
            }

            var textRect = feedbackText.rectTransform;
            textRect.sizeDelta = new Vector2(Mathf.Max(80f, width - 16f), 22f);
            feedbackUntil = Time.unscaledTime + MessageSeconds;
            SetHudVisible(true);
        }

        static string OneLine(string text)
        {
            if (string.IsNullOrEmpty(text))
                return string.Empty;
            var line = text.Replace('\n', ' ').Replace('\r', ' ').Trim();
            var dot = line.IndexOf('.');
            if (dot >= 0)
                line = line.Substring(0, dot + 1);
            return line;
        }

        void OnState(MissionState _, MissionState next)
        {
            // Show HUD during all active gameplay states
            var show = next is MissionState.TutorialMove or MissionState.TutorialGrab or MissionState.TutorialActivate
                or MissionState.GatherTools or MissionState.RepairBuoy or MissionState.SurveyAnimals
                or MissionState.CollectSample or MissionState.CollectRubbish
                or MissionState.ReturnToStation or MissionState.ReturnBottle
                or MissionState.AnalyseSamples or MissionState.CompareAndChoose or MissionState.PlaceMarker
                or MissionState.SubmitLog or MissionState.Results or MissionState.Credits or MissionState.Complete;
            if (!show)
                SetHudVisible(false);
        }

        void SetHudVisible(bool visible)
        {
            if (hudRoot != null)
                hudRoot.SetActive(visible);
        }

        void OnAnimal(SpeciesObservation _) => RefreshProgress();
        void OnSample() => RefreshProgress();
        void OnStringEvent(string _) => RefreshProgress();

        void RefreshProgress()
        {
            if (progressText != null)
                progressText.text = string.Empty;
        }

        void RefreshControls()
        {
            if (controlsText != null)
                controlsText.text = string.Empty;
        }
    }
}
