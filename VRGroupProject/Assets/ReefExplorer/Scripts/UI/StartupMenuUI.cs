using ReefExplorer.Core;
using ReefExplorer.Input;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit.UI;

namespace ReefExplorer.UI
{
    /// <summary>
    /// Screen menu with reliable mouse clicks + keyboard shortcuts.
    /// Hides cleanly after the dive starts so it cannot get stuck open.
    /// </summary>
    public sealed class StartupMenuUI : MonoBehaviour
    {
        [SerializeField] GameObject panelRoot;
        [SerializeField] GameObject hudRoot;
        [SerializeField] Text statusText;
        [SerializeField] Text objectiveHudText;

        PlayerModeSelector modeSelector;
        bool diveStarted;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void EnsureExists()
        {
            if (FindAnyObjectByType<MissionController>() == null)
                return;
            if (FindAnyObjectByType<StartupMenuUI>() != null)
                return;

            // World MissionCanvas is the briefing board. This component only adds a thin screen HUD
            // so we do not duplicate overlapping menus.
            var go = new GameObject("StartupMenuUI");
            go.AddComponent<StartupMenuUI>();
        }

        void Awake()
        {
            modeSelector = FindAnyObjectByType<PlayerModeSelector>();
            EnsureEventSystemForMouse();

            // Prefer the scene-built MissionHudCanvas; avoid a second objective bar.
            var hasSceneHud = GameObject.Find("MissionHudCanvas") != null;
            if (!hasSceneHud && panelRoot == null)
                BuildHudOnly();

            MissionEvents.StateChanged += OnStateChanged;
            if (!hasSceneHud)
            {
                MissionEvents.ObjectiveChanged += OnObjective;
                MissionEvents.FeedbackRequested += OnFeedback;
            }

            MissionEvents.MissionRestarted += OnRestart;

            diveStarted = false;
            if (panelRoot != null)
                panelRoot.SetActive(false);
            if (hudRoot != null)
                hudRoot.SetActive(false);
        }

        void OnDestroy()
        {
            MissionEvents.StateChanged -= OnStateChanged;
            MissionEvents.ObjectiveChanged -= OnObjective;
            MissionEvents.FeedbackRequested -= OnFeedback;
            MissionEvents.MissionRestarted -= OnRestart;
        }

        void Update()
        {
            if (Keyboard.current == null)
                return;

            // Do NOT bind D here — D is move-right in DesktopPlayerController.
            // Mode is chosen with the board buttons (or auto on Start Dive / first WASD).
            var state = MissionController.Instance != null
                ? MissionController.Instance.State
                : MissionState.ModeSelect;
            var onBriefing = state is MissionState.Boot or MissionState.ModeSelect or MissionState.Briefing;

            if (onBriefing)
            {
                diveStarted = false;
                if (Keyboard.current.vKey.wasPressedThisFrame)
                    ChooseXr();
                if (Keyboard.current.enterKey.wasPressedThisFrame || Keyboard.current.numpadEnterKey.wasPressedThisFrame)
                    StartDive();
                return;
            }

            diveStarted = true;

            if (Keyboard.current.hKey.wasPressedThisFrame && hudRoot != null)
                hudRoot.SetActive(!hudRoot.activeSelf);
        }

        void OnStateChanged(MissionState _, MissionState next)
        {
            if (next == MissionState.ModeSelect || next == MissionState.Boot || next == MissionState.Briefing)
            {
                diveStarted = false;
                if (panelRoot != null)
                    panelRoot.SetActive(false);
                if (hudRoot != null)
                    hudRoot.SetActive(false);
                return;
            }

            diveStarted = true;
            HideMenuShowHud();
        }

        void OnRestart()
        {
            diveStarted = false;
            if (panelRoot != null)
                panelRoot.SetActive(false);
            if (hudRoot != null)
                hudRoot.SetActive(false);
        }

        void OnObjective(string text)
        {
            if (objectiveHudText != null)
                objectiveHudText.text = "OBJECTIVE: " + text;
        }

        void OnFeedback(string text)
        {
            if (objectiveHudText != null && diveStarted)
                objectiveHudText.text = "OBJECTIVE update: " + text;
        }

        void ShowMenu()
        {
            diveStarted = false;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            if (panelRoot != null)
                panelRoot.SetActive(true);
            if (hudRoot != null)
                hudRoot.SetActive(false);
            EnsureEventSystemForMouse();
        }

        void HideMenuShowHud()
        {
            diveStarted = true;
            if (panelRoot != null)
                panelRoot.SetActive(false);
            if (hudRoot != null)
                hudRoot.SetActive(true);
        }

        void BuildHudOnly()
        {
            var canvasGo = new GameObject("StartupCanvas");
            canvasGo.transform.SetParent(transform, false);
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 400;
            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            canvasGo.AddComponent<GraphicRaycaster>();

            // No full-screen briefing panel (world MissionCanvas owns that).
            panelRoot = null;

            hudRoot = CreatePanel(canvasGo.transform, "ObjectiveHud", new Vector2(920f, 64f),
                new Color(0.02f, 0.08f, 0.12f, 0.88f));
            var hudRt = hudRoot.GetComponent<RectTransform>();
            hudRt.anchoredPosition = new Vector2(0f, -16f);
            hudRt.anchorMin = new Vector2(0.5f, 1f);
            hudRt.anchorMax = new Vector2(0.5f, 1f);
            hudRt.pivot = new Vector2(0.5f, 1f);
            objectiveHudText = CreateLabel(hudRoot.transform, "ObjectiveText",
                "OBJECTIVE: ...", 20, Vector2.zero, new Vector2(880f, 52f));
            hudRoot.SetActive(false);
        }

