using System.Text;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

namespace ReefExplorer.Diagnostics
{
    /// <summary>
    /// Development-only overlay for diagnosing grab/hover/select issues.
    /// Enable via Inspector or menu: Reef Explorer / Diagnostics / Toggle Grab HUD.
    /// Not intended for the normal player experience.
    /// </summary>
    [DefaultExecutionOrder(1000)]
    public sealed class XRGrabDiagnosticsHUD : MonoBehaviour
    {
        [SerializeField] bool showInBuilds;
        [SerializeField] bool visible = true;
        [SerializeField] Key toggleKey = Key.F3;
        [SerializeField] Vector2 screenPosition = new Vector2(12f, 12f);

        NearFarInteractor[] nearFarInteractors;
        XRInteractionManager manager;
        readonly StringBuilder builder = new StringBuilder(512);
        GUIStyle style;

        public static XRGrabDiagnosticsHUD Instance { get; private set; }

        public bool Visible
        {
            get => visible;
            set => visible = value;
        }

        void Awake()
        {
            if (!Application.isEditor && !Debug.isDebugBuild && !showInBuilds)
            {
                enabled = false;
                return;
            }

            Instance = this;
            RefreshCaches();
        }

        void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        void Update()
        {
            if (Keyboard.current != null && Keyboard.current[toggleKey].wasPressedThisFrame)
                visible = !visible;

            if (Time.frameCount % 60 == 0)
                RefreshCaches();
        }

        void RefreshCaches()
        {
            manager = FindAnyObjectByType<XRInteractionManager>();
            nearFarInteractors = FindObjectsByType<NearFarInteractor>();
        }

        void OnGUI()
        {
            if (!visible)
                return;

            if (style == null)
            {
                style = new GUIStyle(GUI.skin.box)
                {
                    alignment = TextAnchor.UpperLeft,
                    fontSize = 13,
                    richText = true,
                    wordWrap = true,
                    padding = new RectOffset(10, 10, 10, 10)
                };
                style.normal.textColor = Color.white;
            }

            builder.Clear();
            builder.AppendLine("<b>Reef Explorer Grab Diagnostics</b> (F3 toggle)");
            builder.Append("XR Interaction Manager: ").AppendLine(manager != null ? manager.name : "<color=red>MISSING</color>");

            var origins = FindObjectsByType<Unity.XR.CoreUtils.XROrigin>();
            builder.Append("XR Origin count: ").Append(origins.Length);
            if (origins.Length != 1)
                builder.Append(" <color=yellow>(expected 1)</color>");
            builder.AppendLine();

            var listeners = FindObjectsByType<AudioListener>();
            builder.Append("AudioListener count: ").Append(listeners.Length);
            if (listeners.Length != 1)
                builder.Append(" <color=yellow>(expected 1)</color>");
            builder.AppendLine();

            if (nearFarInteractors == null || nearFarInteractors.Length == 0)
            {
                builder.AppendLine("<color=red>No NearFarInteractor found</color>");
            }
            else
            {
                foreach (var interactor in nearFarInteractors)
                {
                    if (interactor == null)
                        continue;

                    builder.AppendLine();
                    builder.Append("<b>").Append(interactor.name).Append("</b> (").Append(interactor.handedness).AppendLine(")");
                    builder.Append("  enabled/active: ").Append(interactor.isActiveAndEnabled).AppendLine();
                    builder.Append("  hasSelection: ").Append(interactor.hasSelection).AppendLine();
                    builder.Append("  hasHover: ").Append(interactor.hasHover).AppendLine();

                    if (interactor.interactablesHovered.Count > 0)
                    {
                        builder.Append("  hovered: ");
                        for (var i = 0; i < interactor.interactablesHovered.Count; i++)
                        {
                            if (i > 0) builder.Append(", ");
                            builder.Append(interactor.interactablesHovered[i].transform.name);
                        }
                        builder.AppendLine();
                    }
                    else
                    {
                        builder.AppendLine("  hovered: (none)");
                    }

                    if (interactor.interactablesSelected.Count > 0)
                    {
                        builder.Append("  selected/grabbed: ");
                        for (var i = 0; i < interactor.interactablesSelected.Count; i++)
                        {
                            if (i > 0) builder.Append(", ");
                            builder.Append(interactor.interactablesSelected[i].transform.name);
                        }
                        builder.AppendLine();
                    }
                    else
                    {
                        builder.AppendLine("  selected/grabbed: (none)");
                    }
                }
            }

            builder.AppendLine();
            builder.AppendLine("<b>Simulator grab tip</b>");
            builder.AppendLine("1) Hold Space = move RIGHT controller with mouse");
            builder.AppendLine("2) Aim the ray / bring controller near the Cube");
            builder.AppendLine("3) Press G = Grip / Select (grab)");
            builder.AppendLine("Mouse Left Click = Trigger / Activate (NOT grab)");

            var width = Mathf.Min(520f, Screen.width - 24f);
            var height = Mathf.Min(420f, Screen.height - 24f);
            GUI.Box(new Rect(screenPosition.x, screenPosition.y, width, height), builder.ToString(), style);
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void AutoAttachInEditorPlayMode()
        {
            if (!Application.isEditor)
                return;

            if (FindAnyObjectByType<XRGrabDiagnosticsHUD>() != null)
                return;

            var go = new GameObject("XR Grab Diagnostics HUD");
            go.AddComponent<XRGrabDiagnosticsHUD>();
            DontDestroyOnLoad(go);
        }
    }
}
