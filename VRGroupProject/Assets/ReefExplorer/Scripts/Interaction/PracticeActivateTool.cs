using ReefExplorer.Core;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace ReefExplorer.Interaction
{
    [RequireComponent(typeof(XRGrabInteractable))]
    public sealed class PracticeActivateTool : MonoBehaviour
    {
        XRGrabInteractable grab;

        void Awake() => grab = GetComponent<XRGrabInteractable>();

        void OnEnable()
        {
            grab.selectEntered.AddListener(OnSelect);
            grab.activated.AddListener(OnActivate);
        }

        void OnDisable()
        {
            grab.selectEntered.RemoveListener(OnSelect);
            grab.activated.RemoveListener(OnActivate);
        }

        void OnSelect(SelectEnterEventArgs _) =>
            MissionController.Instance?.NotifyTutorialStep("grab");

        void OnActivate(ActivateEventArgs _) =>
            MissionController.Instance?.NotifyTutorialStep("activate");
    }
}
