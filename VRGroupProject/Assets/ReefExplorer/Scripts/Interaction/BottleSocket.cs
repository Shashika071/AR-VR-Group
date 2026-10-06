using ReefExplorer.Audio;
using ReefExplorer.Core;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

namespace ReefExplorer.Interaction
{
    /// <summary>
    /// Accepts only the filled sample bottle. Works with XR Socket or proximity+release.
    /// </summary>
    public sealed class BottleSocket : MonoBehaviour
    {
        [SerializeField] XRSocketInteractor socket;
        [SerializeField] Transform snapPoint;
        [SerializeField] float proximityRadius = 0.35f;

        void Awake()
        {
            if (socket == null)
                socket = GetComponent<XRSocketInteractor>();
            if (snapPoint == null)
                snapPoint = transform;
        }

        void OnEnable()
        {
            if (socket != null)
                socket.selectEntered.AddListener(OnSocketSelect);
        }

        void OnDisable()
        {
            if (socket != null)
                socket.selectEntered.RemoveListener(OnSocketSelect);
        }

        void Update()
        {
            // Desktop / fallback: if a filled bottle is released nearby, accept it.
            if (MissionController.Instance == null || MissionController.Instance.BottleReturned)
                return;

            var bottles = FindObjectsByType<SampleBottle>();
            foreach (var bottle in bottles)
            {
                if (bottle == null || bottle.IsHeld || !bottle.IsFilled)
                    continue;

                if (Vector3.Distance(bottle.transform.position, snapPoint.position) <= proximityRadius)
                    Accept(bottle);
            }
        }

        void OnSocketSelect(SelectEnterEventArgs args)
        {
            var bottle = args.interactableObject.transform.GetComponent<SampleBottle>();
            if (bottle == null)
            {
                MissionEvents.RaiseFeedback("Wrong object for this holder.");
                return;
            }

            Accept(bottle);
        }

        void Accept(SampleBottle bottle)
        {
            if (MissionController.Instance == null)
                return;

            var ok = MissionController.Instance.TryAcceptReturnedBottle(bottle.IsFilled);
            if (!ok)
            {
                GameAudio.PlayInvalid(transform.position);
                return;
            }

            bottle.transform.SetPositionAndRotation(snapPoint.position, snapPoint.rotation);
            RigidbodyUtil.ParkKinematic(bottle.GetComponent<Rigidbody>());

            GameAudio.PlayObjectiveComplete(transform.position);
        }
    }
}
