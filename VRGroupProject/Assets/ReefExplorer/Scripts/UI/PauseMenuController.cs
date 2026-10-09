using ReefExplorer.Audio;
using ReefExplorer.Core;
using UnityEngine;
using UnityEngine.UI;

namespace ReefExplorer.UI
{
    public sealed class PauseMenuController : MonoBehaviour
    {
        [SerializeField] GameObject panel;
        [SerializeField] Button resumeButton;
        [SerializeField] Button restartButton;
        [SerializeField] Button quitButton;
        [SerializeField] Slider ambienceSlider;
        [SerializeField] Slider effectsSlider;
        [SerializeField] Toggle muteToggle;
        [SerializeField] GameAudioHub audioHub;

        Transform backdrop;

        void Awake()
        {
            if (panel != null)
                panel.SetActive(false);

            CreateBackdrop();

            if (resumeButton != null)
                resumeButton.onClick.AddListener(() => MissionController.Instance?.Resume());
            if (restartButton != null)
                restartButton.onClick.AddListener(() => MissionController.Instance?.RestartMission());
            if (quitButton != null)
                quitButton.onClick.AddListener(() => MissionController.Instance?.QuitApplication());

            if (ambienceSlider != null)
                ambienceSlider.onValueChanged.AddListener(v => audioHub?.SetAmbienceVolume(v));
            if (effectsSlider != null)
                effectsSlider.onValueChanged.AddListener(v => audioHub?.SetEffectsVolume(v));
            if (muteToggle != null)
                muteToggle.onValueChanged.AddListener(m => audioHub?.SetMuted(m));
        }

        void OnEnable() => MissionEvents.StateChanged += OnState;
        void OnDisable() => MissionEvents.StateChanged -= OnState;

        void OnState(MissionState _, MissionState next)
        {
            if (panel != null)
                panel.SetActive(next == MissionState.Paused);
            if (backdrop != null)
                backdrop.gameObject.SetActive(next == MissionState.Paused);

            if (next == MissionState.Paused)
            {
                Cursor.visible = true;
                Cursor.lockState = CursorLockMode.None;
            }
        }

        void CreateBackdrop()
        {
            if (panel == null || panel.transform.parent == null)
                return;

            var parent = panel.transform.parent;
            backdrop = parent.Find("PauseBackdrop");
            if (backdrop == null)
            {
                var backdropObject = new GameObject("PauseBackdrop", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                backdrop = backdropObject.transform;
                backdrop.SetParent(parent, false);
                backdrop.SetAsFirstSibling();
            }

            var rect = backdrop as RectTransform;
            if (rect != null)
            {
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.one;
                rect.offsetMin = Vector2.zero;
                rect.offsetMax = Vector2.zero;
            }

            var image = backdrop.GetComponent<Image>();
            if (image != null)
            {
                image.color = new Color(0.015f, 0.035f, 0.055f, 0.82f);
                image.raycastTarget = true;
            }

            backdrop.gameObject.SetActive(false);
        }
    }
}
