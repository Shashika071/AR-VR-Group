using System;
using System.Collections.Generic;

namespace ReefExplorer.Survey
{
    /// <summary>
    /// Extended dive log capturing all mission data for the "Reef Rescue — A Safe Place to Grow" mission.
    /// JSON-serializable for saving.
    /// </summary>
    [Serializable]
    public sealed class DiveLogData
    {
        public int schemaVersion = 2;
        public string diveId;
        public string startedAtUtc;
        public string completedAtUtc;
        public bool buoyRestored;

        // --- Site surveys ---
        public List<string> visitedZones = new List<string>();
        public List<SpeciesObservation> observations = new List<SpeciesObservation>();
        public List<string> scannedAnimalIds = new List<string>();

        /// <summary>Per-site coral condition scan records.</summary>
        public List<CoralScanRecord> coralScans = new List<CoralScanRecord>();

        // --- Water samples ---
        [Obsolete("Use perSiteSamples instead")] public bool waterSampleCollected;
        public List<WaterSampleRecord> perSiteSamples = new List<WaterSampleRecord>();

        // --- Rubbish ---
        public List<RubbishRecord> rubbishCollected = new List<RubbishRecord>();

        // --- Hazards ---
        public List<HazardRecord> hazardsFlagged = new List<HazardRecord>();

        // --- Recommendation ---
        public string recommendedSiteId;
        public string recommendationReason;
        public bool markerPlaced;
        public string markerSiteId;

        // --- Completed objectives checklist ---
        public List<string> completedObjectives = new List<string>();
    }

    [Serializable]
    public sealed class SpeciesObservation
    {
        public string observationId;
        public string speciesId;
        public string speciesDisplayName;
        public string zoneId;
        public string siteId;
        public float timeSeconds;
    }

    [Serializable]
    public sealed class CoralScanRecord
    {
        public string siteId;
        public string condition;
        public float timeSeconds;
        public bool polluted;
    }

    [Serializable]
    public sealed class WaterSampleRecord
    {
        public string sampleId;
        public string siteId;
        public bool collected;
        public bool analysed;
        public float collectionTime;
        public float analysisTime;
    }

    [Serializable]
    public sealed class RubbishRecord
    {
        public string rubbishId;
        public string siteId;
        public float timeSeconds;
    }

    [Serializable]
    public sealed class HazardRecord
    {
        public string hazardId;
        public string siteId;
        public string hazardType;
        public float timeSeconds;
    }

    // --- Comparison / results models ---

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
    public sealed class SiteComparisonData
    {
        public string siteId;
        public string displayName;
        public string baselineCoralCondition;
        public string currentCoralCondition;
        public string waterClarity;
        public float temperature;
        public string temperatureSuitability;
        public string currentStrength;
        public int initialRubbish;
        public int rubbishRemoved;
        public int rubbishRemaining;
        public bool hasHazard;
        public string hazardType;
        public bool hazardFlagged;
        public string animalObserved;
        public string animalNote;
        public bool coralScanned;
        public bool sampleCollected;
        public bool sampleAnalysed;
        public int suitabilityScore;
        public string suitabilityReason;
        public bool observationComplete;
    }

    [Serializable]
    public sealed class SurveyComparisonResult
    {
        public string baselineLabel;
        public string disclaimer;
        public bool missionComplete;
        // Legacy compatibility
        public bool waterSampleCollected;
        public bool buoyRestored;
        public float zoneCoveragePercent;
        public int requiredSpeciesCount;
        public int observedRequiredSpeciesCount;
        public List<string> missingRequiredSpecies = new List<string>();
        public List<SpeciesComparisonRow> rows = new List<SpeciesComparisonRow>();
        public string plainLanguageSummary;

        // New per-site comparison data
        public List<SiteComparisonData> siteComparisons = new List<SiteComparisonData>();
        public string recommendedSiteId;
        public string recommendationExplanation;
        public int totalRubbishCollected;
        public int totalHazardsFlagged;
    }
}
