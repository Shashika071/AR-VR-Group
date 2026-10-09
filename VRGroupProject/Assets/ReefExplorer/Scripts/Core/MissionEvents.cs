using System;
using ReefExplorer.Survey;

namespace ReefExplorer.Core
{
    public static class MissionEvents
    {
        public static event Action<MissionState, MissionState> StateChanged;
        public static event Action<string> ObjectiveChanged;
        public static event Action<string> FeedbackRequested;
        public static event Action<SpeciesObservation> AnimalScanned;
        public static event Action SampleCollected;
        public static event Action BottleReturned;
        public static event Action BuoyRestored;
        public static event Action SurveySubmitted;
        public static event Action MissionRestarted;

        // New events for expanded mission
        public static event Action<string> CoralScanned;            // siteId
        public static event Action<string> WaterSampleCollected;    // siteId
        public static event Action<string> WaterSampleAnalysed;     // siteId
        public static event Action<string> RubbishCollected;        // rubbishId
        public static event Action<string> HazardFlagged;           // hazardId
        public static event Action<string> SiteRecommended;         // siteId
        public static event Action MarkerPlaced;
        public static event Action<string, string> RecommendationRejected; // siteId, reason

        public static void RaiseStateChanged(MissionState from, MissionState to) =>
            StateChanged?.Invoke(from, to);
        public static void RaiseObjective(string text) => ObjectiveChanged?.Invoke(text);
        public static void RaiseFeedback(string text) => FeedbackRequested?.Invoke(text);
        public static void RaiseAnimalScanned(SpeciesObservation observation) =>
            AnimalScanned?.Invoke(observation);
        public static void RaiseSampleCollected() => SampleCollected?.Invoke();
        public static void RaiseBottleReturned() => BottleReturned?.Invoke();
        public static void RaiseBuoyRestored() => BuoyRestored?.Invoke();
        public static void RaiseSurveySubmitted() => SurveySubmitted?.Invoke();
        public static void RaiseMissionRestarted() => MissionRestarted?.Invoke();

        // New event raisers
        public static void RaiseCoralScanned(string siteId) => CoralScanned?.Invoke(siteId);
        public static void RaiseWaterSampleCollected(string siteId) => WaterSampleCollected?.Invoke(siteId);
        public static void RaiseWaterSampleAnalysed(string siteId) => WaterSampleAnalysed?.Invoke(siteId);
        public static void RaiseRubbishCollected(string rubbishId) => RubbishCollected?.Invoke(rubbishId);
        public static void RaiseHazardFlagged(string hazardId) => HazardFlagged?.Invoke(hazardId);
        public static void RaiseSiteRecommended(string siteId) => SiteRecommended?.Invoke(siteId);
        public static void RaiseMarkerPlaced() => MarkerPlaced?.Invoke();
        public static void RaiseRecommendationRejected(string siteId, string reason) =>
            RecommendationRejected?.Invoke(siteId, reason);

        public static void ClearAll()
        {
            StateChanged = null;
            ObjectiveChanged = null;
            FeedbackRequested = null;
            AnimalScanned = null;
            SampleCollected = null;
            BottleReturned = null;
            BuoyRestored = null;
            SurveySubmitted = null;
            MissionRestarted = null;
            CoralScanned = null;
            WaterSampleCollected = null;
            WaterSampleAnalysed = null;
            RubbishCollected = null;
            HazardFlagged = null;
            SiteRecommended = null;
            MarkerPlaced = null;
            RecommendationRejected = null;
        }
    }
}
