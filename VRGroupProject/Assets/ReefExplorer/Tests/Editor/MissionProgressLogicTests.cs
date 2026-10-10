using System.Collections.Generic;
using NUnit.Framework;
using ReefExplorer.Survey;

namespace ReefExplorer.Tests
{
    public sealed class MissionProgressLogicTests
    {
        [Test]
        public void DuplicateAnimalIds_AreDetectableFromDiveLog()
        {
            var log = new DiveLogData();
            const string animalId = "clownfish_01";

            Assert.IsFalse(log.scannedAnimalIds.Contains(animalId));
            log.scannedAnimalIds.Add(animalId);
            Assert.IsTrue(log.scannedAnimalIds.Contains(animalId));
        }

        [Test]
        public void RestartClearsCollections_WithoutNullRefs()
        {
            var log = new DiveLogData
            {
                waterSampleCollected = true,
                visitedZones = new List<string> { "zone_coral" },
                observations = new List<SpeciesObservation> { new SpeciesObservation { speciesId = "x" } },
                scannedAnimalIds = new List<string> { "a" }
            };

            log.waterSampleCollected = false;
            log.visitedZones.Clear();
            log.observations.Clear();
            log.scannedAnimalIds.Clear();

            Assert.IsFalse(log.waterSampleCollected);
            Assert.IsEmpty(log.visitedZones);
            Assert.IsEmpty(log.observations);
            Assert.IsEmpty(log.scannedAnimalIds);
        }
    }
}
