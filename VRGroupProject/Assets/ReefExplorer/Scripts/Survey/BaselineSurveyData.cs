using System;
using UnityEngine;

namespace ReefExplorer.Survey
{
    [CreateAssetMenu(menuName = "Reef Explorer/Baseline Survey Data", fileName = "BaselineSurvey")]
    public sealed class BaselineSurveyData : ScriptableObject
    {
        [SerializeField] string surveyLabel = "Previous simulated survey";
        [SerializeField] string disclaimer =
            "Simulated educational data only. Do not treat this as a real reef-health assessment.";
        [SerializeField] BaselineEntry[] entries = Array.Empty<BaselineEntry>();

        public string SurveyLabel => surveyLabel;
        public string Disclaimer => disclaimer;
        public BaselineEntry[] Entries => entries;

        public int GetCount(string speciesId, string zoneId)
        {
            if (string.IsNullOrEmpty(speciesId) || entries == null)
                return 0;

            var total = 0;
            for (var i = 0; i < entries.Length; i++)
            {
                var entry = entries[i];
                if (entry == null || entry.species == null)
                    continue;

                if (!string.Equals(entry.species.SpeciesId, speciesId, StringComparison.OrdinalIgnoreCase))
                    continue;

                if (!string.IsNullOrEmpty(zoneId) &&
                    !string.Equals(entry.zoneId, zoneId, StringComparison.OrdinalIgnoreCase))
                    continue;

                total += Mathf.Max(0, entry.count);
            }

            return total;
        }
    }

    [Serializable]
    public sealed class BaselineEntry
    {
        public SpeciesDefinition species;
        public string zoneId = "zone_a";
        public int count = 1;
    }
}
