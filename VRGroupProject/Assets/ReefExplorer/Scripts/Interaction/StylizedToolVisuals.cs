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
            UpgradeHazards();
            UpgradeRecommendationMarker();
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
            var rubbishItems = FindObjectsByType<RubbishItem>(FindObjectsSortMode.None);
            int idx = 0;
            foreach (var ri in rubbishItems)
            {
                var go = ri.gameObject;
                if (go.transform.Find("Visual") != null) continue;

                var visual = new GameObject("Visual");
                visual.transform.SetParent(go.transform, false);

                // Alternate between can and bag shapes
                if (idx % 2 == 0)
                {
                    CreatePart(visual.transform, "Can", PrimitiveType.Cylinder,
                        Vector3.zero, new Vector3(0.06f, 0.08f, 0.06f),
                        new Color(0.7f, 0.7f, 0.7f));
                    CreatePart(visual.transform, "Label", PrimitiveType.Cube,
                        new Vector3(0f, 0f, 0.035f), new Vector3(0.05f, 0.06f, 0.005f),
                        new Color(0.8f, 0.2f, 0.2f));
                }
                else
                {
                    CreatePart(visual.transform, "Bag", PrimitiveType.Cube,
                        Vector3.zero, new Vector3(0.1f, 0.07f, 0.06f),
                        new Color(0.3f, 0.3f, 0.35f));
                }
                idx++;
            }
        }

        static void UpgradeHazards()
        {
            foreach (var hf in FindObjectsByType<HazardFlag>(FindObjectsSortMode.None))
            {
                var go = hf.gameObject;
                if (go.transform.Find("Visual") != null) continue;

                var visual = new GameObject("Visual");
                visual.transform.SetParent(go.transform, false);

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

            go.transform.SetPositionAndRotation(new Vector3(0.15f, 1.13f, -1.7f), Quaternion.identity);
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
            go.transform.SetPositionAndRotation(new Vector3(-0.35f, 1.14f, -1.7f), Quaternion.identity);
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
            if (go == null)
                return;

            var oldVisual = go.transform.Find("Visual");
            if (oldVisual != null)
                Destroy(oldVisual.gameObject);

            go.transform.position = new Vector3(0.55f, 1.12f, -1.7f);
            // Match tool scale better (was oversized vs scanner/bottle).
            go.transform.localScale = Vector3.one * 0.14f;

            var rootRenderer = go.GetComponent<MeshRenderer>();
            if (rootRenderer != null)
                rootRenderer.enabled = true;

            if (go.GetComponent<Collider>() == null)
                go.AddComponent<SphereCollider>();

            EnsureGrabReady(go);
            RefreshGrabColliders(go);

            var visual = new GameObject("Visual");
            visual.transform.SetParent(go.transform, false);
            CreatePart(visual.transform, "Stripe", PrimitiveType.Cylinder,
                Vector3.zero, new Vector3(1.05f, 0.12f, 1.05f), Color.white);

            AddFloatingLabel(go.transform, "BUOY", new Vector3(0f, 0.9f, 0f), 0.08f);
            if (go.GetComponent<TableDropSnap>() == null)
                go.AddComponent<TableDropSnap>();
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
        }
    }

    /// <summary>
    /// When dropped near the station console, snap onto the table instead of falling under it.
    /// </summary>
    public sealed class TableDropSnap : MonoBehaviour
    {
        [SerializeField] float tableY = 1.14f;
        [SerializeField] Vector3 tableCenter = new Vector3(0f, 1.14f, -1.7f);
        [SerializeField] float tableRadius = 1.6f;

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

