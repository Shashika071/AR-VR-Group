using ReefExplorer.Core;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace ReefExplorer.UI
{
    /// <summary>
    /// B toggles a short list of the required animals and whether each one is scanned.
    /// </summary>
    public sealed class SurveyObjectList : MonoBehaviour
    {
        struct TaskRow
        {
            public string id;
            public bool done;
            public string label;
            public string hint;
        }

        RectTransform panel;
        Text body;
        bool open;
        int selected;
        readonly System.Collections.Generic.List<TaskRow> rows = new();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            if (GameObject.Find("ResearchStation") == null)
                return;
            if (FindAnyObjectByType<SurveyObjectList>() != null)
                return;

            var host = new GameObject("SurveyObjectList");
            host.AddComponent<SurveyObjectList>();
        }

        void Start() => Build();

        void Update()
        {
            if (Keyboard.current == null)
                return;

            if (Keyboard.current.bKey.wasPressedThisFrame)
            {
                open = !open;
                if (panel != null)
                    panel.gameObject.SetActive(open);
            }

            if (open && rows.Count > 0)
            {
                if (Keyboard.current.upArrowKey.wasPressedThisFrame)
                    selected = (selected - 1 + rows.Count) % rows.Count;
                if (Keyboard.current.downArrowKey.wasPressedThisFrame)
                    selected = (selected + 1) % rows.Count;
            }

            if (Keyboard.current.hKey.wasPressedThisFrame)
                ShowHint();

            if (open)
                Refresh();
        }

        void ShowHint()
        {
            if (open && rows.Count > 0)
            {
                selected = Mathf.Clamp(selected, 0, rows.Count - 1);
                MissionEvents.RaiseFeedback(rows[selected].hint);
                return;
            }

            var player = FindAnyObjectByType<ReefExplorer.Input.DesktopPlayerController>();
            var held = player != null ? player.HeldObject : null;
            if (held == null)
            {
                MissionEvents.RaiseFeedback("Open the list with B, choose an object, then press H.");
                return;
            }

            if (held.GetComponent<ReefExplorer.Interaction.PowerCell>() != null)
                MissionEvents.RaiseFeedback("Carry the buoy battery to the cyan box on the buoy and press E.");
            else if (held.GetComponent<ReefExplorer.Interaction.VehiclePowerPack>() != null)
                MissionEvents.RaiseFeedback("Stand next to the dive vehicle and press E to set this battery.");
            else if (held.GetComponent<ReefExplorer.Interaction.SampleBottle>() != null)
                MissionEvents.RaiseFeedback("Stand in a blue sample circle, press E, and wait for the bar.");
            else if (held.GetComponent<ReefExplorer.Interaction.ScannerTool>() != null)
                MissionEvents.RaiseFeedback("Aim at coral, an animal, or the toxin and hold the mouse button.");
            else
                MissionEvents.RaiseFeedback("Press E to use this, or open the list with B and press H.");
        }

        void Build()
        {
            var canvasGo = new GameObject("SurveyListCanvas");
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 70;
            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);

            var root = new GameObject("List");
            root.transform.SetParent(canvasGo.transform, false);
            panel = root.AddComponent<RectTransform>();
            panel.anchorMin = panel.anchorMax = new Vector2(0f, 1f);
            panel.pivot = new Vector2(0f, 1f);
            panel.anchoredPosition = new Vector2(16f, -44f);
            panel.sizeDelta = new Vector2(380f, 560f);

            var back = root.AddComponent<Image>();
            back.color = new Color(0.02f, 0.08f, 0.1f, 0.92f);
            back.sprite = White();

            var textGo = new GameObject("Body");
            textGo.transform.SetParent(root.transform, false);
            var textRect = textGo.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(8f, 6f);
            textRect.offsetMax = new Vector2(-8f, -6f);
            body = textGo.AddComponent<Text>();
            body.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            body.fontSize = 18;
            body.lineSpacing = 1f;
            body.supportRichText = true;
            body.alignment = TextAnchor.UpperLeft;
            body.color = Color.white;
            body.horizontalOverflow = HorizontalWrapMode.Wrap;
            body.verticalOverflow = VerticalWrapMode.Overflow;

            panel.gameObject.SetActive(false);
        }

        void Refresh()
        {
            if (body == null)
                return;

            var mc = MissionController.Instance;
            if (mc == null)
            {
                body.text = "OBJECT LIST\nNo survey yet.";
                return;
            }

            rows.Clear();
            rows.Add(Make("buoy", mc.BuoyRestored, "Buoy battery",
                "Pick up the buoy battery at the station, carry it to the cyan box, and press E."));
            rows.Add(Make("craft", ReefExplorer.Interaction.VehiclePowerPack.IsInstalled, "Craft battery",
                "Pick up the craft battery beside the dive vehicle, stand next to it, and press E."));

            var coralDone = 0;
            var coralNeed = 0;
            var sampleDone = 0;
            var sampleNeed = 0;
            var rubbishDone = 0;
            var rubbishNeed = 0;
            var toxinDone = true;
            var toxinNeeded = false;
            var sites = mc.Sites;
            if (sites != null)
            {
                foreach (var site in sites)
                {
                    if (site == null)
                        continue;
                    var progress = mc.GetSiteProgress(site.SiteId);
                    coralNeed++;
                    sampleNeed++;
                    if (progress.coralScanned)
                        coralDone++;
                    if (progress.sampleCollected)
                        sampleDone++;
                    rubbishNeed += site.InitialRubbishCount;
                    foreach (var rubbish in mc.DiveLog.rubbishCollected)
                    {
                        if (rubbish != null &&
                            string.Equals(rubbish.siteId, site.SiteId, System.StringComparison.OrdinalIgnoreCase))
                            rubbishDone++;
                    }

                    var animalName = site.TargetAnimal != null ? site.TargetAnimal.DisplayName : "Animal";
                    rows.Add(Make("animal_" + site.SiteId, progress.animalScanned, animalName,
                        "Pick up the scanner, go to " + site.DisplayName + ", aim at the " + animalName + ", and hold click."));

                    if (site.HasHazard)
                    {
                        toxinNeeded = true;
                        if (!progress.hazardFlagged)
                            toxinDone = false;
                    }
                }
            }

            rows.Insert(2, Make("coral", coralNeed > 0 && coralDone >= coralNeed,
                "Scan coral  " + coralDone + "/" + coralNeed,
                "Pick up the scanner, go to each site, aim at the coral point, and hold click."));
            rows.Add(Make("sample", sampleNeed > 0 && sampleDone >= sampleNeed,
                "Water sample  " + sampleDone + "/" + sampleNeed,
                "Pick up the blue bottle, stand in a sample circle, press E, and wait for the bar."));
            rows.Add(Make("rubbish", rubbishNeed > 0 && rubbishDone >= rubbishNeed,
                "Rubbish  " + rubbishDone + "/" + rubbishNeed,
                "Go to each site and press E on the rubbish to pick it up."));
            if (toxinNeeded)
                rows.Add(Make("toxin", toxinDone, "Flag toxin",
                    "Pick up the scanner, go to Seagrass Crossing, aim at the green toxic air, and hold click."));

            if (selected >= rows.Count)
                selected = 0;

            var lines = "OBJECT LIST\n";
            for (var i = 0; i < rows.Count; i++)
                lines += Row(rows[i].done, rows[i].label, i == selected);
            lines += "\nUp Down pick\nH hint    B close";
            body.text = lines;
        }

        static TaskRow Make(string id, bool done, string label, string hint)
        {
            return new TaskRow { id = id, done = done, label = label, hint = hint };
        }

        static string Row(bool done, string label, bool picked)
        {
            var mark = picked ? "> " : "  ";
            var box = done ? "[x]  " : "[ ]  ";
            var color = done ? "#7dff9a" : "#ffd27a";
            if (picked)
                color = "#ffffff";
            return "<color=" + color + ">" + mark + box + label + "</color>\n";
        }

        static Sprite White()
        {
            var tex = Texture2D.whiteTexture;
            return Sprite.Create(tex, new Rect(0f, 0f, tex.width, tex.height), new Vector2(0.5f, 0.5f));
        }
    }
}