        void ChooseDesktop()
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            modeSelector ??= FindAnyObjectByType<PlayerModeSelector>();
            modeSelector?.ChooseDesktop();
            SetStatus("Desktop selected. Now click Start Dive or press Enter.");
        }

        void ChooseXr()
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            modeSelector ??= FindAnyObjectByType<PlayerModeSelector>();
            modeSelector?.ChooseXr();
            SetStatus("VR selected. Now click Start Dive or press Enter.");
        }

        void StartDive()
        {
            if (MissionController.Instance == null)
            {
                SetStatus("Mission controller missing.");
                return;
            }

            if (MissionController.Instance.PlayMode == PlayModeType.Unselected)
            {
                // Auto desktop so Start is never "stuck"
                ChooseDesktop();
            }

            if (MissionController.Instance.PlayMode == PlayModeType.Unselected)
            {
                SetStatus("Choose Desktop on the board first.");
                return;
            }

            MissionController.Instance.StartDive();
            HideMenuShowHud();
            SetStatus("Dive started.");
            if (objectiveHudText != null)
            {
                objectiveHudText.text =
                    "OBJECTIVE: Move with WASD. Space to jump. Look with Right Mouse. Grab with E. Keys 1/2/3 pick tools. Press H to hide this bar.";
            }
        }

        void FaceBoard()
        {
            var desktop = FindAnyObjectByType<DesktopPlayerController>(FindObjectsInactive.Include);
            if (desktop != null)
            {
                var board = GameObject.Find("MissionCanvas");
                if (board != null)
                {
                    var dir = board.transform.position - desktop.transform.position;
                    dir.y = 0f;
                    if (dir.sqrMagnitude > 0.01f)
                        desktop.transform.rotation = Quaternion.LookRotation(dir.normalized);
                }
            }

            SetStatus("Turned to board. Click Desktop, then Start Dive (or Enter).");
        }

        void SetStatus(string text)
        {
            if (statusText != null)
                statusText.text = text;
        }

        static void EnsureEventSystemForMouse()
        {
            var es = FindAnyObjectByType<EventSystem>();
            if (es == null)
            {
                var go = new GameObject("EventSystem");
                es = go.AddComponent<EventSystem>();
            }

            // XR module often makes screen UI clicks unreliable in Editor simulator.
            var xrModule = es.GetComponent<XRUIInputModule>();
            if (xrModule != null)
                xrModule.enabled = false;

            var oldStandalone = es.GetComponent<StandaloneInputModule>();
            if (oldStandalone != null)
                oldStandalone.enabled = false;

            var inputModule = es.GetComponent<InputSystemUIInputModule>();
            if (inputModule == null)
                inputModule = es.gameObject.AddComponent<InputSystemUIInputModule>();
            inputModule.enabled = true;

            // Make sure this EventSystem is active.
            es.enabled = true;
            if (EventSystem.current != es)
                es.gameObject.SetActive(true);
        }

        static GameObject CreatePanel(Transform parent, string name, Vector2 size, Color color)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var image = go.AddComponent<Image>();
            image.color = color;
            image.raycastTarget = true;
            var rt = go.GetComponent<RectTransform>();
            rt.sizeDelta = size;
            rt.anchoredPosition = Vector2.zero;
            return go;
        }

        static Text CreateLabel(Transform parent, string name, string value, int size, Vector2 pos, Vector2 rect)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var text = go.AddComponent<Text>();
            text.text = value;
            text.fontSize = size;
            text.color = Color.white;
            text.alignment = TextAnchor.MiddleCenter;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.raycastTarget = false; // clicks pass through text to the button
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            var rt = text.GetComponent<RectTransform>();
            rt.sizeDelta = rect;
            rt.anchoredPosition = pos;
            return text;
        }

        static void CreateButton(Transform parent, string label, Vector2 pos, UnityEngine.Events.UnityAction action)
        {
            var go = new GameObject("Btn_" + label);
            go.transform.SetParent(parent, false);
            var image = go.AddComponent<Image>();
            image.color = new Color(0.12f, 0.5f, 0.6f, 1f);
            image.raycastTarget = true;
            var button = go.AddComponent<Button>();
            button.transition = Selectable.Transition.ColorTint;
            button.navigation = new Navigation { mode = Navigation.Mode.None };
            button.onClick.AddListener(action);
            var rt = go.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(190f, 52f);
            rt.anchoredPosition = pos;

            var textGo = new GameObject("Text");
            textGo.transform.SetParent(go.transform, false);
            var text = textGo.AddComponent<Text>();
            text.text = label;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.white;
            text.fontSize = 18;
            text.raycastTarget = false;
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            var trt = text.GetComponent<RectTransform>();
            trt.anchorMin = Vector2.zero;
            trt.anchorMax = Vector2.one;
            trt.offsetMin = Vector2.zero;
            trt.offsetMax = Vector2.zero;
        }
    }
}
