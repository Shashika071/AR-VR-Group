using System;
using System.Collections.Generic;
using System.Linq;
using ReefExplorer.Survey;
using ReefExplorer.Audio;
using ReefExplorer.UI;
using UnityEngine;

namespace ReefExplorer.Core
{
    /// <summary>
    /// Central mission state machine for Reef Rescue — A Safe Place to Grow.
    /// Replaces the Silent Signal mission controller with the expanded restoration-site mission.
    /// </summary>
    public sealed class MissionController : MonoBehaviour
    {
        public static MissionController Instance { get; private set; }

        [Header("Survey Data")]
        [SerializeField] BaselineSurveyData baselineSurvey;
        [SerializeField] SpeciesDefinition[] requiredSpecies = Array.Empty<SpeciesDefinition>();
        [SerializeField] string[] requiredZones = { "zone_coral", "zone_turtle", "zone_ray" };
        [SerializeField] SiteDefinition[] sites = Array.Empty<SiteDefinition>();

        [Header("Runtime")]
        [SerializeField] MissionState state = MissionState.Boot;
        [SerializeField] PlayModeType playMode = PlayModeType.Unselected;

        readonly DiveLogData diveLog = new DiveLogData();
        readonly HashSet<string> tutorialFlags = new HashSet<string>();
        MissionState stateBeforePause = MissionState.Briefing;
        SurveyComparisonResult lastComparison;
        string lastSavedPath;

        // Site tracking
        readonly Dictionary<string, SiteProgress> siteProgress = new Dictionary<string, SiteProgress>(StringComparer.OrdinalIgnoreCase);

        public MissionState State => state;
        public PlayModeType PlayMode => playMode;
        public DiveLogData DiveLog => diveLog;
        public SurveyComparisonResult LastComparison => lastComparison;
        public string LastSavedPath => lastSavedPath;
        public BaselineSurveyData BaselineSurvey => baselineSurvey;
        public IReadOnlyList<SpeciesDefinition> RequiredSpecies => requiredSpecies;
        public IReadOnlyList<string> RequiredZones => requiredZones;
        public IReadOnlyList<SiteDefinition> Sites => sites;

        public bool HasScanner { get; private set; }
        public bool HasBottle { get; private set; }
        public bool BuoyRestored { get; private set; }
        public bool BaselineUnlocked => BuoyRestored;
        public bool BottleFilled => diveLog.perSiteSamples.Exists(s => s.collected);
        public bool BottleReturned { get; private set; }

        // Legacy compat
#pragma warning disable CS0618
        public bool AllSamplesCollected => HasAllWaterSamples();
        public bool AllSamplesAnalysed => sites.Length == 0 || diveLog.perSiteSamples.TrueForAll(s => s.analysed);
#pragma warning restore CS0618

        public string RecommendedSiteId => diveLog.recommendedSiteId;
        public bool MarkerPlaced => diveLog.markerPlaced;

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            ResetSession("boot");
            SetState(MissionState.ModeSelect);
            SetObjective("Choose VR Headset / Simulator or Desktop Keyboard & Mouse.");
        }

        void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        // ───────── Mode Selection ─────────

        public void SelectPlayMode(PlayModeType mode)
        {
            if (mode == PlayModeType.Unselected)
                return;

            playMode = mode;

            if (state is MissionState.Boot or MissionState.ModeSelect or MissionState.Briefing)
            {
                SetState(MissionState.Briefing);
                SetObjective("Read the mission board, then press Start Mission.");
            }
        }

        public void StartDive()
        {
            if (playMode == PlayModeType.Unselected)
            {
                MissionEvents.RaiseFeedback("Choose VR or Desktop mode first.");
                return;
            }

            SetState(MissionState.TutorialMove);
            SetObjective(playMode == PlayModeType.Desktop
                ? "Tutorial: walk forward with WASD to the marked training gate."
                : "Tutorial: teleport forward to the marked training gate.");
        }

        // ───────── Tutorial ─────────

        public void NotifyTutorialStep(string stepId)
        {
            if (string.IsNullOrEmpty(stepId) || tutorialFlags.Contains(stepId))
                return;

            tutorialFlags.Add(stepId);

            switch (state)
            {
                case MissionState.TutorialMove when stepId == "move":
                    SetState(MissionState.GatherTools);
                    SetObjective("Pick up the scanner and a sample bottle from the console.");
                    break;
            }
        }

        // ───────── Tool Pickup ─────────

