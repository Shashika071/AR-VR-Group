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
        GameObject controlsCard;

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

            AddControlsButton();
        }

        void AddControlsButton()
        {
            if (panel == null || panel.transform.Find("ControlsButton") != null)
                return;

            var panelRect = panel.GetComponent<RectTransform>();
            if (panelRect != null)
            {
                panelRect.sizeDelta += new Vector2(0f, 64f);
                panelRect.anchoredPosition += new Vector2(0f, 32f);
            }

            var buttonGo = new GameObject("ControlsButton", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            buttonGo.transform.SetParent(panel.transform, false);
            var buttonRect = buttonGo.GetComponent<RectTransform>();
            buttonRect.sizeDelta = new Vector2(200f, 48f);
            buttonRect.anchoredPosition = new Vector2(0f, -panelRect.sizeDelta.y * 0.5f + 36f);
            var image = buttonGo.GetComponent<Image>();
            image.color = new Color(0.1f, 0.45f, 0.55f, 1f);
            var button = buttonGo.GetComponent<Button>();
            button.onClick.AddListener(ShowControls);

            var labelGo = new GameObject("Label", typeof(RectTransform));
            labelGo.transform.SetParent(buttonGo.transform, false);
            var label = labelGo.AddComponent<Text>();
            label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            label.fontSize = 20;
            label.fontStyle = FontStyle.Bold;
            label.alignment = TextAnchor.MiddleCenter;
            label.color = Color.white;
            label.text = "Controls";
            var labelRect = label.rectTransform;
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;

            controlsCard = BuildControlsCard();
            controlsCard.SetActive(false);
        }

        GameObject BuildControlsCard()
        {
            var parent = panel.transform.parent;
            var card = new GameObject("ControlsCard", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            card.transform.SetParent(parent, false);
            var rect = card.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(560f, 280f);
            rect.anchoredPosition = Vector2.zero;
            card.GetComponent<Image>().color = new Color(0.05f, 0.1f, 0.14f, 0.96f);

            var bodyGo = new GameObject("Body", typeof(RectTransform));
            bodyGo.transform.SetParent(card.transform, false);
            var body = bodyGo.AddComponent<Text>();
            body.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            body.fontSize = 22;
            body.alignment = TextAnchor.UpperCenter;
            body.color = Color.white;
            body.horizontalOverflow = HorizontalWrapMode.Wrap;
            body.text =
                "CONTROLS\n\n" +
                "Desktop:  Right Mouse look    E grab    Left Click scan\n" +
                "VR:  Space + mouse aim    G grab    Click activate\n\n" +
                "Esc closes this box";
            var bodyRect = body.rectTransform;
            bodyRect.sizeDelta = new Vector2(520f, 180f);
            bodyRect.anchoredPosition = new Vector2(0f, 30f);

            var closeGo = new GameObject("Close", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            closeGo.transform.SetParent(card.transform, false);
            var closeRect = closeGo.GetComponent<RectTransform>();
            closeRect.sizeDelta = new Vector2(160f, 42f);
            closeRect.anchoredPosition = new Vector2(0f, -100f);
            closeGo.GetComponent<Image>().color = new Color(0.1f, 0.45f, 0.55f, 1f);
            closeGo.GetComponent<Button>().onClick.AddListener(HideControls);
            var closeLabelGo = new GameObject("Label", typeof(RectTransform));
            closeLabelGo.transform.SetParent(closeGo.transform, false);
            var closeLabel = closeLabelGo.AddComponent<Text>();
            closeLabel.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            closeLabel.fontSize = 20;
            closeLabel.alignment = TextAnchor.MiddleCenter;
            closeLabel.color = Color.white;
            closeLabel.text = "Back";
            var closeLabelRect = closeLabel.rectTransform;
            closeLabelRect.anchorMin = Vector2.zero;
            closeLabelRect.anchorMax = Vector2.one;
            closeLabelRect.offsetMin = Vector2.zero;
            closeLabelRect.offsetMax = Vector2.zero;
            return card;
        }

        void ShowControls()
        {
            if (controlsCard != null)
                controlsCard.SetActive(true);
        }

        void HideControls()
        {
            if (controlsCard != null)
                controlsCard.SetActive(false);
        }

        void OnEnable() => MissionEvents.StateChanged += OnState;
        void OnDisable() => MissionEvents.StateChanged -= OnState;

        public void RefreshPauseVisual()
        {
            if (MissionController.Instance == null)
                return;
            OnState(MissionController.Instance.State, MissionController.Instance.State);
        }

        void OnState(MissionState _, MissionState next)
        {
            var showMenu = next == MissionState.Paused && !ReefMinimap.ShowingBigMap;
            if (panel != null)
                panel.SetActive(showMenu);
            if (!showMenu)
                HideControls();
            if (backdrop != null)
                backdrop.gameObject.SetActive(showMenu);

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
                image.color = new Color(0.015f, 0.035f, 0.055f, 0.35f);
                image.raycastTarget = true;
            }

            backdrop.gameObject.SetActive(false);
        }
    }
}
