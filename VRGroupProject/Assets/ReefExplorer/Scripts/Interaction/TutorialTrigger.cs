using ReefExplorer.Core;
using UnityEngine;

namespace ReefExplorer.Interaction
{
    [RequireComponent(typeof(Collider))]
    public sealed class TutorialTrigger : MonoBehaviour
    {
        [SerializeField] string stepId = "move";

        void Reset() => GetComponent<Collider>().isTrigger = true;

        void OnTriggerEnter(Collider other)
        {
            if (other.GetComponentInParent<Unity.XR.CoreUtils.XROrigin>() == null &&
                other.GetComponentInParent<ReefExplorer.Input.DesktopPlayerController>() == null)
                return;

            MissionController.Instance?.NotifyTutorialStep(stepId);
        }
    }
}
