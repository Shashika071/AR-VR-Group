using System;
using System.Collections.Generic;
using ReefExplorer.Survey;
using UnityEngine;

namespace ReefExplorer.Core
{
    /// <summary>
    /// Central mission state machine for Reef Rescue — The Silent Signal.
    /// </summary>
    public sealed class MissionController : MonoBehaviour
    {
        public static MissionController Instance { get; private set; }

        [Header("Survey Data")]
        [SerializeField] BaselineSurveyData baselineSurvey;
        [SerializeField] SpeciesDefinition[] requiredSpecies = Array.Empty<SpeciesDefinition>();
        [SerializeField] string[] requiredZones = { "zone_coral", "zone_turtle", "zone_ray" };

        [Header("Runtime")]
        [SerializeField] MissionState state = MissionState.Boot;
        [SerializeField] PlayModeType playMode = PlayModeType.Unselected;

        readonly DiveLogData diveLog = new DiveLogData();
        readonly HashSet<string> tutorialFlags = new HashSet<string>();
        MissionState stateBeforePause = MissionState.Briefing;
        SurveyComparisonResult lastComparison;
        string lastSavedPath;

        public MissionState State => state;
        public PlayModeType PlayMode => playMode;
        public DiveLogData DiveLog => diveLog;
        public SurveyComparisonResult LastComparison => lastComparison;
        public string LastSavedPath => lastSavedPath;
        public BaselineSurveyData BaselineSurvey => baselineSurvey;
        public IReadOnlyList<SpeciesDefinition> RequiredSpecies => requiredSpecies;
        public IReadOnlyList<string> RequiredZones => requiredZones;

        public bool HasScanner { get; private set; }
        public bool HasBottle { get; private set; }
        public bool BuoyRestored { get; private set; }
        public bool BaselineUnlocked => BuoyRestored;
        public bool BottleFilled => diveLog.waterSampleCollected;
        public bool BottleReturned { get; private set; }

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

