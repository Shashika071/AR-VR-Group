using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace ReefExplorer.Interaction
{
    /// <summary>
    /// Builds clearer tool shapes. Keeps grab colliders valid for desktop + XR.
    /// </summary>
    public sealed class StylizedToolVisuals : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            if (GameObject.Find("ResearchStation") == null)
                return;
            if (FindAnyObjectByType<StylizedToolVisuals>() != null)
                return;

            var host = new GameObject("StylizedToolVisuals");
            host.AddComponent<StylizedToolVisuals>();
        }

        void Start()
        {
            UpgradeScanner();
            UpgradeBottle();
            UpgradeBuoy();
            UpgradeAnimals();
            UpgradeCoralSurveyPoints();
            UpgradeRubbish();
            StartCoroutine(RefitTrashBags());
            StartCoroutine(UncoverRubbish());
            UpgradeHazards();
            UpgradeRecommendationMarker();
            StartCoroutine(LabelTableTools());
        }

        IEnumerator LabelTableTools()
        {
            for (var i = 0; i < 8; i++)
                yield return null;

            var root = new GameObject("TableNameLabels");
            NameTool(root.transform, "PowerCell", "BUOY BATTERY", 0.05f);
            NameTool(root.transform, "Scanner", "SCANNER", 0.05f);
            NameTool(root.transform, "SampleBottle", "BOTTLE", 0.05f);
            NameTool(root.transform, "ToxinDisposalTool", "TOXIN TOOL", 0.05f);
            NameTool(root.transform, "RecommendationMarker", "MARKER", 0.22f);
            NameTool(root.transform, "SampleAnalyser", "ANALYSER", 0.22f);
            NameTool(root.transform, "SampleCrate", "SAMPLE BOX", 0.22f);
            NameTool(root.transform, "VehiclePowerPack_1", "CRAFT BATTERY", 0.22f);
        }

        static void NameTool(Transform root, string objectName, string caption, float lift)
        {
            Transform owner = null;
            foreach (var tr in FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (tr != null && tr.name == objectName)
                {
                    owner = tr;
                    break;
                }
            }

            if (owner == null)
                return;

            for (var i = owner.childCount - 1; i >= 0; i--)
            {
                var child = owner.GetChild(i);
                if (child.name.StartsWith("Label_"))
                    Destroy(child.gameObject);
            }

            TableNameTag.Create(root, owner, caption, lift);
        }

        static void UpgradeCoralSurveyPoints()
        {
            foreach (var cp in FindObjectsByType<CoralSurveyPoint>(FindObjectsSortMode.None))
            {
                var go = cp.gameObject;
                if (go.transform.Find("Visual") != null) continue;

                // Make it look like a survey marker stake
                var visual = new GameObject("Visual");
                visual.transform.SetParent(go.transform, false);

                CreatePart(visual.transform, "Post", PrimitiveType.Cylinder,
                    new Vector3(0f, 0.15f, 0f), new Vector3(0.04f, 0.2f, 0.04f),
                    new Color(0.9f, 0.35f, 0.5f));
                CreatePart(visual.transform, "Flag", PrimitiveType.Cube,
                    new Vector3(0.08f, 0.32f, 0f), new Vector3(0.12f, 0.08f, 0.02f),
                    new Color(1f, 0.4f, 0.55f));
            }
        }

        static void UpgradeRubbish()
        {
            var bagPrefab = LoadTrashBag();
            var rubbishItems = FindObjectsByType<RubbishItem>(FindObjectsSortMode.None);
            var i = 0;
            foreach (var ri in rubbishItems)
            {
                var go = ri.gameObject;
                if (go.transform.Find("Visual") != null)
                    continue;

                var visual = new GameObject("Visual");
                visual.transform.SetParent(go.transform, false);
                var parentScale = go.transform.lossyScale;
                visual.transform.localScale = new Vector3(
                    1f / Mathf.Max(0.01f, parentScale.x),
                    1f / Mathf.Max(0.01f, parentScale.y),
                    1f / Mathf.Max(0.01f, parentScale.z));

                var rootRenderer = go.GetComponent<MeshRenderer>();
                if (rootRenderer != null)
                    rootRenderer.enabled = false;

                if (bagPrefab != null)
                {
                    var bag = Object.Instantiate(bagPrefab, visual.transform);
                    bag.name = "TrashBag";
                    bag.transform.localPosition = new Vector3(0f, 0.02f, 0f);
                    bag.transform.localRotation = Quaternion.Euler(0f, i * 47f, 0f);
                    foreach (var col in bag.GetComponentsInChildren<Collider>(true))
                        Object.Destroy(col);
                    FitTrashBag(bag, 0.42f);
                }
                else
                {
                    CreatePart(visual.transform, "Bag", PrimitiveType.Cube,
                        new Vector3(0f, 0.06f, 0f), new Vector3(0.16f, 0.1f, 0.1f),
                        new Color(0.08f, 0.08f, 0.08f));
                }

                i++;
            }
        }

        static GameObject LoadTrashBag()
        {
#if UNITY_EDITOR
            return UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Black_Trash_Bag.fbx");
#else
            return null;
#endif
        }

        IEnumerator RefitTrashBags()
        {
            yield return null;
            foreach (var bag in FindObjectsByType<Transform>(FindObjectsSortMode.None))
            {
                if (bag != null && bag.name == "TrashBag")
                    FitTrashBag(bag.gameObject, 0.42f);
            }
        }

        IEnumerator UncoverRubbish()
        {
            yield return null;
            yield return null;
            yield return null;
            var rocks = RockRenderers();
            foreach (var item in FindObjectsByType<RubbishItem>(FindObjectsSortMode.None))
            {
                if (item == null || !item.gameObject.activeInHierarchy)
                    continue;

                for (var n = 0; n < 8; n++)
                {
                    if (!PushOutOfRock(item.transform, rocks))
                        break;
                }

                var p = item.transform.position;
                p.y = 0.22f;
                item.transform.position = p;
            }
        }

        static List<Renderer> RockRenderers()
        {
            var list = new List<Renderer>();
            foreach (var renderer in FindObjectsByType<Renderer>(FindObjectsSortMode.None))
            {
                if (renderer == null || !renderer.enabled || !CoversTheSand(renderer))
                    continue;
                list.Add(renderer);
            }

            return list;
        }

        static bool CoversTheSand(Renderer renderer)
        {
            var bounds = renderer.bounds;
            if (bounds.extents.y < 0.2f)
                return false;
            if (bounds.extents.x < 0.35f && bounds.extents.z < 0.35f)
                return false;
            if (bounds.extents.x > 5f || bounds.extents.z > 5f)
                return false;
            if (bounds.min.y > 1.4f)
                return false;

            var name = renderer.gameObject.name;
            if (name.IndexOf("Sand", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                name.IndexOf("Water", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                name.IndexOf("Plant", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                name.IndexOf("Beacon", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                name.IndexOf("Particle", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                name.IndexOf("Cloud", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                name.IndexOf("Fish", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                name.IndexOf("Star", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                name.IndexOf("Trash", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                name.IndexOf("Bag", System.StringComparison.OrdinalIgnoreCase) >= 0)
                return false;

            return true;
        }

        static bool PushOutOfRock(Transform item, List<Renderer> rocks)
        {
            var p = item.position;
            Renderer cover = null;
            var nearest = float.MaxValue;
            foreach (var rock in rocks)
            {
                if (rock == null)
                    continue;
                var bounds = rock.bounds;
                bounds.Expand(new Vector3(0.55f, 0.5f, 0.55f));
                var probe = p;
                probe.y = Mathf.Clamp(p.y + 0.2f, bounds.min.y, bounds.max.y);
                if (!bounds.Contains(probe))
                    continue;
                var flat = bounds.center - p;
                flat.y = 0f;
                var dist = flat.sqrMagnitude;
                if (dist >= nearest)
                    continue;
                nearest = dist;
                cover = rock;
            }

            if (cover == null)
                return false;

            var edge = cover.bounds;
            edge.Expand(new Vector3(0.95f, 0f, 0.95f));
            var left = Mathf.Abs(p.x - edge.min.x);
            var right = Mathf.Abs(edge.max.x - p.x);
            var back = Mathf.Abs(p.z - edge.min.z);
            var forward = Mathf.Abs(edge.max.z - p.z);
            var best = Mathf.Min(Mathf.Min(left, right), Mathf.Min(back, forward));
            if (best == left)
                p.x = edge.min.x;
            else if (best == right)
                p.x = edge.max.x;
            else if (best == back)
                p.z = edge.min.z;
            else
                p.z = edge.max.z;
            p.x = Mathf.Clamp(p.x, -12f, 12f);
            p.y = 0.22f;
            p.z = Mathf.Clamp(p.z, 0.5f, 21f);
            item.position = p;
            return true;
        }

        static void FitTrashBag(GameObject go, float targetSize)
        {
            if (go == null)
                return;
            go.transform.localScale = Vector3.one;
            var renderers = go.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0)
                return;
            var bounds = renderers[0].bounds;
            for (var n = 1; n < renderers.Length; n++)
                bounds.Encapsulate(renderers[n].bounds);
            var current = Mathf.Max(bounds.size.x, bounds.size.y, bounds.size.z);
            if (current < 0.001f)
                return;
            go.transform.localScale = Vector3.one * (targetSize / current);
        }

        static void UpgradeHazards()
        {
            foreach (var hf in FindObjectsByType<HazardFlag>(FindObjectsSortMode.None))
            {
                var go = hf.gameObject;
                if (go.transform.Find("Visual") != null) continue;

                var visual = new GameObject("Visual");
                visual.transform.SetParent(go.transform, false);
                var rootRenderer = go.GetComponent<MeshRenderer>();
                if (rootRenderer != null)
                    rootRenderer.enabled = false;

                // Barrel shape for toxic hazard
                CreatePart(visual.transform, "Barrel", PrimitiveType.Cylinder,
                    new Vector3(0f, 0.2f, 0f), new Vector3(0.25f, 0.35f, 0.25f),
                    new Color(0.6f, 0.5f, 0.15f));
                CreatePart(visual.transform, "Stripe1", PrimitiveType.Cube,
                    new Vector3(0f, 0.15f, 0.13f), new Vector3(0.22f, 0.06f, 0.01f),
                    new Color(0.1f, 0.1f, 0.1f));
                CreatePart(visual.transform, "Stripe2", PrimitiveType.Cube,
                    new Vector3(0f, 0.25f, 0.13f), new Vector3(0.22f, 0.06f, 0.01f),
                    new Color(0.1f, 0.1f, 0.1f));
                // Skull icon approximation
                CreatePart(visual.transform, "Warning", PrimitiveType.Sphere,
                    new Vector3(0f, 0.35f, 0.13f), new Vector3(0.08f, 0.08f, 0.02f),
                    new Color(1f, 0.2f, 0.1f));
            }
        }

        static void UpgradeRecommendationMarker()
        {
            var marker = GameObject.Find("RecommendationMarker");
            if (marker == null || marker.transform.Find("Visual") != null) return;

            var visual = new GameObject("Visual");
            visual.transform.SetParent(marker.transform, false);

            CreatePart(visual.transform, "Pole", PrimitiveType.Cylinder,
                Vector3.zero, new Vector3(0.03f, 0.18f, 0.03f),
                new Color(0.3f, 0.85f, 0.5f));
            CreatePart(visual.transform, "Flag", PrimitiveType.Cube,
                new Vector3(0.06f, 0.15f, 0f), new Vector3(0.08f, 0.06f, 0.01f),
                new Color(0.2f, 1f, 0.45f));

            AddFloatingLabel(marker.transform, "MARKER", new Vector3(0f, 0.3f, 0f), 0.012f);
        }

        static void UpgradeScanner()
        {
            var go = GameObject.Find("Scanner");
            if (go == null)
                return;

            // Rebuild visuals; keep Beam child if present.
            for (var i = go.transform.childCount - 1; i >= 0; i--)
            {
                var child = go.transform.GetChild(i);
                if (child.name == "Beam")
                    continue;
                DestroyImmediate(child.gameObject);
            }

            HideRootMesh(go);
            ClearCollidersImmediate(go);

            go.transform.SetPositionAndRotation(new Vector3(-0.3f, 1.12f, -1.78f), Quaternion.identity);
            go.transform.localScale = Vector3.one;

            var box = go.AddComponent<BoxCollider>();
            box.size = new Vector3(0.14f, 0.1f, 0.42f);
            box.center = new Vector3(0f, 0.02f, 0.02f);

            EnsureGrabReady(go);
            RefreshGrabColliders(go);

            var visual = new GameObject("Visual");
            visual.transform.SetParent(go.transform, false);

            var handle = CreatePart(visual.transform, "Handle", PrimitiveType.Cylinder,
                new Vector3(0f, 0f, -0.1f), new Vector3(0.035f, 0.07f, 0.035f),
                new Color(0.15f, 0.18f, 0.22f));
            handle.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);

            CreatePart(visual.transform, "Body", PrimitiveType.Cube,
                new Vector3(0f, 0.015f, 0.08f), new Vector3(0.06f, 0.05f, 0.24f),
                new Color(0.15f, 0.75f, 0.85f));

            var nose = CreatePart(visual.transform, "Emitter", PrimitiveType.Cylinder,
                new Vector3(0f, 0.015f, 0.22f), new Vector3(0.038f, 0.022f, 0.038f),
                new Color(0.95f, 0.85f, 0.25f));
            nose.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);

            var beamTf = go.transform.Find("Beam");
            if (beamTf == null)
            {
                var beamGo = new GameObject("Beam");
                beamGo.transform.SetParent(go.transform, false);
                beamGo.transform.localPosition = new Vector3(0f, 0.015f, 0.26f);
                var lr = beamGo.AddComponent<LineRenderer>();
                lr.enabled = false;
                lr.widthMultiplier = 0.01f;
                var mat = new Material(Shader.Find("Sprites/Default") ?? Shader.Find("Universal Render Pipeline/Unlit"));
                lr.material = mat;
                lr.startColor = new Color(0.2f, 1f, 0.8f, 0.9f);
                lr.endColor = new Color(0.2f, 1f, 0.8f, 0.1f);
            }
            else
            {
                beamTf.localPosition = new Vector3(0f, 0.015f, 0.26f);
            }

            AddFloatingLabel(go.transform, "SCANNER", new Vector3(0f, 0.08f, 0f), 0.012f);
            if (go.GetComponent<TableDropSnap>() == null)
                go.AddComponent<TableDropSnap>();
        }

        static void UpgradeBottle()
        {
            var go = GameObject.Find("SampleBottle");
            if (go == null)
                return;

            // Remove ALL child meshes (old huge Liquid cylinder, Visual, labels).
            ClearChildObjectsImmediate(go.transform);

            HideRootMesh(go);
            ClearCollidersImmediate(go);

            // Table-sized bottle, closer to buoy scale.
            go.transform.SetPositionAndRotation(new Vector3(0.3f, 1.14f, -1.78f), Quaternion.identity);
            go.transform.localScale = Vector3.one;

            var capsule = go.AddComponent<CapsuleCollider>();
            capsule.height = 0.2f;
            capsule.radius = 0.04f;
            capsule.center = new Vector3(0f, 0.02f, 0f);

            EnsureGrabReady(go);
            RefreshGrabColliders(go);

            var visual = new GameObject("Visual");
            visual.transform.SetParent(go.transform, false);

            CreatePart(visual.transform, "Body", PrimitiveType.Cylinder,
                new Vector3(0f, 0f, 0f), new Vector3(0.055f, 0.055f, 0.055f),
                new Color(0.55f, 0.85f, 0.95f));

            CreatePart(visual.transform, "Neck", PrimitiveType.Cylinder,
                new Vector3(0f, 0.075f, 0f), new Vector3(0.025f, 0.022f, 0.025f),
                new Color(0.7f, 0.9f, 0.95f));

            CreatePart(visual.transform, "Cap", PrimitiveType.Cylinder,
                new Vector3(0f, 0.105f, 0f), new Vector3(0.032f, 0.012f, 0.032f),
                new Color(1f, 0.55f, 0.15f));

            var liquid = CreatePart(visual.transform, "Liquid", PrimitiveType.Cylinder,
                new Vector3(0f, -0.008f, 0f), new Vector3(0.04f, 0.038f, 0.04f),
                new Color(0.15f, 0.55f, 0.8f, 0.85f));

            var sample = go.GetComponent<SampleBottle>();
            if (sample != null)
            {
                var field = typeof(SampleBottle).GetField("liquidRenderer",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                field?.SetValue(sample, liquid.GetComponent<Renderer>());
            }

            AddFloatingLabel(go.transform, "BOTTLE", new Vector3(0f, 0.14f, 0f), 0.012f);
            if (go.GetComponent<TableDropSnap>() == null)
                go.AddComponent<TableDropSnap>();
        }

        static void UpgradeBuoy()
        {
            var go = GameObject.Find("PracticeBuoy");
            if (go != null)
                go.SetActive(false);
        }

        static void UpgradeAnimals()
        {
            StyleAnimal("Animal_Clownfish", new Color(1f, 0.45f, 0.1f), true);
            StyleAnimal("Animal_Sea Turtle", new Color(0.25f, 0.65f, 0.35f), false);
            StyleAnimal("Animal_Ray", new Color(0.4f, 0.5f, 0.65f), false);
        }

        static void StyleAnimal(string name, Color color, bool small)
        {
            var go = GameObject.Find(name);
            if (go == null)
                return;
            if (go.transform.Find("Visual") != null)
                return;
            // Procedural animals already have Body / Shell / Wing parts.
            if (go.transform.Find("Body") != null || go.transform.Find("Shell") != null || go.transform.Find("Wing") != null)
                return;

            var rootRenderer = go.GetComponent<MeshRenderer>();
            if (rootRenderer != null)
                rootRenderer.enabled = false;

            var visual = new GameObject("Visual");
            visual.transform.SetParent(go.transform, false);

            if (name.Contains("Clown"))
            {
                CreatePart(visual.transform, "Body", PrimitiveType.Capsule,
                    Vector3.zero, new Vector3(0.35f, 0.25f, 0.55f), color);
                CreatePart(visual.transform, "Stripe", PrimitiveType.Cube,
                    Vector3.zero, new Vector3(0.38f, 0.28f, 0.08f), Color.white);
            }
            else if (name.Contains("Turtle"))
            {
                CreatePart(visual.transform, "Shell", PrimitiveType.Sphere,
                    new Vector3(0f, 0.05f, 0f), new Vector3(0.9f, 0.45f, 0.7f), color);
                CreatePart(visual.transform, "Head", PrimitiveType.Sphere,
                    new Vector3(0f, 0.05f, 0.45f), new Vector3(0.28f, 0.22f, 0.28f), color * 1.1f);
            }
            else
            {
                CreatePart(visual.transform, "Wing", PrimitiveType.Cube,
                    Vector3.zero, new Vector3(1.4f, 0.08f, 0.9f), color);
                CreatePart(visual.transform, "Tail", PrimitiveType.Cube,
                    new Vector3(0f, 0f, -0.7f), new Vector3(0.08f, 0.05f, 0.7f), color * 0.9f);
            }

            AddFloatingLabel(go.transform, name.Replace("Animal_", "").ToUpperInvariant(),
                new Vector3(0f, small ? 0.45f : 0.7f, 0f), 0.03f);
        }

        static void HideRootMesh(GameObject go)
        {
            var mr = go.GetComponent<MeshRenderer>();
            if (mr != null)
                mr.enabled = false;

            // Also remove root mesh filter so the original big cylinder never shows.
            var mf = go.GetComponent<MeshFilter>();
            if (mf != null)
                DestroyImmediate(mf);
        }

        static void ClearChildObjectsImmediate(Transform root)
        {
            for (var i = root.childCount - 1; i >= 0; i--)
            {
                var child = root.GetChild(i);
                // Keep Beam if somehow parented here; bottle should not have Beam.
                DestroyImmediate(child.gameObject);
            }
        }

        static void ClearCollidersImmediate(GameObject go)
        {
            var cols = go.GetComponents<Collider>();
            for (var i = 0; i < cols.Length; i++)
            {
                if (Application.isPlaying)
                    DestroyImmediate(cols[i]);
                else
                    DestroyImmediate(cols[i]);
            }
        }

        static void EnsureGrabReady(GameObject go)
        {
            var body = go.GetComponent<Rigidbody>();
            if (body == null)
                body = go.AddComponent<Rigidbody>();
            body.isKinematic = true;
            body.useGravity = true;
            body.interpolation = RigidbodyInterpolation.Interpolate;
            body.collisionDetectionMode = CollisionDetectionMode.Continuous;
            body.linearDamping = 1f;
            body.angularDamping = 1f;

            var grab = go.GetComponent<XRGrabInteractable>();
            if (grab == null)
            {
                grab = go.AddComponent<XRGrabInteractable>();
                grab.movementType = XRBaseInteractable.MovementType.Instantaneous;
                grab.throwOnDetach = false;
            }
            else
            {
                grab.movementType = XRBaseInteractable.MovementType.Instantaneous;
                grab.throwOnDetach = false;
            }
        }

        static void RefreshGrabColliders(GameObject go)
        {
            var grab = go.GetComponent<XRGrabInteractable>();
            if (grab == null)
                return;

            // XRGrabInteractable caches colliders in Awake; force refresh after we rebuild them.
            grab.enabled = false;
            grab.colliders.Clear();
            var col = go.GetComponent<Collider>();
            if (col != null)
                grab.colliders.Add(col);
            grab.enabled = true;
        }

        static GameObject CreatePart(Transform parent, string name, PrimitiveType type, Vector3 localPos, Vector3 localScale, Color color)
        {
            var part = GameObject.CreatePrimitive(type);
            part.name = name;
            part.transform.SetParent(parent, false);
            part.transform.localPosition = localPos;
            part.transform.localScale = localScale;
            var col = part.GetComponent<Collider>();
            if (col != null)
                DestroyImmediate(col);

            var renderer = part.GetComponent<Renderer>();
            var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            var mat = new Material(shader);
            if (mat.HasProperty("_BaseColor"))
                mat.SetColor("_BaseColor", color);
            else
                mat.color = color;
            renderer.sharedMaterial = mat;
            return part;
        }

        static void AddFloatingLabel(Transform parent, string text, Vector3 localPos, float characterSize = 0.025f)
        {
            var existing = parent.Find("Label_" + text);
            if (existing != null)
                DestroyImmediate(existing.gameObject);

            var label = new GameObject("Label_" + text);
            label.transform.SetParent(parent, false);
            label.transform.localPosition = localPos;
            var mesh = label.AddComponent<TextMesh>();
            mesh.text = text;
            mesh.characterSize = characterSize;
            mesh.fontSize = 32;
            mesh.anchor = TextAnchor.MiddleCenter;
            mesh.alignment = TextAlignment.Center;
            mesh.color = Color.white;
            mesh.fontStyle = FontStyle.Bold;
            label.AddComponent<BillboardLabel>();
            label.AddComponent<HideLabelWhenHeld>();
        }
    }

    /// <summary>Same-size name above a table tool. Hidden while that tool is held.</summary>
    sealed class TableNameTag : MonoBehaviour
    {
        const float CharacterSize = 0.013f;

        Transform target;
        Renderer textRenderer;
        float lift;
        static ReefExplorer.Input.DesktopPlayerController player;

        public static void Create(Transform root, Transform target, string caption, float lift)
        {
            var go = new GameObject("Name_" + caption);
            go.transform.SetParent(root, false);
            var mesh = go.AddComponent<TextMesh>();
            mesh.text = caption;
            mesh.characterSize = CharacterSize;
            mesh.fontSize = 32;
            mesh.anchor = TextAnchor.MiddleCenter;
            mesh.alignment = TextAlignment.Center;
            mesh.color = Color.white;
            mesh.fontStyle = FontStyle.Bold;
            go.AddComponent<BillboardLabel>();
            var tag = go.AddComponent<TableNameTag>();
            tag.target = target;
            tag.lift = lift;
            tag.textRenderer = go.GetComponent<Renderer>();
        }

        void LateUpdate()
        {
            if (textRenderer == null)
                return;
            if (target == null || !target.gameObject.activeInHierarchy || IsHeld(target))
            {
                textRenderer.enabled = false;
                return;
            }

            textRenderer.enabled = true;
            var top = target.position.y;
            var center = target.position;
            var found = false;
            foreach (var renderer in target.GetComponentsInChildren<Renderer>())
            {
                if (renderer == null || renderer is TextMesh || !renderer.enabled || !renderer.gameObject.activeInHierarchy)
                    continue;
                if (renderer.bounds.size.y > 1.2f)
                    continue;
                if (!found || renderer.bounds.max.y > top)
                {
                    top = renderer.bounds.max.y;
                    center = renderer.bounds.center;
                    found = true;
                }
            }

            transform.position = new Vector3(center.x, top + lift, center.z);
            transform.localScale = Vector3.one;
        }

        static bool IsHeld(Transform owner)
        {
            if (player == null)
                player = FindAnyObjectByType<ReefExplorer.Input.DesktopPlayerController>();
            var inHand = player != null ? player.HeldObject : null;
            if (inHand != null && (inHand == owner || owner.IsChildOf(inHand)))
                return true;
            var grab = owner.GetComponent<XRGrabInteractable>();
            return grab != null && grab.isSelected;
        }
    }

    /// <summary>World name stays visible on the table and hides while the tool is in hand.</summary>
    sealed class HideLabelWhenHeld : MonoBehaviour
    {
        Renderer textRenderer;
        Transform owner;
        XRGrabInteractable grab;
        static ReefExplorer.Input.DesktopPlayerController player;

        void Awake()
        {
            textRenderer = GetComponent<Renderer>();
            owner = transform.parent;
            if (owner != null)
                grab = owner.GetComponent<XRGrabInteractable>();
        }

        void LateUpdate()
        {
            if (textRenderer == null)
                return;
            if (player == null)
                player = FindAnyObjectByType<ReefExplorer.Input.DesktopPlayerController>();

            var held = grab != null && grab.isSelected;
            var inHand = player != null ? player.HeldObject : null;
            if (!held && inHand != null && owner != null &&
                (inHand == owner || owner.IsChildOf(inHand)))
                held = true;

            textRenderer.enabled = !held;
        }
    }

    /// <summary>
    /// When dropped near the station console, snap onto the table instead of falling under it.
    /// </summary>
    public sealed class TableDropSnap : MonoBehaviour
    {
        [SerializeField] float tableY = 1.08f;
        [SerializeField] Vector3 tableCenter = new Vector3(0f, 1.08f, -2f);
        [SerializeField] float tableRadius = 1.5f;

        Rigidbody body;
        XRGrabInteractable grab;

        void Awake()
        {
            body = GetComponent<Rigidbody>();
            grab = GetComponent<XRGrabInteractable>();
            if (grab != null)
                grab.selectExited.AddListener(OnSelectExited);
        }

        void OnDestroy()
        {
            if (grab != null)
                grab.selectExited.RemoveListener(OnSelectExited);
        }

        void OnSelectExited(UnityEngine.XR.Interaction.Toolkit.SelectExitEventArgs _)
        {
            Invoke(nameof(SnapIfOverTable), 0.05f);
        }

        public void SnapIfOverTable()
        {
            if (body == null)
                body = GetComponent<Rigidbody>();

            var flat = transform.position;
            flat.y = tableCenter.y;
            var centerFlat = tableCenter;
            centerFlat.y = flat.y;

            if (Vector3.Distance(flat, centerFlat) <= tableRadius)
            {
                transform.position = new Vector3(transform.position.x, tableY, transform.position.z);
                transform.rotation = Quaternion.identity;
                RigidbodyUtil.ParkKinematic(body);
            }
            else if (body != null)
            {
                body.isKinematic = false;
                body.useGravity = true;
                body.detectCollisions = true;
            }
        }
    }

    /// <summary>
    /// Tool floating labels. Same facing math as FaceCameraLabel (kept for existing scenes).
    /// </summary>
    public sealed class BillboardLabel : MonoBehaviour
    {
        void LateUpdate()
        {
            var cam = Camera.main;
            if (cam == null)
                return;

            var away = transform.position - cam.transform.position;
            if (away.sqrMagnitude < 0.0001f)
                return;

            transform.rotation = Quaternion.LookRotation(away.normalized, Vector3.up);
        }
    }
}

