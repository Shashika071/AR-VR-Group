using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit.UI;

namespace ReefExplorer.UI
{
    /// <summary>
    /// Fixes mirrored / hard-to-read world-space mission board text at runtime.
    /// </summary>
    public sealed class WorldUiClarityFix : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Apply()
        {
            var canvasGo = GameObject.Find("MissionCanvas");
            if (canvasGo == null)
                return;

            // Board is on the station wall at -Z; rotate so text faces the player.
            canvasGo.transform.rotation = Quaternion.Euler(0f, 180f, 0f);
            if (canvasGo.transform.localScale.x < 0.0018f)
                canvasGo.transform.localScale = Vector3.one * 0.002f;

            var canvas = canvasGo.GetComponent<Canvas>();
            if (canvas != null && Camera.main != null)
                canvas.worldCamera = Camera.main;

            // Default to mouse UI; PlayerModeSelector re-enables tracked raycaster for XR.
            var tracked = canvasGo.GetComponent<TrackedDeviceGraphicRaycaster>();
            if (tracked != null)
                tracked.enabled = false;

            foreach (var text in canvasGo.GetComponentsInChildren<Text>(true))
            {
                text.color = Color.white;
                text.horizontalOverflow = HorizontalWrapMode.Wrap;
                text.verticalOverflow = VerticalWrapMode.Overflow;
                if (text.fontSize < 22)
                    text.fontSize = 22;
            }

            foreach (var image in canvasGo.GetComponentsInChildren<Image>(true))
            {
                if (image.gameObject.name == "Panel")
                    image.color = new Color(0.03f, 0.12f, 0.18f, 0.96f);
            }
        }
    }
}