        public void SelectPlayMode(PlayModeType mode)
        {
            if (mode == PlayModeType.Unselected)
                return;

            playMode = mode;

            // Only jump to Briefing from pre-dive states — never rewind a live dive
            // if Desktop gets re-selected from movement input.
            if (state is MissionState.Boot or MissionState.ModeSelect or MissionState.Briefing)
            {
                SetState(MissionState.Briefing);
                SetObjective("Read the mission board, then press Start Dive.");
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

        public void NotifyTutorialStep(string stepId)
        {
            if (string.IsNullOrEmpty(stepId) || tutorialFlags.Contains(stepId))
                return;

            tutorialFlags.Add(stepId);

            switch (state)
            {
                case MissionState.TutorialMove when stepId == "move":
                    SetState(MissionState.TutorialGrab);
                    SetObjective(playMode == PlayModeType.Desktop
                        ? "Tutorial: pick up the practice tool (E / Left Click), then drop it (Q)."
                        : "Tutorial: pick up the practice tool (G while aiming), then release grip.");
                    break;
                case MissionState.TutorialGrab when stepId == "grab":
                    SetState(MissionState.TutorialActivate);
                    SetObjective(playMode == PlayModeType.Desktop
                        ? "Tutorial: hold the practice tool and activate (Left Click)."
                        : "Tutorial: hold the practice tool and activate (Trigger / Mouse Click).");
                    break;
                case MissionState.TutorialActivate when stepId == "activate":
                    SetState(MissionState.GatherTools);
                    SetObjective("Pick up the handheld scanner and the sample bottle from the console.");
                    break;
            }
        }

        public void NotifyToolPicked(string toolId)
        {
            if (toolId == "scanner")
                HasScanner = true;
            if (toolId == "bottle")
                HasBottle = true;

            if (state == MissionState.GatherTools && HasScanner && HasBottle)
            {
                SetState(MissionState.RepairBuoy);
                SetObjective("Follow the path to Reef Buoy Seven. Pick up the loose power cell and insert it into the buoy socket.");
            }
        }

        public bool TryRestoreBuoy()
        {
            if (BuoyRestored)
            {
                MissionEvents.RaiseFeedback("Buoy already restored.");
                return false;
            }

            if (state != MissionState.RepairBuoy &&
                state != MissionState.GatherTools &&
                state != MissionState.SurveyAnimals)
            {
                MissionEvents.RaiseFeedback("Finish gathering your tools, then restore the buoy.");
                return false;
            }

            BuoyRestored = true;
            diveLog.buoyRestored = true;
            MissionEvents.RaiseBuoyRestored();
            SetState(MissionState.SurveyAnimals);
            SetObjective("Baseline unlocked. Visit Coral Garden, Seagrass Crossing and Sandy Passage. Scan clownfish, turtle and ray.");
            return true;
        }

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

        public bool TryRecordAnimalScan(string animalInstanceId, SpeciesDefinition species, string zoneId)
        {
            if (species == null || string.IsNullOrEmpty(animalInstanceId))
                return false;

            if (!BuoyRestored)
            {
                MissionEvents.RaiseFeedback("Restore Reef Buoy Seven first so the previous survey unlocks.");
                return false;
            }

            if (diveLog.scannedAnimalIds.Contains(animalInstanceId))
            {
                MissionEvents.RaiseFeedback("Already scanned. Find a different required animal.");
                return false;
            }

            var observation = new SpeciesObservation
            {
                observationId = Guid.NewGuid().ToString("N"),
                speciesId = species.SpeciesId,
                speciesDisplayName = species.DisplayName,
                zoneId = zoneId,
                timeSeconds = Time.timeSinceLevelLoad
            };

            diveLog.scannedAnimalIds.Add(animalInstanceId);
            diveLog.observations.Add(observation);
            MissionEvents.RaiseAnimalScanned(observation);

            var fact = string.IsNullOrWhiteSpace(species.Description)
                ? $"Logged {species.DisplayName}."
                : $"Logged {species.DisplayName}. Fact: {species.Description}";
            MissionEvents.RaiseFeedback(fact);

            if (state == MissionState.SurveyAnimals && HasAllRequiredSpecies())
            {
                SetState(MissionState.CollectSample);
                SetObjective("Go to the marked sampling point and fill the sample bottle.");
            }

            return true;
        }

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

            if (diveLog.waterSampleCollected)
            {
                MissionEvents.RaiseFeedback("Bottle already filled.");
                return false;
            }

            diveLog.waterSampleCollected = true;
            MissionEvents.RaiseSampleCollected();
            SetState(MissionState.ReturnToStation);
            SetObjective("Return to the research station with your filled bottle.");
            return true;
        }

        public void NotifyReturnedToStation()
        {
            if (state == MissionState.ReturnToStation && diveLog.waterSampleCollected)
            {
                SetState(MissionState.ReturnBottle);
                SetObjective("Place the filled bottle into the return holder.");
            }
        }

        public bool TryAcceptReturnedBottle(bool isFilledBottle)
        {
            if (!isFilledBottle)
            {
                MissionEvents.RaiseFeedback("Only the filled sample bottle fits this holder.");
                return false;
            }

            if (!diveLog.waterSampleCollected)
            {
                MissionEvents.RaiseFeedback("Fill the bottle at the sampling point first.");
                return false;
            }

            BottleReturned = true;
            MissionEvents.RaiseBottleReturned();
            SetState(MissionState.SubmitLog);
            SetObjective("Review your observations, then Submit Log on the mission board.");
            return true;
        }

        public bool CanSubmit()
        {
            return BuoyRestored &&
                   BottleReturned &&
                   diveLog.waterSampleCollected &&
                   HasAllRequiredSpecies() &&
                   HasAllZones();
        }

        public bool TrySubmit()
        {
            if (!CanSubmit())
            {
                MissionEvents.RaiseFeedback(BuildIncompleteMessage());
                return false;
            }

            diveLog.completedAtUtc = DateTime.UtcNow.ToString("o");
            lastComparison = SurveyAnalyzer.Compare(diveLog, baselineSurvey, requiredSpecies, requiredZones);
            lastSavedPath = DiveLogSaver.Save(diveLog, lastComparison);
            MissionEvents.RaiseSurveySubmitted();
            SetState(MissionState.Results);
            SetObjective("First mission complete. Review the survey comparison, then Credits.");
            return true;
        }

        public void ShowCredits()
        {
            SetState(MissionState.Credits);
            SetObjective("Thanks for diving. Restart or Quit when ready.");
        }

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

        void ResetSession(string reason)
        {
            diveLog.diveId = Guid.NewGuid().ToString("N");
            diveLog.startedAtUtc = DateTime.UtcNow.ToString("o");
            diveLog.completedAtUtc = null;
            diveLog.waterSampleCollected = false;
            diveLog.buoyRestored = false;
            diveLog.visitedZones.Clear();
            diveLog.observations.Clear();
            diveLog.scannedAnimalIds.Clear();
            tutorialFlags.Clear();
            HasScanner = false;
            HasBottle = false;
            BottleReturned = false;
            BuoyRestored = false;
            lastComparison = null;
            lastSavedPath = null;
            playMode = PlayModeType.Unselected;
            Debug.Log($"[ReefRescue] Session reset ({reason}).");
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

        string BuildIncompleteMessage()
        {
            if (!BuoyRestored)
                return "Restore Reef Buoy Seven (insert the power cell) before submitting.";
            if (!BottleReturned)
                return "Return the filled bottle to its holder before submitting.";
            if (!diveLog.waterSampleCollected)
                return "Water sample is missing.";
            if (!HasAllRequiredSpecies())
                return "Scan all required animals before submitting.";
            if (!HasAllZones())
                return "Visit all three survey zones before submitting.";
            return "Mission incomplete.";
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
}
