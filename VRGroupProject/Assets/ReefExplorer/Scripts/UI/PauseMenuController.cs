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

        void Awake()
        {
            if (panel != null)
                panel.SetActive(false);

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
        }
    }
}
