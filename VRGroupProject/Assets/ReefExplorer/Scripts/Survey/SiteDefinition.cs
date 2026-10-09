using System;
using UnityEngine;

namespace ReefExplorer.Survey
{
    /// <summary>
    /// Configurable data for one survey site (Coral Garden / Seagrass Crossing / Sandy Passage).
    /// Used by the comparison board and recommendation system.
    /// All readings are preset simulated data — not random.
    /// </summary>
    [CreateAssetMenu(menuName = "Reef Explorer/Site Definition", fileName = "Site_")]
    public sealed class SiteDefinition : ScriptableObject
    {
        [Header("Identity")]
        [SerializeField] string siteId = "site_coral";
        [SerializeField] string displayName = "Coral Garden";
        [SerializeField] string zoneId = "zone_coral";
        [TextArea][SerializeField] string description = "A patch of branching coral colonies on a rocky substrate.";

        [Header("Baseline (previous survey)")]
        [SerializeField] string baselineCoralCondition = "Moderate — some bleaching on table corals";
        [SerializeField] string baselineSubstrate = "Stable rock and rubble";

        [Header("Current observations (preset simulated)")]
        [SerializeField] string currentCoralCondition = "Fair — storm damage to branching corals, substrate intact";
        [SerializeField] string currentSubstrate = "Stable rock and rubble, minor loose fragments";

        [Header("Simulated water readings")]
        [SerializeField] WaterReadings waterReadings = new WaterReadings();

        [Header("Rubbish")]
        [SerializeField] int initialRubbishCount = 3;
        [SerializeField] string rubbishDescription = "Plastic bottles and a food container near the reef edge.";

        [Header("Hazard")]
        [SerializeField] bool hasHazard;
        [SerializeField] string hazardType = "";
        [SerializeField] string hazardDescription = "";
        [SerializeField] bool hazardBlocksRestoration;

        [Header("Decision criteria")]
        [SerializeField] bool suitableForRestoration = true;
        [TextArea][SerializeField] string suitabilityReason =
            "Suitable readings, stable substrate and manageable small debris.";
        [TextArea][SerializeField] string unsuitableReason = "";
        [SerializeField] int suitabilityScore = 80;

        [Header("Animal")]
        [SerializeField] SpeciesDefinition targetAnimal;
        [SerializeField] string animalObservationNote = "Clownfish sheltering in surviving anemones.";

        // --- Public API ---
        public string SiteId => siteId;
        public string DisplayName => displayName;
        public string ZoneId => zoneId;
        public string Description => description;
        public string BaselineCoralCondition => baselineCoralCondition;
        public string BaselineSubstrate => baselineSubstrate;
        public string CurrentCoralCondition => currentCoralCondition;
        public string CurrentSubstrate => currentSubstrate;
        public WaterReadings WaterReadings => waterReadings;
        public int InitialRubbishCount => initialRubbishCount;
        public string RubbishDescription => rubbishDescription;
        public bool HasHazard => hasHazard;
        public string HazardType => hazardType;
        public string HazardDescription => hazardDescription;
        public bool HazardBlocksRestoration => hazardBlocksRestoration;
        public bool SuitableForRestoration => suitableForRestoration;
        public string SuitabilityReason => suitabilityReason;
        public string UnsuitableReason => unsuitableReason;
        public int SuitabilityScore => suitabilityScore;
        public SpeciesDefinition TargetAnimal => targetAnimal;
        public string AnimalObservationNote => animalObservationNote;
    }

    /// <summary>
    /// Preset simulated water quality readings for a site.
    /// Clearly labelled as educational — not scientific standards.
    /// </summary>
    [Serializable]
    public sealed class WaterReadings
    {
        [Tooltip("Simulated clarity: Clear / Moderate / Murky")]
        public string waterClarity = "Moderate";

        [Tooltip("Simulated temperature in °C")]
        public float temperatureCelsius = 26.5f;

        [Tooltip("Is the temperature suitable for coral? (simulated)")]
        public string temperatureSuitability = "Suitable (25-29 °C range)";

        [Tooltip("Simulated pH")]
        public float pH = 8.1f;

        [Tooltip("Simulated current strength: Low / Moderate / Strong")]
        public string currentStrength = "Moderate";

        [Tooltip("Brief simulated summary for display")]
        public string summary = "Simulated readings for this educational mission.";
    }
}
