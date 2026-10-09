using ReefExplorer.Core;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace ReefExplorer.Interaction
{
    /// <summary>
    /// Removable buoy power cell — grab and place into BuoyPowerSocket.
    /// </summary>
    [RequireComponent(typeof(XRGrabInteractable))]
    public sealed class PowerCell : MonoBehaviour
    {
        [SerializeField] XRGrabInteractable grab;
        public bool IsHeld { get; private set; }

        void Awake()
        {
            if (grab == null)
                grab = GetComponent<XRGrabInteractable>();
        }

        void OnEnable()
        {
            if (grab == null)
                return;
            grab.selectEntered.AddListener(OnGrab);
            grab.selectExited.AddListener(OnDrop);
        }

        void OnDisable()
        {
            if (grab == null)
                return;
            grab.selectEntered.RemoveListener(OnGrab);
            grab.selectExited.RemoveListener(OnDrop);
        }

        void OnGrab(SelectEnterEventArgs _)
        {
            IsHeld = true;
            BuoyPowerSocket.ShowPlaceHint(true);
            MissionEvents.RaiseFeedback("Buoy battery picked up.");
        }

        void OnDrop(SelectExitEventArgs _)
        {
            IsHeld = false;
            BuoyPowerSocket.ShowPlaceHint(false);
        }

        public void MarkHeldDesktop(bool held)
        {
            IsHeld = held;
            BuoyPowerSocket.ShowPlaceHint(held);
            if (held)
                MissionEvents.RaiseFeedback("Buoy battery picked up.");
        }
    }
}
