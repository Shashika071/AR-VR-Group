using ReefExplorer.Interaction;
using UnityEditor;
using UnityEngine;

namespace ReefExplorer.EditorTools
{
    /// <summary>
    /// Adds Quaternius Cute Fish Pack (CC0) models onto survey animals.
    /// Menu: Reef Explorer / Apply Cute Fish Pack Models
    /// </summary>
    public static class CuteFishPackInstaller
    {
        const string FbxFolder = "Assets/Cute Fish Pack - Feb 2020/FBX/";

        [MenuItem("Reef Explorer/4. Apply Cute Fish Pack Models")]
        public static void ApplyModels()
        {
            if (EditorApplication.isPlaying)
            {
                EditorUtility.DisplayDialog("Stop Play Mode", "Exit Play Mode first, then run this menu.", "OK");
                return;
            }

            if (!AssetDatabase.IsValidFolder("Assets/Cute Fish Pack - Feb 2020"))
            {
                EditorUtility.DisplayDialog(
                    "Cute Fish Pack missing",
                    "Could not find Assets/Cute Fish Pack - Feb 2020.\nPut the pack in Assets first.",
                    "OK");
                return;
            }

            var clown = ApplyToAnimal("Animal_Clownfish", "Clownfish.fbx", 0.4f, new Vector3(0f, 0.1f, 0f));
            // Pack has no ray mesh — Flatfish is the closest flat swimming shape.
            var ray = ApplyToAnimal("Animal_Ray", "Flatfish.fbx", 0.55f, new Vector3(0f, 0.05f, 0f));
            SpawnScenicFish("BlueTang.fbx", new Vector3(-26f, 1.4f, 28f), 0.35f);
            SpawnScenicFish("YellowTang.fbx", new Vector3(28f, 1.5f, 26f), 0.35f);
            SpawnScenicFish("ParrotFish.fbx", new Vector3(8f, 1.3f, 46f), 0.45f);
            SpawnScenicFish("ButterflyFish.fbx", new Vector3(-8f, 1.6f, 36f), 0.3f);
            SpawnScenicFish("MoorishIdol.fbx", new Vector3(22f, 1.4f, 42f), 0.3f);

            AssetDatabase.SaveAssets();

            EditorUtility.DisplayDialog(
                "Cute Fish Pack applied",
                "Applied:\n" +
                $"• Clownfish model → {(clown ? "OK" : "animal not found (open ReefExplorer scene)")}\n" +
                $"• Flatfish as Ray stand-in → {(ray ? "OK" : "animal not found")}\n" +
                "• Turtle: use menu 5 (Loggerhead FBX)\n" +
                "• Added scenic BlueTang / YellowTang / ParrotFish\n\n" +
                "Licence: CC0 (Quaternius) — recorded in Docs/ASSET_CREDITS.md",
                "OK");
        }

        static bool ApplyToAnimal(string animalName, string fbxFile, float scale, Vector3 localPos)
        {
            var animal = GameObject.Find(animalName);
            if (animal == null)
            {
                Debug.LogWarning($"[ReefExplorer] {animalName} not found. Open ReefExplorer scene first.");
                return false;
            }

            var model = LoadFbx(fbxFile);
            if (model == null)
                return false;

            // Remove old placeholder visuals
            var oldVisual = animal.transform.Find("Visual");
            if (oldVisual != null)
                Object.DestroyImmediate(oldVisual.gameObject);

            var rootRenderer = animal.GetComponent<MeshRenderer>();
            if (rootRenderer != null)
                rootRenderer.enabled = false;

            // Keep a collider on the animal root for scanning
            if (animal.GetComponent<Collider>() == null)
            {
                var capsule = animal.AddComponent<CapsuleCollider>();
                capsule.height = 0.8f;
                capsule.radius = 0.35f;
            }

            var instance = (GameObject)PrefabUtility.InstantiatePrefab(model);
            if (instance == null)
                instance = Object.Instantiate(model);

            instance.name = "CuteFishModel";
            instance.transform.SetParent(animal.transform, false);
            instance.transform.localPosition = localPos;
            instance.transform.localRotation = Quaternion.Euler(0f, 90f, 0f);
            instance.transform.localScale = Vector3.one * scale;

            // Ensure child renderers visible; strip colliders from mesh so scan uses root collider
            foreach (var col in instance.GetComponentsInChildren<Collider>())
                Object.DestroyImmediate(col);

            var survey = animal.GetComponent<SurveyAnimal>();
            if (survey != null)
            {
                var so = new SerializedObject(survey);
                var rends = so.FindProperty("tintRenderers");
                var renderers = instance.GetComponentsInChildren<Renderer>();
                rends.arraySize = renderers.Length;
                for (var i = 0; i < renderers.Length; i++)
                    rends.GetArrayElementAtIndex(i).objectReferenceValue = renderers[i];
                so.ApplyModifiedPropertiesWithoutUndo();
            }

            EditorUtility.SetDirty(animal);
            return true;
        }

        static void SpawnScenicFish(string fbxFile, Vector3 worldPos, float scale)
        {
            var existingName = "Scenic_" + System.IO.Path.GetFileNameWithoutExtension(fbxFile);
            var existing = GameObject.Find(existingName);
            if (existing != null)
                Object.DestroyImmediate(existing);

            var model = LoadFbx(fbxFile);
            if (model == null)
                return;

            var instance = (GameObject)PrefabUtility.InstantiatePrefab(model);
            if (instance == null)
                instance = Object.Instantiate(model);

            instance.name = existingName;
            instance.transform.position = worldPos;
            instance.transform.localScale = Vector3.one * scale;
            foreach (var col in instance.GetComponentsInChildren<Collider>())
                Object.DestroyImmediate(col);

            var wander = instance.AddComponent<ReefExplorer.Environment.AnimalWander>();
            var wso = new SerializedObject(wander);
            wso.FindProperty("center").vector3Value = worldPos;
            wso.FindProperty("extents").vector3Value = new Vector3(5f, 0.8f, 5f);
            wso.FindProperty("speed").floatValue = 0.45f;
            wso.ApplyModifiedPropertiesWithoutUndo();
        }

        static GameObject LoadFbx(string fileName)
        {
            var path = FbxFolder + fileName;
            var model = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (model == null)
                Debug.LogError("[ReefExplorer] Missing model: " + path);
            return model;
        }
    }
}
