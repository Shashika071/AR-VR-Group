using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using ReefExplorer.Survey;
using UnityEngine;

namespace ReefExplorer.Tests
{
    public sealed class SurveyAnalyzerTests
    {
        [Test]
        public void Compare_CountsDifferences_AndHandlesZeroBaseline()
        {
            var species = MakeSpecies("clownfish", "Clownfish");

            var baseline = ScriptableObject.CreateInstance<BaselineSurveyData>();
            SetField(baseline, "surveyLabel", "Baseline");
            SetField(baseline, "disclaimer", "Simulated educational data only.");
            SetField(baseline, "entries", new[]
            {
                new BaselineEntry { species = species, zoneId = "zone_coral", count = 0 }
            });

            var log = new DiveLogData
            {
                waterSampleCollected = true,
                buoyRestored = true,
                visitedZones = new List<string> { "zone_coral", "zone_turtle", "zone_ray" },
                observations = new List<SpeciesObservation>
                {
                    new SpeciesObservation
                    {
                        speciesId = "clownfish",
                        speciesDisplayName = "Clownfish",
                        zoneId = "zone_coral"
                    }
                }
            };

            var result = SurveyAnalyzer.Compare(
                log,
                baseline,
                new List<SpeciesDefinition> { species },
                new List<string> { "zone_coral", "zone_turtle", "zone_ray" });

            Assert.AreEqual(1, result.rows.Count);
            Assert.AreEqual(0, result.rows[0].baselineCount);
            Assert.AreEqual(1, result.rows[0].currentCount);
            Assert.AreEqual(1, result.rows[0].difference);
            Assert.AreEqual(100f, result.zoneCoveragePercent);
            StringAssert.Contains("Simulated", result.disclaimer);
        }

        [Test]
        public void Compare_IncompleteSurvey_ListsMissingSpecies()
        {
            var a = MakeSpecies("clownfish", "Clownfish");
            var b = MakeSpecies("ray", "Ray");

            var log = new DiveLogData
            {
                waterSampleCollected = false,
                visitedZones = new List<string> { "zone_coral" },
                observations = new List<SpeciesObservation>
                {
                    new SpeciesObservation { speciesId = "clownfish", zoneId = "zone_coral" }
                }
            };

            var result = SurveyAnalyzer.Compare(
                log,
                null,
                new List<SpeciesDefinition> { a, b },
                new List<string> { "zone_coral", "zone_turtle", "zone_ray" });

            Assert.IsFalse(result.missionComplete);
            Assert.Contains("Ray", result.missingRequiredSpecies);
            Assert.Less(result.zoneCoveragePercent, 100f);
        }

        [Test]
        public void DiveLogSaver_WritesJson()
        {
            var log = new DiveLogData
            {
                diveId = "test_dive",
                waterSampleCollected = true,
                observations = new List<SpeciesObservation>()
            };

            var path = DiveLogSaver.Save(log, null);
            Assert.IsTrue(System.IO.File.Exists(path));
            var json = System.IO.File.ReadAllText(path);
            StringAssert.Contains("test_dive", json);
            StringAssert.Contains("educational", json.ToLowerInvariant());
        }

        static SpeciesDefinition MakeSpecies(string id, string display)
        {
            var species = ScriptableObject.CreateInstance<SpeciesDefinition>();
            SetField(species, "speciesId", id);
            SetField(species, "displayName", display);
            return species;
        }

        static void SetField(object target, string fieldName, object value)
        {
            var field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(field, fieldName);
            field.SetValue(target, value);
        }
    }
}
