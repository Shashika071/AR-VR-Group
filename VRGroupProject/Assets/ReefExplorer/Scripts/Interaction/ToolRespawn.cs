using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace ReefExplorer.Interaction
{
    /// <summary>
    /// Returns dropped tools to a safe spawn point if they fall out of the playable area.
    /// </summary>
    [RequireComponent(typeof(XRGrabInteractable))]
    public sealed class ToolRespawn : MonoBehaviour
    {
        [SerializeField] Transform respawnPoint;
        [SerializeField] float minY = -2f;
        [SerializeField] float maxDistanceFromOrigin = 80f;
        [SerializeField] float checkInterval = 0.5f;

        XRGrabInteractable grab;
        Rigidbody body;
        float nextCheck;

        void Awake()
        {
            grab = GetComponent<XRGrabInteractable>();
            body = GetComponent<Rigidbody>();
            if (respawnPoint == null)
            {
                var anchor = new GameObject($"{name}_RespawnAnchor");
                anchor.transform.SetPositionAndRotation(transform.position, transform.rotation);
                respawnPoint = anchor.transform;
            }
        }

        void OnEnable()
        {
            grab.selectExited.AddListener(OnReleased);
        }

        void OnDisable()
        {
            grab.selectExited.RemoveListener(OnReleased);
        }

        void Update()
        {
            if (Time.unscaledTime < nextCheck)
                return;

            nextCheck = Time.unscaledTime + checkInterval;
            if (grab.isSelected)
                return;

            if (transform.position.y < minY ||
                transform.position.sqrMagnitude > maxDistanceFromOrigin * maxDistanceFromOrigin)
            {
                Respawn();
            }
        }

        void OnReleased(SelectExitEventArgs _)
        {
            // Delay one frame so throw velocity can settle before evaluating.
            nextCheck = Time.unscaledTime + 0.2f;
        }

        public void Respawn()
        {
            if (grab.isSelected)
                return;

            transform.SetPositionAndRotation(respawnPoint.position, respawnPoint.rotation);
            RigidbodyUtil.ParkKinematic(body);
        }
    }
}
