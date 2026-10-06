using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace ReefExplorer.Survey
{
    public static class SurveyAnalyzer
    {
        public static SurveyComparisonResult Compare(
            DiveLogData current,
            BaselineSurveyData baseline,
            IReadOnlyList<SpeciesDefinition> requiredSpecies,
            IReadOnlyList<string> requiredZones)
        {
            var result = new SurveyComparisonResult
            {
                baselineLabel = baseline != null ? baseline.SurveyLabel : "No baseline provided",
                disclaimer = baseline != null
                    ? baseline.Disclaimer
                    : "Simulated educational data only.",
                missionComplete = false,
                waterSampleCollected = current != null && current.waterSampleCollected,
                buoyRestored = current != null && current.buoyRestored
            };

            var requiredZoneCount = requiredZones != null ? requiredZones.Count : 0;
            var visited = 0;
            if (current?.visitedZones != null && requiredZones != null)
            {
                for (var i = 0; i < requiredZones.Count; i++)
                {
                    if (current.visitedZones.Exists(z =>
                            string.Equals(z, requiredZones[i], StringComparison.OrdinalIgnoreCase)))
                        visited++;
                }
            }

            result.zoneCoveragePercent = requiredZoneCount == 0
                ? 0f
                : (visited / (float)requiredZoneCount) * 100f;

            var required = requiredSpecies ?? Array.Empty<SpeciesDefinition>();
            result.requiredSpeciesCount = required.Count;

            var observedRequired = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (current?.observations != null)
            {
                foreach (var observation in current.observations)
                {
                    if (observation == null || string.IsNullOrEmpty(observation.speciesId))
                        continue;

                    foreach (var species in required)
                    {
                        if (species != null &&
                            string.Equals(species.SpeciesId, observation.speciesId, StringComparison.OrdinalIgnoreCase))
                        {
                            observedRequired.Add(species.SpeciesId);
                        }
                    }
                }
            }

            result.observedRequiredSpeciesCount = observedRequired.Count;
            foreach (var species in required)
            {
                if (species == null)
                    continue;
                if (!observedRequired.Contains(species.SpeciesId))
                    result.missingRequiredSpecies.Add(species.DisplayName);
            }

            BuildRows(result, current, baseline, required);

            result.missionComplete =
                result.buoyRestored &&
                result.waterSampleCollected &&
                result.missingRequiredSpecies.Count == 0 &&
                result.zoneCoveragePercent >= 99.9f;

            result.plainLanguageSummary = BuildSummary(result);
            return result;
        }

        static void BuildRows(
            SurveyComparisonResult result,
            DiveLogData current,
            BaselineSurveyData baseline,
            IReadOnlyList<SpeciesDefinition> required)
        {
            foreach (var species in required)
            {
                if (species == null)
                    continue;

                var zones = CollectZones(current, baseline, species.SpeciesId);
                if (zones.Count == 0)
                    zones.Add("all");

                foreach (var zone in zones)
                {
                    var currentCount = CountCurrent(current, species.SpeciesId, zone);
                    var baselineCount = baseline != null
                        ? baseline.GetCount(species.SpeciesId, zone == "all" ? null : zone)
                        : 0;

                    result.rows.Add(new SpeciesComparisonRow
                    {
                        speciesId = species.SpeciesId,
                        displayName = species.DisplayName,
                        zoneId = zone,
                        baselineCount = baselineCount,
                        currentCount = currentCount,
                        difference = currentCount - baselineCount
                    });
                }
            }
        }

        static List<string> CollectZones(DiveLogData current, BaselineSurveyData baseline, string speciesId)
        {
            var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (current?.observations != null)
            {
                foreach (var o in current.observations)
                {
                    if (o != null &&
                        string.Equals(o.speciesId, speciesId, StringComparison.OrdinalIgnoreCase) &&
                        !string.IsNullOrEmpty(o.zoneId))
                        set.Add(o.zoneId);
                }
            }

            if (baseline?.Entries != null)
            {
                foreach (var e in baseline.Entries)
                {
                    if (e?.species != null &&
                        string.Equals(e.species.SpeciesId, speciesId, StringComparison.OrdinalIgnoreCase) &&
                        !string.IsNullOrEmpty(e.zoneId))
                        set.Add(e.zoneId);
                }
            }

            return new List<string>(set);
        }

        static int CountCurrent(DiveLogData current, string speciesId, string zoneId)
        {
            if (current?.observations == null)
                return 0;

            var count = 0;
            foreach (var o in current.observations)
            {
                if (o == null)
                    continue;
                if (!string.Equals(o.speciesId, speciesId, StringComparison.OrdinalIgnoreCase))
                    continue;
                if (zoneId != "all" &&
                    !string.Equals(o.zoneId, zoneId, StringComparison.OrdinalIgnoreCase))
                    continue;
                count++;
            }

            return count;
        }

        static string BuildSummary(SurveyComparisonResult result)
        {
            var sb = new StringBuilder();
            sb.Append("You recorded ")
                .Append(result.observedRequiredSpeciesCount)
                .Append(" of ")
                .Append(result.requiredSpeciesCount)
                .Append(" required species. ");

            sb.Append("Survey area coverage: ")
                .Append(result.zoneCoveragePercent.ToString("0"))
                .Append("%. ");

            sb.Append(result.buoyRestored
                ? "Buoy signal restored. "
                : "Buoy still silent. ");

            sb.Append(result.waterSampleCollected
                ? "Water sample collected. "
                : "Water sample missing. ");

            if (result.missingRequiredSpecies.Count > 0)
            {
                sb.Append("Still missing: ")
                    .Append(string.Join(", ", result.missingRequiredSpecies))
                    .Append(". ");
            }

            sb.Append("Differences from the baseline are for learning only and do not prove reef health.");
            return sb.ToString();
        }
    }
}
