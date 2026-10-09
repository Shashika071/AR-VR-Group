using ReefExplorer.Core;
using UnityEngine;

namespace ReefExplorer.Interaction
{
    [RequireComponent(typeof(Collider))]
    public sealed class HazardFlag : MonoBehaviour, IScannable
    {
        [SerializeField] string hazardId = "hazard_net_01";
        [SerializeField] string siteId = "site_coral";
        [SerializeField] string hazardType = "Fishing Net";
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

            var ok = MissionController.Instance.TryFlagHazard(hazardId, siteId, hazardType);
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
                        mat.SetColor("_BaseColor", Color.Lerp(mat.GetColor("_BaseColor"), Color.red, 0.5f));
                    }
                }
            }
        }
    }
}
