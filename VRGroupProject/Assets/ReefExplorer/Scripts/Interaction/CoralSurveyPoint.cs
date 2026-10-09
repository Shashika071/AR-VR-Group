using ReefExplorer.Core;
using UnityEngine;

namespace ReefExplorer.Interaction
{
    [RequireComponent(typeof(Collider))]
    public sealed class CoralSurveyPoint : MonoBehaviour, IScannable
    {
        [SerializeField] string siteId = "site_coral";
        [SerializeField] bool scanned;
        [SerializeField] Renderer[] tintRenderers;

        public bool IsScanned => scanned;

        void OnEnable() => MissionEvents.MissionRestarted += ResetScanned;
        void OnDisable() => MissionEvents.MissionRestarted -= ResetScanned;

        void Reset()
        {
            var col = GetComponent<Collider>();
            if (col != null)
                col.isTrigger = false;
        }

        public bool TryScan()
        {
            if (scanned || MissionController.Instance == null)
                return false;

            var ok = MissionController.Instance.TryRecordCoralScan(siteId);
            if (!ok)
                return false;

            scanned = true;
            ApplyScannedVisual();
            return true;
        }

        public void ResetScanned()
        {
            scanned = false;
            ApplyScannedVisual();
        }

        void ApplyScannedVisual()
        {
            if (tintRenderers == null)
                return;

            foreach (var renderer in tintRenderers)
            {
                if (renderer == null)
                    continue;

                foreach (var mat in renderer.materials)
                {
                    if (mat.HasProperty("_BaseColor"))
                    {
                        var c = mat.GetColor("_BaseColor");
                        c.a = scanned ? 0.65f : 1f;
                        mat.SetColor("_BaseColor", scanned ? Color.Lerp(c, Color.white, 0.35f) : c);
                    }
                }
            }
        }
    }
}