        public void NotifyToolPicked(string toolId)
        {
            if (toolId == "scanner")
                HasScanner = true;
            if (toolId == "bottle")
                HasBottle = true;

            if (state == MissionState.GatherTools && HasScanner && HasBottle)
            {
                SetState(MissionState.RepairBuoy);
                SetObjective("Follow the path to the monitoring station. Pick up the loose power cell and insert it.");
            }
        }

        // ───────── Buoy Repair ─────────

        public bool TryRestoreBuoy()
        {
            if (BuoyRestored)
            {
                MissionEvents.RaiseFeedback("Monitoring station already restored.");
                return false;
            }

            if (state is MissionState.Boot or MissionState.ModeSelect or MissionState.Paused
                or MissionState.Complete or MissionState.Credits or MissionState.Results)
            {
                MissionEvents.RaiseFeedback("Start the dive, then place the buoy battery in the cyan box.");
                return false;
            }

            BuoyRestored = true;
            diveLog.buoyRestored = true;
            CompleteObjective("buoy_restored");
            MissionEvents.RaiseBuoyRestored();
            SetState(MissionState.SurveyAnimals);
            SetObjective("Communication restored. Visit Coral Garden, Seagrass Crossing and Starfish Ledge.\n" +
                          "At each site: scan the coral survey point, scan the animal, collect a water sample, and pick up rubbish.");
            MissionEvents.RaiseFeedback(
                "Communication restored. We have the previous survey. Now we need to see what has changed. — Maya");
            return true;
        }

        // ───────── Zone Entry ─────────

        public void NotifyZoneEntered(string zoneId)
        {
            if (string.IsNullOrEmpty(zoneId))
                return;

            if (!diveLog.visitedZones.Exists(z =>
                    string.Equals(z, zoneId, StringComparison.OrdinalIgnoreCase)))
            {
                diveLog.visitedZones.Add(zoneId);
            }
        }

        // ───────── Coral Scanning ─────────

        public bool TryRecordCoralScan(string siteId)
        {
            if (string.IsNullOrEmpty(siteId))
                return false;

            var site = GetSite(siteId);
            if (site == null)
            {
                MissionEvents.RaiseFeedback("Unknown survey site.");
                return false;
            }

            var progress = GetOrCreateSiteProgress(siteId);
            var rescan = progress.coralScanned && CanRescanCoral(siteId);
            if (progress.coralScanned && !rescan)
            {
                MissionEvents.RaiseFeedback("Coral condition already recorded for this site.");
                return false;
            }

            if (rescan)
                diveLog.coralScans.RemoveAll(s =>
                    s != null && string.Equals(s.siteId, siteId, StringComparison.OrdinalIgnoreCase));

            var polluted = SiteIsPolluted(siteId);
            var condition = DescribeCoral(siteId);
            progress.coralScanned = true;
            diveLog.coralScans.Add(new CoralScanRecord
            {
                siteId = siteId,
                condition = condition,
                timeSeconds = Time.timeSinceLevelLoad,
                polluted = polluted
            });

            MissionEvents.RaiseCoralScanned(siteId);
            var body = site.DisplayName + "\n" + condition;
            if (polluted)
                body += "\nClean the rubbish and toxin, then scan again.";
            DiveReadout.Show("CORAL SCAN", body, 9f);
            MissionEvents.RaiseFeedback(polluted
                ? "Coral is polluted, clean it and scan again."
                : "Coral scan finished.");
            CheckSurveyProgress();
            return true;
        }

        public bool SiteIsPolluted(string siteId)
        {
            var site = GetSite(siteId);
            if (site == null)
                return false;

            if (site.HasHazard && !diveLog.hazardsFlagged.Exists(h =>
                    h != null && string.Equals(h.siteId, siteId, StringComparison.OrdinalIgnoreCase)))
                return true;

            foreach (var item in FindObjectsByType<ReefExplorer.Interaction.RubbishItem>(FindObjectsSortMode.None))
            {
                if (item != null && item.gameObject.activeInHierarchy &&
                    string.Equals(item.SiteId, siteId, StringComparison.OrdinalIgnoreCase))
                    return true;
            }

            return false;
        }

        public bool CanRescanCoral(string siteId)
        {
            var record = diveLog.coralScans.Find(s =>
                s != null && s.polluted && string.Equals(s.siteId, siteId, StringComparison.OrdinalIgnoreCase));
            return record != null && !SiteIsPolluted(siteId);
        }

