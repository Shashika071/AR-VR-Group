using ReefExplorer.Core;
using ReefExplorer.Input;
using UnityEngine;

namespace ReefExplorer.Environment
{
    /// <summary>
    /// If the view is empty / controls dead (XR selected with no headset, player fallen, etc.),
    /// force Desktop mode and put the player back at the station facing the board.
    /// </summary>
    public sealed class DesktopGameplayRecovery : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            if (GameObject.Find("ResearchStation") == null)
                return;
            if (FindAnyObjectByType<DesktopGameplayRecovery>() != null)
                return;

            var host = new GameObject("DesktopGameplayRecovery");
            host.AddComponent<DesktopGameplayRecovery>();
        }

        void Update()
        {
            // XR / Device Simulator owns R (translate vs rotate). Never reset the player in that mode,
            // or the demo snaps back to the desktop camera and hides the XR Origin.
            if (MissionController.Instance != null &&
                MissionController.Instance.PlayMode == PlayModeType.XR)
                return;

            var desktop = GameObject.Find("DesktopPlayer");
            if (desktop == null)
                return;

            // Fell through world → soft land on sand instead of always hard-resetting to start.
            var p = desktop.transform.position;
            if (p.y < -1f)
            {
                SoftLand(desktop, p);
            }
            else if (p.y > 40f)
            {
                Recover();
            }
        }

        static void SoftLand(GameObject desktop, Vector3 p)
        {
            var cc = desktop.GetComponent<CharacterController>();
            if (cc != null)
                cc.enabled = false;

            // Keep XZ where they were exploring; put feet back on the seabed.
            desktop.transform.position = new Vector3(
                Mathf.Clamp(p.x, -18f, 18f),
                0.1f,
                Mathf.Clamp(p.z, -2f, 26f));

            if (cc != null)
                cc.enabled = true;
        }

        static void Recover()
        {
            // Destroy leftover jellyfish / craft that can block the camera.
            DestroyNamed("Ambient_Jellyfish");

            var desktop = GameObject.Find("DesktopPlayer");
            if (desktop == null)
                return;

            var xr = GameObject.Find("XR Origin (XR Rig)");
            if (xr != null)
                xr.SetActive(false);

            desktop.SetActive(true);

            var cc = desktop.GetComponent<CharacterController>();
            if (cc != null)
            {
                cc.enabled = false; // allow teleport without CC fighting
                desktop.transform.SetPositionAndRotation(
                    new Vector3(0f, 0f, 0.5f),
                    Quaternion.Euler(0f, 180f, 0f));
                cc.enabled = true;
            }
            else
            {
                desktop.transform.SetPositionAndRotation(
                    new Vector3(0f, 0f, 0.5f),
                    Quaternion.Euler(0f, 180f, 0f));
            }

            var cam = desktop.GetComponentInChildren<Camera>(true);
            if (cam != null)
            {
                cam.enabled = true;
                cam.tag = "MainCamera";
                cam.transform.localPosition = new Vector3(0f, 1.5f, 0f);
                cam.transform.localRotation = Quaternion.identity;
            }

            var mode = Object.FindAnyObjectByType<PlayerModeSelector>();
            mode?.ChooseDesktop(faceBoard: false);

            if (MissionController.Instance != null &&
                MissionController.Instance.PlayMode != PlayModeType.Desktop)
            {
                MissionController.Instance.SelectPlayMode(PlayModeType.Desktop);
            }

            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            Debug.Log("[ReefExplorer] Desktop recovery: player at station. WASD move, Right-click look.");
        }

        static void DestroyNamed(string name)
        {
            var go = GameObject.Find(name);
            if (go != null)
                Object.Destroy(go);
        }
    }
}
