using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.UI;

namespace ReefExplorer.UI
{
    /// <summary>
    /// Mounts MissionCanvas flat on the station board so it is never half-clipped
    /// or mirrored. Does not billboard the board (that was clipping into MissionBoard).
    /// </summary>
    public sealed class WorldUiClarityFix : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Apply()
        {
            if (GameObject.Find("ResearchStation") == null)
                return;

            var canvasGo = GameObject.Find("MissionCanvas");
            if (canvasGo == null)
                return;

            // Billboard on the board yaws the plane into MissionBoard → half UI missing.
            var face = canvasGo.GetComponent<FaceCameraLabel>();
            if (face != null)
                Object.Destroy(face);

            var s = Mathf.Abs(canvasGo.transform.localScale.x);
            if (s < 0.001f)
                s = 0.0022f;
            canvasGo.transform.localScale = Vector3.one * s;

            // Fixed mount on the back wall. Y=180 = readable World Space UI toward the station.
            canvasGo.transform.position = new Vector3(0f, 1.75f, -2.88f);
            canvasGo.transform.rotation = Quaternion.Euler(0f, 180f, 0f);

            // Keep MissionBoard as a dark frame, but push it behind the canvas.
            var board = GameObject.Find("MissionBoard");
            if (board != null)
            {
                board.transform.position = new Vector3(0f, 1.75f, -3.05f);
                var col = board.GetComponent<Collider>();
                if (col != null)
                    col.enabled = false;
            }

            var canvas = canvasGo.GetComponent<Canvas>();
            if (canvas != null && Camera.main != null)
                canvas.worldCamera = Camera.main;

            var tracked = canvasGo.GetComponent<TrackedDeviceGraphicRaycaster>();
            if (tracked != null)
                tracked.enabled = false;
        }
    }
}