        public string DescribeCoral(string siteId)
        {
            var site = GetSite(siteId);
            if (site == null)
                return "Unknown coral.";
            if (!SiteIsPolluted(siteId))
                return site.CurrentCoralCondition;
            if (site.HasHazard)
                return "Polluted — toxin is in the water and debris is on the coral";
            return "Polluted — rubbish is covering the coral";
        }

        public string LatestCoralCondition(string siteId)
        {
            CoralScanRecord last = null;
            foreach (var scan in diveLog.coralScans)
            {
                if (scan != null && string.Equals(scan.siteId, siteId, StringComparison.OrdinalIgnoreCase))
                    last = scan;
            }

            return last != null ? last.condition : null;
        }

        // ───────── Animal Scanning ─────────

        public bool TryRecordAnimalScan(string animalInstanceId, SpeciesDefinition species, string zoneId)
        {
            if (species == null || string.IsNullOrEmpty(animalInstanceId))
                return false;

            if (diveLog.scannedAnimalIds.Contains(animalInstanceId))
            {
                MissionEvents.RaiseFeedback("Already scanned. Find a different required animal.");
                return false;
            }

            // Find associated site
            var siteId = GetSiteIdForZone(zoneId);

            var observation = new SpeciesObservation
            {
                observationId = Guid.NewGuid().ToString("N"),
                speciesId = species.SpeciesId,
                speciesDisplayName = species.DisplayName,
                zoneId = zoneId,
                siteId = siteId,
                timeSeconds = Time.timeSinceLevelLoad
            };

            diveLog.scannedAnimalIds.Add(animalInstanceId);
            diveLog.observations.Add(observation);

            if (!string.IsNullOrEmpty(siteId))
            {
                var progress = GetOrCreateSiteProgress(siteId);
                progress.animalScanned = true;
            }

            MissionEvents.RaiseAnimalScanned(observation);

            var fact = string.IsNullOrWhiteSpace(species.Description)
                ? $"Logged {species.DisplayName}."
                : $"Logged {species.DisplayName}. Fact: {species.Description}";
            MissionEvents.RaiseFeedback(fact);

            CheckSurveyProgress();
            return true;
        }

        // ───────── Water Sample Collection ─────────

        public bool TryCollectSample(string siteId, bool bottlePresent, bool inSampleZone)
        {
            if (!inSampleZone)
            {
                MissionEvents.RaiseFeedback("Move the bottle into the marked sampling area.");
                return false;
            }

            if (!bottlePresent)
            {
                MissionEvents.RaiseFeedback("Bring a sample bottle into the sampling area.");
                return false;
            }

            if (string.IsNullOrEmpty(siteId))
            {
                // Legacy compat: fall back to single sample
                return TryFillBottle(bottlePresent, inSampleZone);
            }

            if (diveLog.perSiteSamples.Exists(s =>
                    string.Equals(s.siteId, siteId, StringComparison.OrdinalIgnoreCase) && s.collected))
            {
                MissionEvents.RaiseFeedback("Sample already collected from this site.");
                return false;
            }

            var sample = new WaterSampleRecord
            {
                sampleId = Guid.NewGuid().ToString("N"),
                siteId = siteId,
                collected = true,
                analysed = false,
                collectionTime = Time.timeSinceLevelLoad
            };
            diveLog.perSiteSamples.Add(sample);

            var progress = GetOrCreateSiteProgress(siteId);
            progress.sampleCollected = true;

            var site = GetSite(siteId);
            var siteName = site != null ? site.DisplayName : siteId;

            MissionEvents.RaiseWaterSampleCollected(siteId);
            MissionEvents.RaiseSampleCollected();
            if (HasAllWaterSamples())
                MissionEvents.RaiseFeedback("All water samples complete, put the bottle in the box on the table.");
            else
                MissionEvents.RaiseFeedback($"Water sample collected from {siteName}.");

            CheckSurveyProgress();
            return true;
        }

        // Legacy single-bottle method
        public bool TryFillBottle(bool bottlePresent, bool inSampleZone)
        {
            if (!inSampleZone)
            {
                MissionEvents.RaiseFeedback("Move the bottle into the marked sampling area.");
                return false;
            }

            if (!bottlePresent)
            {
                MissionEvents.RaiseFeedback("Bring the sample bottle into the sampling area.");
                return false;
            }

#pragma warning disable CS0618
            if (diveLog.waterSampleCollected)
            {
                MissionEvents.RaiseFeedback("Bottle already filled.");
                return false;
            }

            diveLog.waterSampleCollected = true;
#pragma warning restore CS0618
            MissionEvents.RaiseSampleCollected();
            CheckSurveyProgress();
            return true;
        }

