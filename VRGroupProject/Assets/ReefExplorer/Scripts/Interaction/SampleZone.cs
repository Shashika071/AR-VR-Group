using ReefExplorer.Audio;
using ReefExplorer.Core;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ReefExplorer.Interaction
{
    [RequireComponent(typeof(Collider))]
    public sealed class SampleZone : MonoBehaviour
    {
        [SerializeField] string siteId = "site_coral";
        [SerializeField] string prompt = "Hold bottle here and press E / Trigger to fill.";
        [SerializeField] Key desktopFillKey = Key.E;

        SampleBottle bottleInZone;

        void Reset()
        {
            var col = GetComponent<Collider>();
            col.isTrigger = true;
        }

        void OnTriggerEnter(Collider other)
        {
            var bottle = other.GetComponentInParent<SampleBottle>();
            if (bottle == null)
                return;

            bottleInZone = bottle;
            if (!string.IsNullOrEmpty(prompt))
                MissionEvents.RaiseFeedback(prompt);
        }

        void OnTriggerExit(Collider other)
        {
            var bottle = other.GetComponentInParent<SampleBottle>();
            if (bottle != null && bottle == bottleInZone)
                bottleInZone = null;
        }

        void Update()
        {
            if (MissionController.Instance == null)
                return;

            var desktopPressed = Keyboard.current != null && Keyboard.current[desktopFillKey].wasPressedThisFrame;
            if (!desktopPressed)
                return;

            TryFill();
        }

        public bool TryFill()
        {
            var ok = MissionController.Instance != null &&
                     MissionController.Instance.TryCollectSample(siteId, bottleInZone != null, true);

            if (ok)
            {
                bottleInZone?.MarkFilled();
                GameAudio.PlaySampleFill(transform.position);
            }
            else
            {
                GameAudio.PlayInvalid(transform.position);
            }

            return ok;
        }

        void OnDrawGizmos()
        {
            Gizmos.color = new Color(0.2f, 0.7f, 1f, 0.35f);
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawCube(Vector3.zero, Vector3.one);
        }
    }
}
