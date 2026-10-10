using System.Collections.Generic;
using System.IO;
using System.Linq;
using ReefExplorer.Environment;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace ReefExplorer.EditorTools
{
    public static class PlayerAssetCatalogBuilder
    {
        const string CatalogPath = "Assets/ReefExplorer/Resources/PlayerAssetCatalog.asset";

        [MenuItem("Reef Rescue/Build Windows")]
        public static void BuildWindows()
        {
            BuildCatalog();
            var scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray();
            var project = Directory.GetParent(Application.dataPath).FullName;
            var dir = Path.Combine(project, "Builds", "ReefRescue");
            Directory.CreateDirectory(dir);
            var exe = Path.Combine(dir, "ReefRescue.exe");
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = exe,
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.None
            });

            if (report.summary.result != BuildResult.Succeeded)
                throw new System.Exception("[ReefExplorer] Build failed: " + report.summary.result);

            var boot = Path.Combine(dir, "ReefRescue_Data", "boot.config");
            if (File.Exists(boot))
            {
                var lines = File.ReadAllLines(boot).Where(line =>
                    line.IndexOf("xr", System.StringComparison.OrdinalIgnoreCase) < 0).ToArray();
                File.WriteAllLines(boot, lines);
            }

            Debug.Log("[ReefExplorer] Build succeeded: " + exe);
        }

        public static void BuildCatalog()
        {
            if (!AssetDatabase.IsValidFolder("Assets/ReefExplorer/Resources"))
                AssetDatabase.CreateFolder("Assets/ReefExplorer", "Resources");

            var paths = new HashSet<string>();
            AddFolder("Assets/Fish", paths);
            AddFolder("Assets/New_fish", paths);
            AddFolder("Assets/Corals", paths);
            AddFolder("Assets/Other_Animals", paths);
            AddFolder("Assets/new_item", paths);
            AddFolder("Assets/Water_bubles", paths);
            AddFolder("Assets/underwater-sound", paths);
            AddFolder("Assets/ReefExplorer/Audio", paths);
            AddFolder("Assets/Sea_Star", paths);
            AddExplicit("Assets/submarinespaceship_nautilus-31.glb", paths);
            AddExplicit("Assets/Car Battery.glb", paths);
            AddExplicit("Assets/Car Battery.obj", paths);
            AddExplicit("Assets/Black_Trash_Bag.fbx", paths);
            AddExplicit("Assets/[FBX] AAA Battery 03/AAA Battery 03.FBX", paths);

            var models = new List<PlayerAssetCatalog.ModelEntry>();
            var clips = new List<PlayerAssetCatalog.ClipEntry>();
            var textures = new List<PlayerAssetCatalog.TextureEntry>();
            foreach (var path in paths)
            {
                var ext = Path.GetExtension(path).ToLowerInvariant();
                if (ext is ".fbx" or ".obj" or ".glb" or ".gltf" or ".prefab")
                {
                    var model = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                    if (model != null)
                        models.Add(new PlayerAssetCatalog.ModelEntry { path = path, asset = model });
                }
                else if (ext is ".wav" or ".mp3" or ".ogg")
                {
                    var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(path);
                    if (clip != null)
                        clips.Add(new PlayerAssetCatalog.ClipEntry { path = path, asset = clip });
                }
                else if (ext is ".png" or ".jpg" or ".jpeg" or ".tga")
                {
                    var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
                    if (texture != null)
                        textures.Add(new PlayerAssetCatalog.TextureEntry { path = path, asset = texture });
                }
            }

            var catalog = AssetDatabase.LoadAssetAtPath<PlayerAssetCatalog>(CatalogPath);
            if (catalog == null)
            {
                catalog = ScriptableObject.CreateInstance<PlayerAssetCatalog>();
                AssetDatabase.CreateAsset(catalog, CatalogPath);
            }

            catalog.models = models.ToArray();
            catalog.clips = clips.ToArray();
            catalog.textures = textures.ToArray();
            EditorUtility.SetDirty(catalog);
            AssetDatabase.SaveAssets();
            Debug.Log($"[ReefExplorer] Packed {models.Count} models, {clips.Count} sounds, {textures.Count} textures.");
        }

        static void AddFolder(string folder, HashSet<string> paths)
        {
            if (!AssetDatabase.IsValidFolder(folder))
                return;
            foreach (var guid in AssetDatabase.FindAssets(string.Empty, new[] { folder }))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                if (!string.IsNullOrEmpty(path) && !AssetDatabase.IsValidFolder(path))
                    paths.Add(path);
            }
        }

        static void AddExplicit(string path, HashSet<string> paths)
        {
            if (AssetDatabase.LoadAssetAtPath<Object>(path) != null)
                paths.Add(path);
        }
    }
}
