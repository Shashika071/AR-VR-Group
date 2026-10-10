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
        Text hintText;
        string hintLine = "";
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
                if (!open)
                    hintLine = "";
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
                hintLine = rows[selected].hint;
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
                MissionEvents.RaiseFeedback("Stand in a blue sample circle and press E, then put the bottle in the box on the table.");
            else if (held.GetComponent<ReefExplorer.Interaction.ToxinDisposalTool>() != null)
                MissionEvents.RaiseFeedback("Stand in the green cloud and press E to dispose the toxin.");
            else if (held.GetComponent<ReefExplorer.Interaction.ScannerTool>() != null)
                MissionEvents.RaiseFeedback("Aim at coral or an animal and hold the mouse button.");
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
            panel.anchoredPosition = new Vector2(16f, -56f);
            panel.sizeDelta = new Vector2(560f, 640f);

            var back = root.AddComponent<Image>();
            back.color = new Color(0.02f, 0.05f, 0.08f, 1f);
            back.sprite = White();

            var textGo = new GameObject("Body");
            textGo.transform.SetParent(root.transform, false);
            var textRect = textGo.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(16f, 96f);
            textRect.offsetMax = new Vector2(-16f, -16f);
            body = textGo.AddComponent<Text>();
            body.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            body.fontSize = 28;
            body.lineSpacing = 1f;
            body.supportRichText = true;
            body.alignment = TextAnchor.UpperLeft;
            body.color = Color.white;
            body.horizontalOverflow = HorizontalWrapMode.Wrap;
            body.verticalOverflow = VerticalWrapMode.Overflow;
            var bodyOutline = body.gameObject.AddComponent<Outline>();
            bodyOutline.effectColor = Color.black;
            bodyOutline.effectDistance = new Vector2(1.4f, -1.4f);

            var hintBar = new GameObject("HintBar");
            hintBar.transform.SetParent(root.transform, false);
            var barRect = hintBar.AddComponent<RectTransform>();
            barRect.anchorMin = new Vector2(0f, 0f);
            barRect.anchorMax = new Vector2(1f, 0f);
            barRect.pivot = new Vector2(0.5f, 0f);
            barRect.anchoredPosition = new Vector2(0f, 8f);
            barRect.sizeDelta = new Vector2(-16f, 84f);
            var barImage = hintBar.AddComponent<Image>();
            barImage.color = new Color(0.08f, 0.22f, 0.28f, 1f);
            barImage.sprite = White();

            var hintGo = new GameObject("Hint");
            hintGo.transform.SetParent(hintBar.transform, false);
            var hintRect = hintGo.AddComponent<RectTransform>();
            hintRect.anchorMin = Vector2.zero;
            hintRect.anchorMax = Vector2.one;
            hintRect.offsetMin = new Vector2(12f, 8f);
            hintRect.offsetMax = new Vector2(-12f, -8f);
            hintText = hintGo.AddComponent<Text>();
            hintText.font = body.font;
            hintText.fontSize = 24;
            hintText.fontStyle = FontStyle.Bold;
            hintText.alignment = TextAnchor.UpperLeft;
            hintText.color = Color.white;
            hintText.horizontalOverflow = HorizontalWrapMode.Wrap;
            hintText.verticalOverflow = VerticalWrapMode.Overflow;
            var hintOutline = hintGo.AddComponent<Outline>();
            hintOutline.effectColor = Color.black;
            hintOutline.effectDistance = new Vector2(1.2f, -1.2f);
            hintText.text = "";
            hintBar.SetActive(false);
            hintBar.transform.SetSiblingIndex(root.transform.childCount - 1);

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
                "Pick up the buoy battery on the table, carry it to the cyan box, and press E."));
            rows.Add(Make("craft", ReefExplorer.Interaction.VehiclePowerPack.IsInstalled, "Craft battery",
                "Pick up the craft battery on the station console, stand next to the dive vehicle, and press E."));

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

            foreach (var point in FindObjectsByType<ReefExplorer.Interaction.CoralSurveyPoint>(FindObjectsSortMode.None))
            {
                if (point == null || !point.gameObject.activeInHierarchy)
                    continue;
                coralNeed++;
                if (point.IsScanned)
                    coralDone++;
            }

            ReefExplorer.Environment.VisibleObjectiveTrim.Apply();
            foreach (var zone in FindObjectsByType<ReefExplorer.Interaction.SampleZone>(FindObjectsSortMode.None))
            {
                if (zone == null || !zone.gameObject.activeInHierarchy)
                    continue;
                sampleNeed++;
                if (mc.DiveLog.perSiteSamples.Exists(s =>
                        s != null && s.collected &&
                        string.Equals(s.siteId, zone.SiteId, System.StringComparison.OrdinalIgnoreCase)))
                    sampleDone++;
            }

            rows.Insert(2, Make("coral", coralNeed > 0 && coralDone >= coralNeed,
                "Scan coral  " + coralDone + "/" + coralNeed,
                "Pick up the scanner, aim at the coral marker, and hold click."));
            rows.Add(Make("sample", sampleNeed > 0 && sampleDone >= sampleNeed,
                "Water sample  " + sampleDone + "/" + sampleNeed,
                "Pick up the blue bottle, stand in a sample circle, press E, and wait for the bar."));
            var samplesTested = sampleNeed > 0 && sampleDone >= sampleNeed &&
                                mc.DiveLog.perSiteSamples.TrueForAll(s => s == null || !s.collected || s.analysed);
            rows.Add(Make("analyser", samplesTested, "Analyser",
                "Pick up the analyser on the table and press E. A bar fills, then the water test appears."));
            rows.Add(Make("box", ReefExplorer.Interaction.SampleReturnBox.IsDeposited, "Sample box",
                "After every water sample, put the bottle in the box on the table and press E."));
            rubbishNeed = Mathf.Max(0, rubbishNeed - ReefExplorer.Environment.ToxinFieldRuntime.HiddenRubbish);
            var toxinTotal = ReefExplorer.Interaction.ToxinPatch.Total;
            var toxinCleared = ReefExplorer.Interaction.ToxinPatch.ClearedCount;
            if (toxinTotal <= 0)
            {
                toxinTotal = toxinNeeded ? 1 : 0;
                toxinCleared = toxinDone ? 1 : 0;
            }
            rows.Add(Make("rubbish", rubbishNeed > 0 && rubbishDone >= rubbishNeed,
                "Rubbish  " + rubbishDone + "/" + rubbishNeed,
                "Go to each site and press E on the rubbish to pick it up."));
            if (toxinNeeded || toxinTotal > 0)
                rows.Add(Make("toxin", toxinTotal > 0 && toxinCleared >= toxinTotal, "Dispose toxin  " + toxinCleared + "/" + toxinTotal,
                    "Pick up the disposal tool on the table, stand in each green cloud, and press E."));
            rows.Add(Make("marker", mc.MarkerPlaced, "Recommendation marker",
                "Choose a restoration site, pick up the green marker on the console, carry it to that site's holder, and press E."));

            if (selected >= rows.Count)
                selected = 0;

            if (!string.IsNullOrEmpty(hintLine) && rows.Count > 0)
                hintLine = rows[Mathf.Clamp(selected, 0, rows.Count - 1)].hint;

            var lines = "OBJECT LIST\n";
            for (var i = 0; i < rows.Count; i++)
                lines += Row(rows[i].done, rows[i].label, i == selected);
            lines += "\nUp Down pick    H hint    B close";
            body.text = lines;
            var hintOpen = !string.IsNullOrEmpty(hintLine);
            body.rectTransform.offsetMin = new Vector2(16f, hintOpen ? 96f : 16f);
            if (hintText != null)
            {
                hintText.text = hintLine;
                hintText.transform.parent.gameObject.SetActive(hintOpen);
            }
            panel.sizeDelta = new Vector2(560f, 78f + rows.Count * 34f + (hintOpen ? 88f : 28f));
        }

        static TaskRow Make(string id, bool done, string label, string hint)
        {
            return new TaskRow { id = id, done = done, label = label, hint = hint };
        }

        static string Row(bool done, string label, bool picked)
        {
            var mark = picked ? "> " : "  ";
            var box = done ? "[x]  " : "[ ]  ";
            var color = done ? "#b6ff72" : "#ffe14a";
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
