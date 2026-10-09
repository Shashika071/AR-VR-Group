using ReefExplorer.Core;
using ReefExplorer.Survey;
using UnityEngine;

namespace ReefExplorer.Interaction
{
    [RequireComponent(typeof(Collider))]
    public sealed class SurveyAnimal : MonoBehaviour, IScannable
    {
        [SerializeField] string animalInstanceId;
        [SerializeField] SpeciesDefinition species;
        [SerializeField] string zoneId = "zone_coral";
        [SerializeField] bool scanned;
        [SerializeField] Renderer[] tintRenderers;

        public string AnimalInstanceId =>
            string.IsNullOrEmpty(animalInstanceId) ? gameObject.name : animalInstanceId;

        public SpeciesDefinition Species => species;
        public string ZoneId => zoneId;
        public bool IsScanned => scanned;
        public bool Scanned => scanned; // legacy

        void OnEnable() => MissionEvents.MissionRestarted += ResetScanned;
        void OnDisable() => MissionEvents.MissionRestarted -= ResetScanned;

        void Reset()
        {
            animalInstanceId = gameObject.name;
            var col = GetComponent<Collider>();
            if (col != null)
                col.isTrigger = false;
        }

        public bool TryMarkScanned() => TryScan();

        public bool TryScan()
        {
            if (scanned || species == null || MissionController.Instance == null)
                return false;

            var ok = MissionController.Instance.TryRecordAnimalScan(AnimalInstanceId, species, zoneId);
            if (!ok)
                return false;

            scanned = true;
            ApplyScannedVisual();
            return true;
        }

        public void ResetForRestart() => ResetScanned();

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
