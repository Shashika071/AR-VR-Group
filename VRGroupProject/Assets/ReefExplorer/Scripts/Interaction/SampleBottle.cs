using ReefExplorer.Core;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace ReefExplorer.Interaction
{
    [RequireComponent(typeof(XRGrabInteractable))]
    public sealed class SampleBottle : MonoBehaviour
    {
        [SerializeField] string toolId = "bottle";
        [SerializeField] Renderer liquidRenderer;
        [SerializeField] Color emptyColor = new Color(0.7f, 0.85f, 0.95f, 0.25f);
        [SerializeField] Color filledColor = new Color(0.15f, 0.55f, 0.75f, 0.85f);

        XRGrabInteractable grab;
        bool filled;

        public bool IsFilled => filled;
        public bool IsHeld => grab != null && grab.isSelected;

        void Awake()
        {
            grab = GetComponent<XRGrabInteractable>();
            ApplyVisual();
        }

        void OnEnable()
        {
            grab.selectEntered.AddListener(OnGrabbed);
            MissionEvents.MissionRestarted += OnRestart;
            MissionEvents.SampleCollected += OnSampleCollected;
        }

        void OnDisable()
        {
            grab.selectEntered.RemoveListener(OnGrabbed);
            MissionEvents.MissionRestarted -= OnRestart;
            MissionEvents.SampleCollected -= OnSampleCollected;
        }

        void OnGrabbed(SelectEnterEventArgs _)
        {
            MissionController.Instance?.NotifyToolPicked(toolId);
        }

        void OnSampleCollected()
        {
            filled = true;
            ApplyVisual();
        }

        void OnRestart()
        {
            filled = false;
            ApplyVisual();
        }

        public void MarkFilled()
        {
            filled = true;
            ApplyVisual();
        }

        void ApplyVisual()
        {
            if (liquidRenderer == null)
                return;

            var mat = liquidRenderer.material;
            if (mat.HasProperty("_BaseColor"))
                mat.SetColor("_BaseColor", filled ? filledColor : emptyColor);
            else if (mat.HasProperty("_Color"))
                mat.SetColor("_Color", filled ? filledColor : emptyColor);
        }
    }
}
