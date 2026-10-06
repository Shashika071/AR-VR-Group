using UnityEngine;

namespace ReefExplorer.Environment
{
    /// <summary>
    /// Fixes pink/magenta water particles (missing material) and forces a blue drop colour.
    /// </summary>
    public sealed class UnderwaterParticlesFix : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            var particles = GameObject.Find("UnderwaterParticles");
            if (particles == null)
                return;

            if (particles.GetComponent<UnderwaterParticlesFix>() == null)
                particles.AddComponent<UnderwaterParticlesFix>();
        }

        void Start()
        {
            var ps = GetComponent<ParticleSystem>();
            var renderer = GetComponent<ParticleSystemRenderer>();
            if (ps == null || renderer == null)
                return;

            var main = ps.main;
            main.startColor = new Color(0.35f, 0.75f, 1f, 0.55f); // clear blue drops
            main.startSize = 0.04f;

            var mat = CreateBlueParticleMaterial();
            renderer.sharedMaterial = mat;
            renderer.trailMaterial = mat;
        }

        static Material CreateBlueParticleMaterial()
        {
            // Prefer URP particle shaders; fall back safely.
            var shader =
                Shader.Find("Universal Render Pipeline/Particles/Unlit") ??
                Shader.Find("Universal Render Pipeline/Particles/Simple Lit") ??
                Shader.Find("Particles/Standard Unlit") ??
                Shader.Find("Sprites/Default") ??
                Shader.Find("Unlit/Color");

            var mat = new Material(shader);
            var blue = new Color(0.3f, 0.7f, 1f, 0.6f);

            if (mat.HasProperty("_BaseColor"))
                mat.SetColor("_BaseColor", blue);
            if (mat.HasProperty("_Color"))
                mat.SetColor("_Color", blue);
            if (mat.HasProperty("_TintColor"))
                mat.SetColor("_TintColor", blue);

            // Additive-ish soft look when supported.
            if (mat.HasProperty("_Surface"))
                mat.SetFloat("_Surface", 1f); // transparent
            if (mat.HasProperty("_Blend"))
                mat.SetFloat("_Blend", 0f);

            mat.color = blue;
            return mat;
        }
    }
}
