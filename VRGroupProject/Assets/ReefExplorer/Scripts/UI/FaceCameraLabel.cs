using UnityEngine;

namespace ReefExplorer.UI
{
    /// <summary>
    /// Billboard for world TextMesh labels so text stays readable (never mirrored).
    /// MissionCanvas is handled separately by WorldUiClarityFix (fixed mount, not billboarded).
    /// </summary>
    public sealed class FaceCameraLabel : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void AttachAll()
        {
            if (GameObject.Find("ResearchStation") == null)
                return;

            foreach (var tm in Object.FindObjectsByType<TextMesh>())
            {
                if (tm == null)
                    continue;
                if (tm.GetComponent<FaceCameraLabel>() == null)
                    tm.gameObject.AddComponent<FaceCameraLabel>();
            }
        }

        void LateUpdate()
        {
            var cam = Camera.main;
            if (cam == null)
                return;

            // Same formula as working tool labels (BillboardLabel):
            // TextMesh +Z points away from camera → front faces the viewer.
            var away = transform.position - cam.transform.position;
            if (away.sqrMagnitude < 0.0001f)
                return;

            transform.rotation = Quaternion.LookRotation(away.normalized, Vector3.up);
        }
    }
}
