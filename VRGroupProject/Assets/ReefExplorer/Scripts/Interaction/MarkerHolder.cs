using ReefExplorer.Audio;
using ReefExplorer.Core;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

namespace ReefExplorer.Interaction
{
    public sealed class MarkerHolder : MonoBehaviour
    {
        [SerializeField] string siteId = "site_coral";
        [SerializeField] UnityEngine.XR.Interaction.Toolkit.Interactors.XRSocketInteractor socket;

        void Awake()
        {
            if (socket == null)
                socket = GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactors.XRSocketInteractor>();
        }

        void OnEnable()
        {
            if (socket != null)
                socket.selectEntered.AddListener(OnMarkerPlaced);
        }

        void OnDisable()
        {
            if (socket != null)
                socket.selectEntered.RemoveListener(OnMarkerPlaced);
        }

        void OnMarkerPlaced(SelectEnterEventArgs args)
        {
            var marker = args.interactableObject.transform.GetComponent<RecommendationMarker>();
            if (marker == null) return;

            var mc = MissionController.Instance;
            if (mc == null) return;

            if (mc.TryPlaceMarker(siteId))
            {
                GameAudio.PlayMarkerPlace(transform.position);
                socket.socketActive = false; // lock it in
            }
            else
            {
                // Force drop if incorrect site or premature
                if (args.interactorObject is UnityEngine.XR.Interaction.Toolkit.Interactors.XRBaseInteractor baseInteractor)
                    baseInteractor.interactionManager.SelectCancel((UnityEngine.XR.Interaction.Toolkit.Interactors.IXRSelectInteractor)baseInteractor, marker.GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.IXRSelectInteractable>());
            }
        }
    }
}
