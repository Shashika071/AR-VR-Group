using ReefExplorer.Audio;
using ReefExplorer.Core;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace ReefExplorer.Interaction
{
    [RequireComponent(typeof(XRGrabInteractable))]
    public sealed class RubbishItem : MonoBehaviour
    {
        [SerializeField] string rubbishId = "rubbish_01";
        [SerializeField] string siteId = "site_coral";

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
            if (MissionController.Instance == null)
                return;

            var ok = MissionController.Instance.TryCollectRubbish(rubbishId, siteId);
            if (ok)
            {
                GameAudio.PlayRubbishCollect(transform.position);
                
                // Force drop so it doesn't stay stuck to the hand
                if (args.interactorObject is UnityEngine.XR.Interaction.Toolkit.Interactors.XRBaseInteractor baseInteractor)
                    baseInteractor.interactionManager.SelectCancel((UnityEngine.XR.Interaction.Toolkit.Interactors.IXRSelectInteractor)baseInteractor, (UnityEngine.XR.Interaction.Toolkit.Interactables.IXRSelectInteractable)grab);

                // Hide and disable instead of destroy to allow restarts to restore it if needed
                gameObject.SetActive(false);
            }
            else
            {
                // Force drop if they already picked it up somehow
                if (args.interactorObject is UnityEngine.XR.Interaction.Toolkit.Interactors.XRBaseInteractor baseInteractor2)
                    baseInteractor2.interactionManager.SelectCancel((UnityEngine.XR.Interaction.Toolkit.Interactors.IXRSelectInteractor)baseInteractor2, (UnityEngine.XR.Interaction.Toolkit.Interactables.IXRSelectInteractable)grab);
            }
        }
    }
}
