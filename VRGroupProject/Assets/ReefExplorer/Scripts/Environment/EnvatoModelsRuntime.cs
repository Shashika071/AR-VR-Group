using ReefExplorer.Interaction;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace ReefExplorer.Environment
{
    /// <summary>
    /// Fills the compact reef with Envato FBX: many corals + fish schools.
    /// Mission scan animals keep SurveyAnimal scripts on their roots.
    /// </summary>
    public sealed class EnvatoModelsRuntime : MonoBehaviour
    {
        const string FishFolder = "Assets/Fish";
        const string NewFishFolder = "Assets/New_fish";
        const string CoralFolder = "Assets/Corals";
        const string AnimalsFolder = "Assets/Other_Animals";

        static readonly string[] SchoolFish =
        {
            "Angelfish.obj",
            "Betta_Fish.obj",
            "Undualte_Triggerfish.FBX",
            "Protomelas Spilonotus.FBX",
            "Aligator Gar.FBX",
        };

        static readonly Color[] FishColors =
        {
            new(1f, 0.55f, 0.15f), new(1f, 0.85f, 0.2f), new(0.15f, 0.45f, 0.95f),
            new(0.95f, 0.9f, 0.2f), new(0.2f, 0.7f, 0.85f), new(0.95f, 0.6f, 0.25f),
            new(0.85f, 0.25f, 0.35f), new(0.95f, 0.95f, 0.85f), new(0.2f, 0.75f, 0.45f),
            new(0.55f, 0.25f, 0.85f), new(0.3f, 0.85f, 0.55f), new(0.9f, 0.75f, 0.4f),
        };

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            if (GameObject.Find("ResearchStation") == null)
                return;
            if (FindAnyObjectByType<EnvatoModelsRuntime>() != null)
                return;

            var host = new GameObject("EnvatoModelsRuntime");
            host.AddComponent<EnvatoModelsRuntime>();
        }

        void Start() => StartCoroutine(ApplyAfterLayout());

        System.Collections.IEnumerator ApplyAfterLayout()
        {
            // Wait so compact zone layout runs first.
            yield return null;
            yield return null;
            Apply();
        }

        void Apply()
        {
            DestroyByName("Ambient_Jellyfish");
            DestroyByName("EnvatoFishSchool");
            // Do not destroy authored UnderwaterEnvironment_v1 décor.
            if (GameObject.Find("UnderwaterEnvironment_v1") == null)
                DestroyByName("EnvatoReefDecor");

            // Mission animals — ground art comes from the Editor environment build.
            ReplaceAnimalVisual("Animal_Clownfish", $"{FishFolder}/Clownfish.fbx", 0.5f, FishColors[0]);
            if (!HasVisual("Animal_Clownfish"))
                ReplaceAnimalVisual("Animal_Clownfish", $"{FishFolder}/ZebraClownFish.fbx", 0.5f, FishColors[0]);

            // Loggerhead FBX is authored nose-down (-Y). Rotate so belly is down and head leads swim.
            ReplaceAnimalVisual("Animal_Sea Turtle", $"{AnimalsFolder}/LoggerheadTurtle.FBX", 1.1f,
                new Color(0.35f, 0.7f, 0.4f), localEuler: new Vector3(-90f, 0f, 0f));

            // New_fish meshes are authored nose-down — level any already-placed ambient fish.
            LevelNewFishOrientation();
            PlacePackFish();

            // Authored underwater build already places New_fish — do not add a second school.
            var hasAuthoredReef = GameObject.Find("UnderwaterEnvironment_v1") != null;
            var fishSpawn = 0;
            if (!hasAuthoredReef)
            {
                var school = new GameObject("EnvatoFishSchool");
                var maxSchool = 4;
                var flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
                for (var i = 0; i < SchoolFish.Length && fishSpawn < maxSchool; i++)
                {
                    var prefab = LoadModel($"{NewFishFolder}/{SchoolFish[i]}");
                    if (prefab == null)
                        continue;

                    var pos = new Vector3(
                        Random.Range(-8f, 8f),
                        Random.Range(0.9f, 1.6f),
                        Random.Range(6f, 16f));
                    var root = new GameObject($"SchoolFish_{fishSpawn++}");
                    root.transform.SetParent(school.transform);
                    root.transform.position = pos;
                    var visual = Instantiate(prefab, root.transform);
                    visual.name = "Visual";
                    visual.transform.localPosition = Vector3.zero;
                    // -90 keeps the belly down. Extra 180 yaw turns the head forward.
                    visual.transform.localRotation = Quaternion.Euler(-90f, 0f, 0f);
                    DisableCollidersNow(visual);
                    FitUniformScale(visual, Random.Range(0.32f, 0.48f));
                    ApplyUrpTint(visual, FishColors[i % FishColors.Length]);
                    foreach (var r in visual.GetComponentsInChildren<Renderer>(true))
                        r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

                    var wander = root.AddComponent<AnimalWander>();
                    typeof(AnimalWander).GetField("center", flags)?.SetValue(wander, pos);
                    typeof(AnimalWander).GetField("extents", flags)?.SetValue(wander, new Vector3(2.5f, 0.35f, 2.5f));
                    typeof(AnimalWander).GetField("speed", flags)?.SetValue(wander, Random.Range(0.25f, 0.4f));
                }
            }

            Debug.Log($"[ReefExplorer] Mission/ambient fish ready ({fishSpawn}).");
        }

        static bool NameHas(UnityEngine.Object obj, string a, string b)
        {
            if (obj == null)
                return false;
            return obj.name.IndexOf(a, System.StringComparison.OrdinalIgnoreCase) >= 0
                || obj.name.IndexOf(b, System.StringComparison.OrdinalIgnoreCase) >= 0;
        }

        static bool UsesProtomelas(AnimalWander wander)
        {
            foreach (var filter in wander.GetComponentsInChildren<MeshFilter>(true))
            {
                if (NameHas(filter.sharedMesh, "Protomelas", "Taeniolatus"))
                    return true;
            }

            foreach (var renderer in wander.GetComponentsInChildren<Renderer>(true))
            {
                var mats = renderer.sharedMaterials;
                for (var i = 0; i < mats.Length; i++)
                {
                    var mat = mats[i];
                    if (mat == null)
                        continue;
                    if (NameHas(mat, "Protomelas", "Taeniolatus"))
                        return true;
                    var tex = mat.mainTexture;
                    if (tex == null && mat.HasProperty("_BaseMap"))
                        tex = mat.GetTexture("_BaseMap");
                    if (NameHas(tex, "Protomelas", "Taeniolatus"))
                        return true;
                }
            }

            return false;
        }

        static void LevelNewFishOrientation()
        {
            var flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
            var offsetField = typeof(AnimalWander).GetField("meshEulerOffset", flags);
            if (offsetField == null)
                return;

            foreach (var wander in FindObjectsByType<AnimalWander>(FindObjectsSortMode.None))
            {
                // This mesh is authored nose-up. Pitch +90 lays the head forward and the back up.
                if (UsesProtomelas(wander))
                {
                    offsetField.SetValue(wander, new Vector3(90f, 0f, 0f));
                    var protomelasMesh = wander.transform.Find("Visual");
                    if (protomelasMesh != null)
                        protomelasMesh.localRotation = Quaternion.identity;
                    continue;
                }

                var n = wander.name;
                if (!n.StartsWith("AmbientFish_") && !n.StartsWith("ScenicFish_") &&
                    !n.StartsWith("CentreFish_") && !n.StartsWith("SchoolFish_"))
                    continue;

                // Mesh is on the same object (editor spawn) — use swim offset.
                if (wander.transform.Find("Visual") == null)
                {
                    offsetField.SetValue(wander, new Vector3(-90f, 0f, 0f));
                    continue;
                }

                // SchoolFish root + Visual child — rotate the mesh only, once.
                var visual = wander.transform.Find("Visual");
                if (visual != null)
                    visual.localRotation = Quaternion.Euler(-90f, 0f, 0f);
                offsetField.SetValue(wander, Vector3.zero);
            }
        }

        /// <summary>
        /// Nose is along -X. Anthias, triggerfish, and Protomelas are already upright.
        /// Butterfly is Z-up, so it also pitches -90.
        /// </summary>
        void PlacePackFish()
        {
            var root = GameObject.Find("PackFishSchool");
            if (root == null)
                root = new GameObject("PackFishSchool");

            // Triggerfish nose is on X, so yaw it forward. Protomelas nose is on +Y, so pitch it forward.
            // Anthias nose is -Z. A 90 degree pitch was standing it on its tail, so only yaw 180.
            var forward = new Vector3(0f, -90f, 0f);
            var protomelas = new Vector3(90f, 0f, 0f);
            var butterfly = new Vector3(-90f, 90f, 0f);
            var anthias = new Vector3(0f, 180f, 0f);
            var entries = new[]
            {
                ("Assets/Fish/[FBX] Undualte_Triggerfish/Undualte_Triggerfish.FBX", "PackFish_Trigger_0", forward, new Vector3(1.2f, 1.15f, 4.6f), 0.42f),
                ("Assets/Fish/[FBX] Undualte_Triggerfish/Undualte_Triggerfish.FBX", "PackFish_Trigger_1", forward, new Vector3(-4.8f, 1.2f, 11.2f), 0.42f),
                ("Assets/Fish/[FBX] Protomelas taeniolatus/Protomelas taeniolatus.FBX", "PackFish_Protomelas_0", protomelas, new Vector3(-1.5f, 1.25f, 6.4f), 0.42f),
                ("Assets/Fish/[FBX] Protomelas taeniolatus/Protomelas taeniolatus.FBX", "PackFish_Protomelas_1", protomelas, new Vector3(4.4f, 1.15f, 12.1f), 0.42f),
                ("Assets/Fish/Butterfly/Butterfly.FBX", "PackFish_Butterfly_0", butterfly, new Vector3(2.1f, 1.2f, 5.4f), 0.34f),
                ("Assets/Fish/Butterfly/Butterfly.FBX", "PackFish_Butterfly_1", butterfly, new Vector3(-5.2f, 1.15f, 10.4f), 0.34f),
                ("Assets/Fish/Anthias1/Anthias1.FBX", "PackFish_Anthias_0", anthias, new Vector3(-2.3f, 1.1f, 7.1f), 0.38f),
                ("Assets/Fish/Anthias1/Anthias1.FBX", "PackFish_Anthias_1", anthias, new Vector3(3.2f, 1.2f, 11.6f), 0.38f),
            };

            foreach (var entry in entries)
            {
                if (GameObject.Find(entry.Item2) != null)
                    continue;
                SpawnPackFish(root.transform, entry.Item1, entry.Item2, entry.Item4, entry.Item5, entry.Item3);
            }

            PlaceJellyfish(root.transform);
        }

        void PlaceJellyfish(Transform parent)
        {
            const string path = "Assets/Other_Animals/jelly/Jellyfish.fbx";
            var spots = new[]
            {
                new Vector3(0.4f, 1.7f, 5.8f),
                new Vector3(-3.1f, 1.9f, 9.4f),
            };
            for (var i = 0; i < spots.Length; i++)
            {
                var name = $"PackJelly_{i}";
                if (GameObject.Find(name) != null)
                    continue;
                var prefab = LoadModel(path);
                if (prefab == null)
                    return;

                var jelly = Instantiate(prefab, parent);
                jelly.name = name;
                jelly.transform.position = spots[i];
                jelly.transform.rotation = Quaternion.identity;
                DisableCollidersNow(jelly);
                FitToTarget(jelly, 0.55f);
                StartCoroutine(RefitNextFrame(jelly, 0.55f));
                ApplyUrpTint(jelly, new Color(0.7f, 0.85f, 1f));
                jelly.AddComponent<JellyFloat>();
            }
        }

        void SpawnPackFish(Transform parent, string path, string fishName, Vector3 pos, float size, Vector3 meshEuler)
        {
            var prefab = LoadModel(path);
            if (prefab == null)
                return;

            var fish = new GameObject(fishName);
            fish.transform.SetParent(parent);
            fish.transform.position = pos;
            var visual = Instantiate(prefab, fish.transform);
            visual.name = "Visual";
            visual.transform.localPosition = Vector3.zero;
            visual.transform.localRotation = Quaternion.identity;
            DisableCollidersNow(visual);
            FitToTarget(visual, size);
            StartCoroutine(RefitNextFrame(visual, size));
            ApplyUrpTint(visual, Color.white);
            foreach (var r in visual.GetComponentsInChildren<Renderer>(true))
                r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

            var wander = fish.AddComponent<AnimalWander>();
            var flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
            typeof(AnimalWander).GetField("center", flags)?.SetValue(wander, pos);
            typeof(AnimalWander).GetField("extents", flags)?.SetValue(wander, new Vector3(2.4f, 0.35f, 2.4f));
            typeof(AnimalWander).GetField("speed", flags)?.SetValue(wander, UnityEngine.Random.Range(0.28f, 0.42f));
            typeof(AnimalWander).GetField("meshEulerOffset", flags)?.SetValue(wander, meshEuler);
        }

        static bool HasVisual(string animalName)
        {
            var go = GameObject.Find(animalName);
            return go != null && go.transform.Find("Visual") != null;
        }

        static void SpawnOne(
            Transform parent, string path, Vector3 pos, float size, Color tint, Vector3 extents, float speed)
        {
            var prefab = LoadModel(path);
            if (prefab == null)
                return;
            var root = new GameObject(System.IO.Path.GetFileNameWithoutExtension(path));
            root.transform.SetParent(parent);
            root.transform.position = pos;
            var visual = Instantiate(prefab, root.transform);
            visual.name = "Visual";
            DisableCollidersNow(visual);
            FitUniformScale(visual, size);
            ApplyUrpTint(visual, tint);
            var wander = root.AddComponent<AnimalWander>();
            var flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
            typeof(AnimalWander).GetField("center", flags)?.SetValue(wander, pos);
            typeof(AnimalWander).GetField("extents", flags)?.SetValue(wander, extents);
            typeof(AnimalWander).GetField("speed", flags)?.SetValue(wander, speed);
        }

        static void DestroyByName(string name)
        {
            var go = GameObject.Find(name);
            if (go != null)
                Destroy(go);
        }

        static void ReplaceAnimalVisual(
            string animalName, string assetPath, float targetSize, Color tint, Vector3? localEuler = null)
        {
            var animal = GameObject.Find(animalName);
            if (animal == null)
                return;

            var prefab = LoadModel(assetPath);
            if (prefab == null)
                return;

            for (var i = animal.transform.childCount - 1; i >= 0; i--)
            {
                var child = animal.transform.GetChild(i);
                if (child.name.StartsWith("Label"))
                    continue;
                Destroy(child.gameObject);
            }

            var rootMr = animal.GetComponent<MeshRenderer>();
            if (rootMr != null)
                rootMr.enabled = false;
            var rootMf = animal.GetComponent<MeshFilter>();
            if (rootMf != null)
                Destroy(rootMf);

            var visual = Instantiate(prefab, animal.transform);
            visual.name = "Visual";
            visual.transform.localPosition = Vector3.zero;
            visual.transform.localRotation = localEuler.HasValue
                ? Quaternion.Euler(localEuler.Value)
                : Quaternion.identity;
            DisableCollidersNow(visual);
            FitUniformScale(visual, targetSize);
            ApplyUrpTint(visual, tint);

            var box = animal.GetComponent<BoxCollider>();
            if (box == null)
                box = animal.AddComponent<BoxCollider>();
            box.center = new Vector3(0f, 0.15f, 0f);
            box.size = new Vector3(0.9f, 0.6f, 0.9f);

            var survey = animal.GetComponent<SurveyAnimal>();
            if (survey != null)
            {
                typeof(SurveyAnimal)
                    .GetField("tintRenderers",
                        System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                    ?.SetValue(survey, visual.GetComponentsInChildren<Renderer>());
            }
        }

        static GameObject LoadModel(string assetPath)
        {
#if UNITY_EDITOR
            var go = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
            if (go != null)
                return go;
            var alt = assetPath.Replace(".fbx", ".FBX").Replace(".obj", ".OBJ");
            return AssetDatabase.LoadAssetAtPath<GameObject>(alt);
#else
            return null;
#endif
        }

        static void DisableCollidersNow(GameObject go)
        {
            foreach (var col in go.GetComponentsInChildren<Collider>(true))
            {
                col.enabled = false;
                Destroy(col);
            }
        }

        static void FitUniformScale(GameObject go, float targetSize)
        {
            FitToTarget(go, targetSize);
        }

        /// <summary>
        /// Scales to an exact size. The old 0.01 minimum left the butterfly several metres wide.
        /// </summary>
        static void FitToTarget(GameObject go, float targetSize)
        {
            if (go == null)
                return;
            go.transform.localScale = Vector3.one;
            var bounds = GetWorldBounds(go);
            var current = Mathf.Max(bounds.size.x, bounds.size.y, bounds.size.z);
            if (current < 0.001f)
                return;
            go.transform.localScale = Vector3.one * (targetSize / current);
        }

        System.Collections.IEnumerator RefitNextFrame(GameObject go, float targetSize)
        {
            yield return null;
            FitToTarget(go, targetSize);
        }

        static void ApplyUrpTint(GameObject go, Color tint)
        {
            var shader = Shader.Find("Universal Render Pipeline/Lit")
                         ?? Shader.Find("Universal Render Pipeline/Simple Lit");
            if (shader == null)
                return;

            foreach (var r in go.GetComponentsInChildren<Renderer>(true))
            {
                // Preserve authored albedo maps when the FBX already has textures.
                var shared = r.sharedMaterials;
                var keep = true;
                for (var i = 0; i < shared.Length; i++)
                {
                    var m = shared[i];
                    if (m == null)
                    {
                        keep = false;
                        break;
                    }

                    var hasMap = m.mainTexture != null
                                 || (m.HasProperty("_BaseMap") && m.GetTexture("_BaseMap") != null);
                    if (!hasMap)
                    {
                        keep = false;
                        break;
                    }
                }

                if (keep)
                    continue;

                var mats = new Material[Mathf.Max(1, shared.Length)];
                for (var i = 0; i < mats.Length; i++)
                {
                    mats[i] = new Material(shader);
                    if (mats[i].HasProperty("_BaseColor"))
                        mats[i].SetColor("_BaseColor", tint);
                    if (mats[i].HasProperty("_EmissionColor"))
                        mats[i].SetColor("_EmissionColor", Color.black);
                }

                r.materials = mats;
            }
        }

        static Bounds GetWorldBounds(GameObject go)
        {
            var rends = go.GetComponentsInChildren<Renderer>();
            if (rends.Length == 0)
                return new Bounds(go.transform.position, Vector3.one * 0.2f);
            var b = rends[0].bounds;
            for (var i = 1; i < rends.Length; i++)
                b.Encapsulate(rends[i].bounds);
            return b;
        }
    }

    /// <summary>Keeps a jellyfish bell-up and lets it drift up and down.</summary>
    public sealed class JellyFloat : MonoBehaviour
    {
        Vector3 origin;
        float phase;

        void Start()
        {
            origin = transform.position;
            phase = Random.Range(0f, 6.28f);
        }

        void Update()
        {
            var p = origin;
            p.y += Mathf.Sin(Time.time * 0.55f + phase) * 0.22f;
            transform.position = p;
        }
    }
}
