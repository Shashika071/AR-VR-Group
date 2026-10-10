using System;
using UnityEngine;

namespace ReefExplorer.Environment
{
    /// <summary>
    /// Models, sounds, and textures packed into a player build.
    /// The editor still loads the same files from their asset paths.
    /// </summary>
    public sealed class PlayerAssetCatalog : ScriptableObject
    {
        [Serializable]
        public struct ModelEntry
        {
            public string path;
            public GameObject asset;
        }

        [Serializable]
        public struct ClipEntry
        {
            public string path;
            public AudioClip asset;
        }

        [Serializable]
        public struct TextureEntry
        {
            public string path;
            public Texture2D asset;
        }

        public ModelEntry[] models = Array.Empty<ModelEntry>();
        public ClipEntry[] clips = Array.Empty<ClipEntry>();
        public TextureEntry[] textures = Array.Empty<TextureEntry>();

        static PlayerAssetCatalog cached;

        public static GameObject Model(string path)
        {
            var catalog = Get();
            if (catalog == null || catalog.models == null)
                return null;
            foreach (var entry in catalog.models)
            {
                if (entry.asset != null && SamePath(entry.path, path))
                    return entry.asset;
            }

            return null;
        }

        public static AudioClip Clip(string path)
        {
            var catalog = Get();
            if (catalog == null || catalog.clips == null)
                return null;
            foreach (var entry in catalog.clips)
            {
                if (entry.asset != null && SamePath(entry.path, path))
                    return entry.asset;
            }

            return null;
        }

        public static Texture2D Texture(string path)
        {
            var catalog = Get();
            if (catalog == null || catalog.textures == null)
                return null;
            foreach (var entry in catalog.textures)
            {
                if (entry.asset != null && SamePath(entry.path, path))
                    return entry.asset;
            }

            return null;
        }

        static PlayerAssetCatalog Get()
        {
            if (cached == null)
                cached = Resources.Load<PlayerAssetCatalog>("PlayerAssetCatalog");
            return cached;
        }

        static bool SamePath(string stored, string asked)
        {
            if (string.IsNullOrEmpty(stored) || string.IsNullOrEmpty(asked))
                return false;
            if (string.Equals(stored, asked, StringComparison.OrdinalIgnoreCase))
                return true;
            var swapped = asked.Replace(".fbx", ".FBX").Replace(".obj", ".OBJ");
            return string.Equals(stored, swapped, StringComparison.OrdinalIgnoreCase);
        }
    }
}
