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

        public string SiteId => siteId;

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

            RubbishBin.ShowHint(true);
            MissionEvents.RaiseFeedback("Carry this rubbish to the bin at the station.");
        }

        public bool CollectNow()
        {
            if (MissionController.Instance == null)
                return false;
            if (!MissionController.Instance.TryCollectRubbish(rubbishId, siteId))
                return false;
            GameAudio.PlayRubbishCollect(transform.position);
            gameObject.SetActive(false);
            return true;
        }
    }
}
