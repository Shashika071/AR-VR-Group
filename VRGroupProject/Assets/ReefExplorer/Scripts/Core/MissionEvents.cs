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
        }
    }
}
