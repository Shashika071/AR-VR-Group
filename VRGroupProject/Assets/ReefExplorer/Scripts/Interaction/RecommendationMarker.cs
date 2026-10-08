using ReefExplorer.Core;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit;

namespace ReefExplorer.Interaction
{
    [RequireComponent(typeof(XRGrabInteractable))]
    public sealed class RecommendationMarker : MonoBehaviour
    {
        XRGrabInteractable grab;

        void Awake()
        {
            grab = GetComponent<XRGrabInteractable>();
        }

        void OnEnable()
        {
            grab.selectEntered.AddListener(OnGrabbed);
        }

        void OnDisable()
        {
            grab.selectEntered.RemoveListener(OnGrabbed);
        }

        void OnGrabbed(SelectEnterEventArgs args)
        {
            if (MissionController.Instance == null) return;
            
            // Only allow grabbing if a site has been recommended
            if (string.IsNullOrEmpty(MissionController.Instance.RecommendedSiteId))
            {
                MissionEvents.RaiseFeedback("Choose a restoration site recommendation first before taking the marker.");
                if (args.interactorObject is UnityEngine.XR.Interaction.Toolkit.Interactors.XRBaseInteractor baseInteractor)
                    baseInteractor.interactionManager.SelectCancel((UnityEngine.XR.Interaction.Toolkit.Interactors.IXRSelectInteractor)baseInteractor, (UnityEngine.XR.Interaction.Toolkit.Interactables.IXRSelectInteractable)grab);
            }
            else
            {
                var site = MissionController.Instance.Sites.Count > 0 
                    ? MissionController.Instance.RecommendedSiteId 
                    : "";
                MissionEvents.RaiseFeedback($"Take this marker to the selected site ({site}) and place it in the holder.");
            }
        }
    }
}
