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

            MissionEvents.RaiseFeedback("Use the disposal tool on the green cloud.");
            return false;
        }

        public bool TryDispose()
        {
            if (scanned || MissionController.Instance == null)
                return false;

            var ok = MissionController.Instance.TryDisposeToxin(hazardId, siteId, hazardType);
            if (!ok)
                return false;

            scanned = true;
            SetVisible(false);
            return true;
        }

        public void ResetScanned()
        {
            scanned = false;
            SetVisible(true);
        }

        public void SetVisible(bool visible)
        {
            foreach (var renderer in GetComponentsInChildren<Renderer>(true))
            {
                if (renderer != null)
                    renderer.enabled = visible;
            }

            foreach (var collider in GetComponentsInChildren<Collider>(true))
            {
                if (collider != null)
                    collider.enabled = visible;
            }
        }
    }
}
