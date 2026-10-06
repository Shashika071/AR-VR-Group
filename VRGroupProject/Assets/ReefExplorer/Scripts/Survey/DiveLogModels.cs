using System;
using System.Collections.Generic;

namespace ReefExplorer.Survey
{
    [Serializable]
    public sealed class SpeciesObservation
    {
        public string observationId;
        public string speciesId;
        public string speciesDisplayName;
        public string zoneId;
        public float timeSeconds;
    }

    [Serializable]
    public sealed class DiveLogData
    {
        public string diveId;
        public string startedAtUtc;
        public string completedAtUtc;
        public bool waterSampleCollected;
        public List<string> visitedZones = new List<string>();
        public List<SpeciesObservation> observations = new List<SpeciesObservation>();
        public List<string> scannedAnimalIds = new List<string>();
    }

    [Serializable]
    public sealed class SpeciesComparisonRow
    {
        public string speciesId;
        public string displayName;
        public string zoneId;
        public int baselineCount;
        public int currentCount;
        public int difference;
    }

    [Serializable]
    public sealed class SurveyComparisonResult
    {
        public string baselineLabel;
        public string disclaimer;
        public bool missionComplete;
        public bool waterSampleCollected;
        public float zoneCoveragePercent;
        public int requiredSpeciesCount;
        public int observedRequiredSpeciesCount;
        public List<string> missingRequiredSpecies = new List<string>();
        public List<SpeciesComparisonRow> rows = new List<SpeciesComparisonRow>();
        public string plainLanguageSummary;
    }
}