        // ───────── Rubbish Collection ─────────

        public bool TryCollectRubbish(string rubbishId, string siteId)
        {
            if (string.IsNullOrEmpty(rubbishId))
                return false;

            if (diveLog.rubbishCollected.Exists(r =>
                    string.Equals(r.rubbishId, rubbishId, StringComparison.OrdinalIgnoreCase)))
            {
                MissionEvents.RaiseFeedback("This item has already been collected.");
                return false;
            }

            diveLog.rubbishCollected.Add(new RubbishRecord
            {
                rubbishId = rubbishId,
                siteId = siteId,
                timeSeconds = Time.timeSinceLevelLoad
            });

            MissionEvents.RaiseRubbishCollected(rubbishId);
            MissionEvents.RaiseFeedback($"Rubbish collected and placed in basket. ({diveLog.rubbishCollected.Count} total)");

            CheckSurveyProgress();
            return true;
        }

        // ───────── Hazard Flagging ─────────

        public bool TryFlagHazard(string hazardId, string siteId, string hazardType)
        {
            if (string.IsNullOrEmpty(hazardId))
                return false;

            if (diveLog.hazardsFlagged.Exists(h =>
                    string.Equals(h.hazardId, hazardId, StringComparison.OrdinalIgnoreCase)))
            {
                MissionEvents.RaiseFeedback("This hazard has already been flagged for the specialist team.");
                return false;
            }

            diveLog.hazardsFlagged.Add(new HazardRecord
            {
                hazardId = hazardId,
                siteId = siteId,
                hazardType = hazardType,
                timeSeconds = Time.timeSinceLevelLoad
            });

            if (!string.IsNullOrEmpty(siteId))
            {
                var progress = GetOrCreateSiteProgress(siteId);
                progress.hazardFlagged = true;
            }

            MissionEvents.RaiseHazardFlagged(hazardId);
            MissionEvents.RaiseFeedback("Hazard flagged for specialist removal team. Do not attempt to remove it yourself.");

            CheckSurveyProgress();
            return true;
        }

        // ───────── Station Return ─────────

        public void NotifyReturnedToStation()
        {
            if (state == MissionState.ReturnToStation)
            {
                SetState(MissionState.AnalyseSamples);
                SetObjective("Place your water samples into the analyser to get readings for each site.");
            }
            else if (state == MissionState.ReturnBottle)
            {
                SetState(MissionState.AnalyseSamples);
                SetObjective("Place your water samples into the analyser.");
            }
        }

        // ───────── Sample Analysis ─────────

        public bool TryAnalyseSample(string siteId)
        {
            if (string.IsNullOrEmpty(siteId))
            {
                MissionEvents.RaiseFeedback("This sample has no site label. Collect a labelled sample first.");
                return false;
            }

            var sample = diveLog.perSiteSamples.Find(s =>
                string.Equals(s.siteId, siteId, StringComparison.OrdinalIgnoreCase));

            if (sample == null || !sample.collected)
            {
                MissionEvents.RaiseFeedback($"No collected sample from this site. Visit the sampling point at the site first.");
                return false;
            }

            if (sample.analysed)
            {
                MissionEvents.RaiseFeedback("This sample has already been analysed.");
                return false;
            }

            sample.analysed = true;
            sample.analysisTime = Time.timeSinceLevelLoad;

            var progress = GetOrCreateSiteProgress(siteId);
            progress.sampleAnalysed = true;

            var site = GetSite(siteId);
            MissionEvents.RaiseWaterSampleAnalysed(siteId);

            if (site != null)
            {
                MissionEvents.RaiseFeedback(
                    $"Analysis complete for {site.DisplayName}.\n" +
                    $"Clarity: {site.WaterReadings.waterClarity} | " +
                    $"Temp: {site.WaterReadings.temperatureCelsius:F1} °C ({site.WaterReadings.temperatureSuitability}) | " +
                    $"pH: {site.WaterReadings.pH:F1}\n" +
                    $"(Simulated readings for this educational mission)");
            }

            if (AllSamplesAnalysed)
            {
                SetState(MissionState.CompareAndChoose);
                SetObjective("All samples analysed. Review the comparison board and choose a restoration trial site.");
                BuildComparison();
            }

            return true;
        }

