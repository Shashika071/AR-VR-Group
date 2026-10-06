using ReefExplorer.Core;
using UnityEngine;

namespace ReefExplorer.Interaction
{
    [RequireComponent(typeof(Collider))]
    public sealed class ZoneTrigger : MonoBehaviour
    {
        [SerializeField] string zoneId = "zone_coral";
        [SerializeField] bool isStation;

        void Reset()
        {
            GetComponent<Collider>().isTrigger = true;
        }

        void OnTriggerEnter(Collider other)
        {
            if (!IsPlayer(other))
                return;

            if (isStation)
                MissionController.Instance?.NotifyReturnedToStation();
            else
                MissionController.Instance?.NotifyZoneEntered(zoneId);
        }

        static bool IsPlayer(Collider other)
        {
            return other.CompareTag("Player") ||
                   other.GetComponentInParent<Unity.XR.CoreUtils.XROrigin>() != null ||
                   other.GetComponentInParent<ReefExplorer.Input.DesktopPlayerController>() != null;
        }
    }
}
