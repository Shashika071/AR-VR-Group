using System.Linq;
using System.Text;
using ReefExplorer.Core;
using ReefExplorer.Survey;
using UnityEngine;
using UnityEngine.UI;

namespace ReefExplorer.UI
{
    public sealed class ComparisonBoardUI : MonoBehaviour
    {
        [SerializeField] GameObject panel;
        [SerializeField] Text comparisonText;
        [SerializeField] Button recommendCoralButton;
        [SerializeField] Button recommendSeagrassButton;
        [SerializeField] Button recommendSandButton;

        void Awake()
        {
            if (panel != null)
                panel.SetActive(false);

            Wire(recommendCoralButton, () => TryRecommend("site_coral"));
            Wire(recommendSeagrassButton, () => TryRecommend("site_seagrass"));
            Wire(recommendSandButton, () => TryRecommend("site_sand"));
        }

        void OnEnable()
        {
            MissionEvents.StateChanged += OnState;
            MissionEvents.WaterSampleAnalysed += OnEvidenceChanged;
            MissionEvents.CoralScanned += OnEvidenceChanged;
            MissionEvents.RubbishCollected += OnRubbish;
            MissionEvents.HazardFlagged += OnEvidenceChanged;
        }

        void OnDisable()
        {
            MissionEvents.StateChanged -= OnState;
            MissionEvents.WaterSampleAnalysed -= OnEvidenceChanged;
            MissionEvents.CoralScanned -= OnEvidenceChanged;
            MissionEvents.RubbishCollected -= OnRubbish;
            MissionEvents.HazardFlagged -= OnEvidenceChanged;
        }

        void OnState(MissionState _, MissionState next)
        {
            if (panel != null)
            {
                var show = next >= MissionState.AnalyseSamples && next <= MissionState.SubmitLog;
                panel.SetActive(show);
                if (show) RefreshBoard();
            }

            var canRecommend = next == MissionState.CompareAndChoose;
            if (recommendCoralButton != null) recommendCoralButton.gameObject.SetActive(canRecommend);
            if (recommendSeagrassButton != null) recommendSeagrassButton.gameObject.SetActive(canRecommend);
            if (recommendSandButton != null) recommendSandButton.gameObject.SetActive(canRecommend);
        }

        void OnEvidenceChanged(string _) => RefreshBoard();
        void OnRubbish(string _) => RefreshBoard();

        void TryRecommend(string siteId)
        {
            if (MissionController.Instance != null)
                MissionController.Instance.TryRecommendSite(siteId);
        }

        void RefreshBoard()
        {
            if (comparisonText == null || MissionController.Instance == null)
                return;

            var mc = MissionController.Instance;
            if (mc.Sites == null || mc.Sites.Count == 0)
                return;

            var sb = new StringBuilder();
            sb.AppendLine("<b>RESTORATION SITE COMPARISON</b>\n");

            foreach (var site in mc.Sites)
            {
                if (site == null) continue;
                
                var progress = mc.GetSiteProgress(site.SiteId);
                var log = mc.DiveLog;
                
                sb.AppendLine($"<b>{site.DisplayName.ToUpper()}</b>");
                
                // Observations
                sb.Append("Coral: ");
                var scannedCondition = mc.LatestCoralCondition(site.SiteId);
                sb.AppendLine(progress.coralScanned
                    ? (string.IsNullOrEmpty(scannedCondition) ? site.CurrentCoralCondition : scannedCondition)
                    : "<i>Not scanned yet</i>");
                
                sb.Append("Baseline: ");
                sb.AppendLine(mc.BuoyRestored ? site.BaselineCoralCondition : "<i>[Offline]</i>");
                
                sb.Append("Animal: ");
                sb.AppendLine(progress.animalScanned ? site.AnimalObservationNote : "<i>[Pending scan]</i>");
                
                // Samples
                var sample = log.perSiteSamples.Find(s => s.siteId == site.SiteId);
                sb.Append("Water: ");
                if (sample != null && sample.analysed)
                    sb.AppendLine($"{site.WaterReadings.waterClarity} | {site.WaterReadings.temperatureCelsius:F1}°C | pH {site.WaterReadings.pH:F1}");
                else if (sample != null && sample.collected)
                    sb.AppendLine("<i>[Sample collected, pending analysis]</i>");
                else
                    sb.AppendLine("<i>[Pending collection]</i>");
                    
                // Rubbish and Hazards
                var rubbish = log.rubbishCollected.Count(r => r.siteId == site.SiteId);
                sb.AppendLine($"Rubbish Removed: {rubbish}/{site.InitialRubbishCount}");
                
                if (site.HasHazard)
                {
                    var flagged = log.hazardsFlagged.Exists(h => h.siteId == site.SiteId);
                    sb.AppendLine($"Hazard: {(flagged ? $"<color=orange>{site.HazardType} (Flagged)</color>" : "<i>[Pending flag]</i>")}");
                }
                else
                {
                    sb.AppendLine("Hazard: None detected");
                }
                
                sb.AppendLine();
            }

            comparisonText.text = sb.ToString();
        }
        
        static void Wire(Button button, UnityEngine.Events.UnityAction action)
        {
            if (button == null)
                return;
            button.onClick.RemoveListener(action);
            button.onClick.AddListener(action);
        }
    }
}