        // ───────── Bottle Return (legacy compat) ─────────

        public bool TryAcceptReturnedBottle(bool isFilledBottle)
        {
            if (!isFilledBottle)
            {
                MissionEvents.RaiseFeedback("Only a filled sample bottle fits this holder.");
                return false;
            }

            BottleReturned = true;
            MissionEvents.RaiseBottleReturned();

            // Check if we should move to analysis
            if (state == MissionState.ReturnBottle || state == MissionState.ReturnToStation)
            {
                SetState(MissionState.AnalyseSamples);
                SetObjective("Place water samples into the analyser for each site.");
            }

            return true;
        }

        // ───────── Site Recommendation ─────────

        public bool TryRecommendSite(string siteId)
        {
            if (string.IsNullOrEmpty(siteId))
                return false;

            var site = GetSite(siteId);
            if (site == null)
            {
                MissionEvents.RaiseFeedback("Unknown site.");
                return false;
            }

            // Check evidence completeness
            if (!HasSufficientEvidence())
            {
                MissionEvents.RaiseFeedback("Collect more evidence before making a recommendation. Complete all site surveys first.");
                return false;
            }

            // Check if choice conflicts with evidence
            if (!site.SuitableForRestoration)
            {
                var reason = string.IsNullOrEmpty(site.UnsuitableReason)
                    ? $"{site.DisplayName} may not be suitable for restoration."
                    : site.UnsuitableReason;

                MissionEvents.RaiseRecommendationRejected(siteId, reason);
                MissionEvents.RaiseFeedback($"Consider: {reason}\nReview the evidence and you may reconsider.");
                return false;
            }

            diveLog.recommendedSiteId = siteId;
            diveLog.recommendationReason = site.SuitabilityReason;
            CompleteObjective("site_recommended");
            MissionEvents.RaiseSiteRecommended(siteId);
            MissionEvents.RaiseFeedback(
                $"Recommendation accepted: {site.DisplayName}.\n" +
                $"Reason: {site.SuitabilityReason}\n" +
                "Carry the research marker to the site and place it in the designated holder.");

            SetState(MissionState.PlaceMarker);
            SetObjective($"Carry the research marker to {site.DisplayName} and place it in the holder.");

            return true;
        }

        // ───────── Marker Placement ─────────

        public bool TryPlaceMarker(string atSiteId)
        {
            if (string.IsNullOrEmpty(diveLog.recommendedSiteId))
            {
                MissionEvents.RaiseFeedback("Choose a restoration site recommendation first.");
                return false;
            }

            if (!string.Equals(atSiteId, diveLog.recommendedSiteId, StringComparison.OrdinalIgnoreCase))
            {
                var correctSite = GetSite(diveLog.recommendedSiteId);
                var correctName = correctSite != null ? correctSite.DisplayName : diveLog.recommendedSiteId;
                MissionEvents.RaiseFeedback($"This marker belongs at the recommended site: {correctName}.");
                return false;
            }

            diveLog.markerPlaced = true;
            diveLog.markerSiteId = atSiteId;
            CompleteObjective("marker_placed");
            MissionEvents.RaiseMarkerPlaced();
            MissionEvents.RaiseFeedback("Research marker placed. Return to the station to submit your final report.");

            SetState(MissionState.SubmitLog);
            SetObjective("Return to the research station and submit your final report.");
            return true;
        }

        // ───────── Submit ─────────

        public bool CanSubmit()
        {
            return BuoyRestored &&
                   HasAllRequiredSpecies() &&
                   HasAllZones() &&
                   !string.IsNullOrEmpty(diveLog.recommendedSiteId) &&
                   diveLog.markerPlaced;
        }

        public bool TrySubmit()
        {
            if (!CanSubmit())
            {
                MissionEvents.RaiseFeedback(BuildIncompleteMessage());
                return false;
            }

            diveLog.completedAtUtc = DateTime.UtcNow.ToString("o");
            BuildComparison();
            lastSavedPath = DiveLogSaver.Save(diveLog, lastComparison);
            MissionEvents.RaiseSurveySubmitted();
            SetState(MissionState.Results);
            SetObjective("Mission complete. Review your dive report.");
            GameAudio.PlayMissionSuccess();
            return true;
        }

        public void ShowCredits()
        {
            SetState(MissionState.Credits);
            SetObjective("Thanks for diving. Restart or Quit when ready.");
        }

