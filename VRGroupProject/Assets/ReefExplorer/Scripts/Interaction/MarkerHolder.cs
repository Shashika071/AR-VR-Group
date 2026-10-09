using ReefExplorer.Audio;
using ReefExplorer.Core;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

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

        public static bool TryPlaceNearest(Vector3 from, RecommendationMarker marker)
        {
            if (marker == null)
                return false;

            MarkerHolder best = null;
            var bestDist = 3.4f;
            foreach (var holder in FindObjectsByType<MarkerHolder>(FindObjectsSortMode.None))
            {
                if (holder == null || !holder.gameObject.activeInHierarchy)
                    continue;
                var dist = Vector3.Distance(from, holder.transform.position);
                if (dist < bestDist)
                {
                    bestDist = dist;
                    best = holder;
                }
            }

            return best != null && best.Plant(marker);
        }

        bool Plant(RecommendationMarker marker)
        {
            var mc = MissionController.Instance;
            if (mc == null)
                return false;
            if (string.IsNullOrEmpty(mc.RecommendedSiteId) && !mc.TryRecommendSite(siteId))
                return false;
            if (!mc.TryPlaceMarker(siteId))
                return false;

            GameAudio.PlayMarkerPlace(transform.position);
            if (socket != null)
                socket.socketActive = false;
            marker.transform.SetParent(transform, true);
            marker.transform.position = transform.position + Vector3.up * 0.35f;
            marker.transform.rotation = Quaternion.identity;
            var grab = marker.GetComponent<XRGrabInteractable>();
            if (grab != null)
                grab.enabled = false;
            return true;
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
