using ReefExplorer.Audio;
using ReefExplorer.Core;
using ReefExplorer.Environment;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

namespace ReefExplorer.Interaction
{
    /// <summary>
    /// Accepts only PowerCell objects. Validates correct object for buoy restore.
    /// </summary>
    public sealed class BuoyPowerSocket : MonoBehaviour
    {
        [SerializeField] XRSocketInteractor socket;
        [SerializeField] Transform snapPoint;
        [SerializeField] MonitoringBuoy buoy;
        [SerializeField] float proximityRadius = 0.45f;

        void Awake()
        {
            if (socket == null)
                socket = GetComponent<XRSocketInteractor>();
            if (snapPoint == null)
                snapPoint = transform;
            if (buoy == null)
                buoy = GetComponentInParent<MonitoringBuoy>();
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
            if (MissionController.Instance == null || MissionController.Instance.BuoyRestored)
                return;

            var cells = FindObjectsByType<PowerCell>();
            foreach (var cell in cells)
            {
                if (cell == null || cell.IsHeld)
                    continue;
                if (Vector3.Distance(cell.transform.position, snapPoint.position) <= proximityRadius)
                    Accept(cell);
            }
        }

        void OnSocketSelect(SelectEnterEventArgs args)
        {
            var cell = args.interactableObject.transform.GetComponent<PowerCell>();
            if (cell == null)
            {
                MissionEvents.RaiseFeedback("Wrong object. Insert the loose power cell.");
                GameAudio.PlayInvalid(transform.position);
                return;
            }

            Accept(cell);
        }

        void Accept(PowerCell cell)
        {
            if (MissionController.Instance == null)
                return;

            if (!MissionController.Instance.TryRestoreBuoy())
            {
                GameAudio.PlayInvalid(transform.position);
                return;
            }

            cell.transform.SetPositionAndRotation(snapPoint.position, snapPoint.rotation);
            RigidbodyUtil.ParkKinematic(cell.GetComponent<Rigidbody>());
            if (buoy != null)
                buoy.SetRestored(true);

            MissionEvents.RaiseFeedback(
                "Signal restored. We have the previous survey. Now let’s complete today’s observations. — Maya");
        }
    }
}