        // ───────── Pause ─────────

        public void Pause()
        {
            if (state == MissionState.Paused || state == MissionState.Boot || state == MissionState.ModeSelect)
                return;

            stateBeforePause = state;
            SetState(MissionState.Paused);
            Time.timeScale = 0f;
        }

        public void Resume()
        {
            if (state != MissionState.Paused)
                return;

            Time.timeScale = 1f;
            SetState(stateBeforePause);
        }

        // ───────── Restart ─────────

        public void RestartMission()
        {
            Time.timeScale = 1f;
            ResetSession("restart");
            MissionEvents.RaiseMissionRestarted();
            SetState(MissionState.ModeSelect);
            SetObjective("Choose VR Headset / Simulator or Desktop Keyboard & Mouse.");
        }

        public void QuitApplication()
        {
            Time.timeScale = 1f;
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        // ───────── Internal helpers ─────────

        void ResetSession(string reason)
        {
            diveLog.schemaVersion = 2;
            diveLog.diveId = Guid.NewGuid().ToString("N");
            diveLog.startedAtUtc = DateTime.UtcNow.ToString("o");
            diveLog.completedAtUtc = null;
#pragma warning disable CS0618
            diveLog.waterSampleCollected = false;
#pragma warning restore CS0618
            diveLog.buoyRestored = false;
            diveLog.visitedZones.Clear();
            diveLog.observations.Clear();
            diveLog.scannedAnimalIds.Clear();
            diveLog.coralScans.Clear();
            diveLog.perSiteSamples.Clear();
            diveLog.rubbishCollected.Clear();
            diveLog.hazardsFlagged.Clear();
            diveLog.recommendedSiteId = null;
            diveLog.recommendationReason = null;
            diveLog.markerPlaced = false;
            diveLog.markerSiteId = null;
            diveLog.completedObjectives.Clear();
            tutorialFlags.Clear();
            siteProgress.Clear();
            HasScanner = false;
            HasBottle = false;
            BottleReturned = false;
            BuoyRestored = false;
            lastComparison = null;
            lastSavedPath = null;
            playMode = PlayModeType.Unselected;
            Debug.Log($"[ReefRescue] Session reset ({reason}).");
        }

        void CompleteObjective(string id)
        {
            if (!diveLog.completedObjectives.Contains(id))
                diveLog.completedObjectives.Add(id);
        }

        public bool TryDisposeToxin(string hazardId, string siteId, string hazardType)
        {
            if (string.IsNullOrEmpty(hazardId))
                return false;

            if (diveLog.hazardsFlagged.Exists(h =>
                    string.Equals(h.hazardId, hazardId, StringComparison.OrdinalIgnoreCase)))
            {
                MissionEvents.RaiseFeedback("This toxin is already disposed.");
                return false;
            }

            diveLog.hazardsFlagged.Add(new HazardRecord
            {
                hazardId = hazardId,
                siteId = siteId,
                hazardType = hazardType,
                timeSeconds = Time.timeSinceLevelLoad
            });

            if (!string.IsNullOrEmpty(siteId))
            {
                var progress = GetOrCreateSiteProgress(siteId);
                progress.hazardFlagged = true;
            }

            MissionEvents.RaiseHazardFlagged(hazardId);
            MissionEvents.RaiseFeedback("Toxin disposed.");
            CheckSurveyProgress();
            return true;
        }

        static bool SiteHasCoralPoint(string siteId)
        {
            foreach (var point in FindObjectsByType<ReefExplorer.Interaction.CoralSurveyPoint>(FindObjectsSortMode.None))
            {
                if (point != null && point.gameObject.activeInHierarchy &&
                    string.Equals(point.SiteId, siteId, StringComparison.OrdinalIgnoreCase))
                    return true;
            }

            return false;
        }

        public bool SiteHasSampleZone(string siteId)
        {
            foreach (var zone in FindObjectsByType<ReefExplorer.Interaction.SampleZone>(FindObjectsSortMode.None))
            {
                if (zone != null && zone.gameObject.activeInHierarchy &&
                    string.Equals(zone.SiteId, siteId, StringComparison.OrdinalIgnoreCase))
                    return true;
            }

            return false;
        }

        public bool HasAllWaterSamples()
        {
            var need = 0;
            var got = 0;
            foreach (var zone in FindObjectsByType<ReefExplorer.Interaction.SampleZone>(FindObjectsSortMode.None))
            {
                if (zone == null || !zone.gameObject.activeInHierarchy)
                    continue;
                need++;
                if (diveLog.perSiteSamples.Exists(s =>
                        s != null && s.collected &&
                        string.Equals(s.siteId, zone.SiteId, StringComparison.OrdinalIgnoreCase)))
                    got++;
            }

            return need > 0 && got >= need;
        }

        bool HasAllRequiredSpecies()
        {
            if (requiredSpecies == null || requiredSpecies.Length == 0)
                return false;

            foreach (var species in requiredSpecies)
            {
                if (species == null)
                    return false;

                var found = diveLog.observations.Exists(o =>
                    o != null &&
                    string.Equals(o.speciesId, species.SpeciesId, StringComparison.OrdinalIgnoreCase));
                if (!found)
                    return false;
            }

            return true;
        }

        bool HasAllZones()
        {
            if (requiredZones == null || requiredZones.Length == 0)
                return false;

            foreach (var zone in requiredZones)
            {
                if (!diveLog.visitedZones.Exists(z =>
                        string.Equals(z, zone, StringComparison.OrdinalIgnoreCase)))
                    return false;
            }

            return true;
        }

        bool HasSufficientEvidence()
        {
            // Need at least coral scans and animal observations for all sites
            if (sites == null || sites.Length == 0)
                return HasAllRequiredSpecies() && HasAllZones();

            foreach (var site in sites)
            {
                if (site == null) continue;
                var progress = GetOrCreateSiteProgress(site.SiteId);
                if (SiteHasCoralPoint(site.SiteId) && !progress.coralScanned)
                    return false;
                if (!progress.animalScanned)
                    return false;
            }

            return true;
        }

        void CheckSurveyProgress()
        {
            if (state != MissionState.SurveyAnimals &&
                state != MissionState.CollectSample &&
                state != MissionState.CollectRubbish)
                return;

            // Check if all sites have been fully surveyed
            bool allSurveyed = true;
            if (sites != null && sites.Length > 0)
            {
                foreach (var site in sites)
                {
                    if (site == null) continue;
                    var progress = GetOrCreateSiteProgress(site.SiteId);
                    if ((SiteHasCoralPoint(site.SiteId) && !progress.coralScanned) ||
                        !progress.animalScanned ||
                        (SiteHasSampleZone(site.SiteId) && !progress.sampleCollected))
                    {
                        allSurveyed = false;
                        break;
                    }
                }
            }
            else
            {
                allSurveyed = HasAllRequiredSpecies() && HasAllZones();
            }

            if (allSurveyed)
            {
                SetState(MissionState.ReturnToStation);
                SetObjective("All sites surveyed. Return to the research station with your samples.");
            }
            else
            {
                // Update objective with remaining tasks
                UpdateSurveyObjective();
            }
        }

        void UpdateSurveyObjective()
        {
            if (sites == null || sites.Length == 0) return;

            var remaining = new List<string>();
            foreach (var site in sites)
            {
                if (site == null) continue;
                var progress = GetOrCreateSiteProgress(site.SiteId);
                var tasks = new List<string>();
                if (SiteHasCoralPoint(site.SiteId) && !progress.coralScanned) tasks.Add("coral");
                if (!progress.animalScanned) tasks.Add("animal");
                if (SiteHasSampleZone(site.SiteId) && !progress.sampleCollected) tasks.Add("sample");
                if (tasks.Count > 0)
                    remaining.Add($"{site.DisplayName}: {string.Join(", ", tasks)}");
            }

            if (remaining.Count > 0)
            {
                SetObjective("Survey the reef areas. Pick up rubbish as you go.\n" +
                             "Remaining: " + string.Join(" | ", remaining));
            }
        }

        string BuildIncompleteMessage()
        {
            if (!BuoyRestored)
                return "Restore the monitoring station (insert the power cell) before submitting.";
            if (!HasAllRequiredSpecies())
                return "Scan all required animals before submitting.";
            if (!HasAllZones())
                return "Visit all three survey zones before submitting.";
            if (string.IsNullOrEmpty(diveLog.recommendedSiteId))
                return "Choose a restoration site recommendation before submitting.";
            if (!diveLog.markerPlaced)
                return "Place the research marker at the recommended site before submitting.";
            return "Mission incomplete.";
        }

        void BuildComparison()
        {
            lastComparison = SurveyAnalyzer.Compare(diveLog, baselineSurvey, requiredSpecies, requiredZones);

            // Build per-site comparison data
            lastComparison.siteComparisons.Clear();
            if (sites != null)
            {
                foreach (var site in sites)
                {
                    if (site == null) continue;
                    var progress = GetOrCreateSiteProgress(site.SiteId);
                    var rubbishForSite = diveLog.rubbishCollected.Count(r =>
                        string.Equals(r.siteId, site.SiteId, StringComparison.OrdinalIgnoreCase));
                    var hazardForSite = diveLog.hazardsFlagged.Exists(h =>
                        string.Equals(h.siteId, site.SiteId, StringComparison.OrdinalIgnoreCase));

                    var sampleRecord = diveLog.perSiteSamples.Find(s =>
                        string.Equals(s.siteId, site.SiteId, StringComparison.OrdinalIgnoreCase));

                    lastComparison.siteComparisons.Add(new SiteComparisonData
                    {
                        siteId = site.SiteId,
                        displayName = site.DisplayName,
                        baselineCoralCondition = site.BaselineCoralCondition,
                        currentCoralCondition = LatestCoralCondition(site.SiteId) ?? site.CurrentCoralCondition,
                        waterClarity = site.WaterReadings.waterClarity,
                        temperature = site.WaterReadings.temperatureCelsius,
                        temperatureSuitability = site.WaterReadings.temperatureSuitability,
                        currentStrength = site.WaterReadings.currentStrength,
                        initialRubbish = site.InitialRubbishCount,
                        rubbishRemoved = rubbishForSite,
                        rubbishRemaining = Mathf.Max(0, site.InitialRubbishCount - rubbishForSite),
                        hasHazard = site.HasHazard,
                        hazardType = site.HazardType,
                        hazardFlagged = hazardForSite,
                        animalObserved = progress.animalScanned ? site.TargetAnimal?.DisplayName : "",
                        animalNote = progress.animalScanned ? site.AnimalObservationNote : "",
                        coralScanned = progress.coralScanned,
                        sampleCollected = sampleRecord?.collected ?? false,
                        sampleAnalysed = sampleRecord?.analysed ?? false,
                        suitabilityScore = site.SuitabilityScore,
                        suitabilityReason = site.SuitableForRestoration
                            ? site.SuitabilityReason
                            : site.UnsuitableReason,
                        observationComplete = progress.coralScanned && progress.animalScanned &&
                                              (sampleRecord?.analysed ?? false)
                    });
                }
            }

            lastComparison.recommendedSiteId = diveLog.recommendedSiteId;
            lastComparison.recommendationExplanation = diveLog.recommendationReason;
            lastComparison.totalRubbishCollected = diveLog.rubbishCollected.Count;
            lastComparison.totalHazardsFlagged = diveLog.hazardsFlagged.Count;
        }

        SiteDefinition GetSite(string siteId)
        {
            if (sites == null || string.IsNullOrEmpty(siteId))
                return null;

            foreach (var site in sites)
            {
                if (site != null && string.Equals(site.SiteId, siteId, StringComparison.OrdinalIgnoreCase))
                    return site;
            }

            return null;
        }

        string GetSiteIdForZone(string zoneId)
        {
            if (sites == null || string.IsNullOrEmpty(zoneId))
                return "";

            foreach (var site in sites)
            {
                if (site != null && string.Equals(site.ZoneId, zoneId, StringComparison.OrdinalIgnoreCase))
                    return site.SiteId;
            }

            return "";
        }

        SiteProgress GetOrCreateSiteProgress(string siteId)
        {
            if (siteProgress.TryGetValue(siteId, out var progress))
                return progress;

            progress = new SiteProgress();
            siteProgress[siteId] = progress;
            return progress;
        }

        public SiteProgress GetSiteProgress(string siteId)
        {
            return GetOrCreateSiteProgress(siteId);
        }

        void SetState(MissionState next)
        {
            if (state == next)
                return;

            var previous = state;
            state = next;
            MissionEvents.RaiseStateChanged(previous, next);
        }

        void SetObjective(string text) => MissionEvents.RaiseObjective(text);
    }

    /// <summary>
    /// Tracks per-site survey progress at runtime (not serialized to the save file directly).
    /// </summary>
    public sealed class SiteProgress
    {
        public bool coralScanned;
        public bool animalScanned;
        public bool sampleCollected;
        public bool sampleAnalysed;
        public bool hazardFlagged;
    }
}
