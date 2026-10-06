using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace ReefExplorer.Interaction
{
    /// <summary>
    /// Keeps mission tools resting on the station console at start so they do not fall under the table.
    /// </summary>
    public sealed class PlaceToolsOnTable : MonoBehaviour
    {
        [SerializeField] Vector3 buoyPos = new Vector3(0.55f, 1.08f, -1.7f);
        [SerializeField] Vector3 scannerPos = new Vector3(0.05f, 1.1f, -1.7f);
        [SerializeField] Vector3 bottlePos = new Vector3(-0.45f, 1.2f, -1.7f);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            if (FindAnyObjectByType<PlaceToolsOnTable>() != null)
                return;
            if (GameObject.Find("ResearchStation") == null)
                return;

            var go = new GameObject("PlaceToolsOnTable");
            go.AddComponent<PlaceToolsOnTable>();
        }

        void Start()
        {
            // Positions only — stylized visuals set their own scale.
            Place("PracticeBuoy", buoyPos);
            Place("Scanner", scannerPos);
            Place("SampleBottle", bottlePos);
        }

        void Place(string name, Vector3 worldPos)
        {
            var go = GameObject.Find(name);
            if (go == null)
                return;

            go.transform.position = worldPos;

            var body = go.GetComponent<Rigidbody>();
            if (body != null)
            {
                body.useGravity = true;
                RigidbodyUtil.ParkKinematic(body);
            }

            var grab = go.GetComponent<XRGrabInteractable>();
            if (grab != null)
            {
                grab.selectEntered.RemoveListener(OnGrabbed);
                grab.selectEntered.AddListener(OnGrabbed);
            }

            // Update respawn anchor if present.
            var respawn = go.GetComponent<ToolRespawn>();
            if (respawn != null)
            {
                var soPos = worldPos;
                // ToolRespawn creates its own anchor in Awake; move object is enough for first place.
            }
        }

        void OnGrabbed(SelectEnterEventArgs args)
        {
            var body = args.interactableObject.transform.GetComponent<Rigidbody>();
            if (body == null)
                return;

            // Desktop sets kinematic itself; for XR Instantaneous also uses kinematic while held.
            // After release, allow physics again.
            var grab = args.interactableObject.transform.GetComponent<XRGrabInteractable>();
            if (grab != null)
            {
                grab.selectExited.RemoveListener(OnReleased);
                grab.selectExited.AddListener(OnReleased);
            }
        }

        void OnReleased(SelectExitEventArgs args)
        {
            var body = args.interactableObject.transform.GetComponent<Rigidbody>();
            if (body == null)
                return;

            body.isKinematic = false;
            body.useGravity = true;
        }
    }
}
