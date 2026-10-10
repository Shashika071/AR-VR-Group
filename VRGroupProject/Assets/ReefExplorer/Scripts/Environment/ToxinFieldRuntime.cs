using System.Collections;
using System.Collections.Generic;
using ReefExplorer.Interaction;
using UnityEngine;

namespace ReefExplorer.Environment
{
    /// <summary>
    /// Green toxin cloud over the hazard, with a few fish left dead inside it.
    /// Also hides floating word labels so places are read by sight.
    /// </summary>
    public sealed class ToxinFieldRuntime : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            if (GameObject.Find("ResearchStation") == null)
                return;
            if (FindAnyObjectByType<ToxinFieldRuntime>() != null)
                return;

            var host = new GameObject("ToxinFieldRuntime");
            host.AddComponent<ToxinFieldRuntime>();
        }

        IEnumerator Start()
        {
            // Fish and word labels are spawned a couple of frames after load.
            yield return null;
            yield return null;
            yield return null;
            yield return null;

            HideWordLabels();

            var hazard = FindAnyObjectByType<HazardFlag>();
            var center = hazard != null
                ? hazard.transform.position
                : new Vector3(0f, 0.4f, 15.5f);
            center.y = 1.15f;

            var spots = new[]
            {
                center,
                center + new Vector3(3.4f, 0f, -0.8f),
                center + new Vector3(-2.8f, 0f, 2.6f),
            };
            for (var i = 0; i < spots.Length; i++)
            {
                spots[i].y = 1.15f;
                BuildCloud(spots[i], i == 0 ? "ToxinCloud" : "ToxinCloud_" + i);
            }

            if (ToxinDisposalTool.IsDisposed)
                ToxinPatch.HideAll();
            RemoveRubbishInToxin(spots);
            KillNearbyFish(center);
        }

        public static int HiddenRubbish { get; private set; }

        public static void ResetStatic() => HiddenRubbish = 0;

        static void RemoveRubbishInToxin(Vector3[] spots)
        {
            foreach (var rubbish in FindObjectsByType<RubbishItem>(FindObjectsSortMode.None))
            {
                if (rubbish == null || !rubbish.gameObject.activeInHierarchy)
                    continue;
                foreach (var spot in spots)
                {
                    var flat = rubbish.transform.position;
                    flat.y = spot.y;
                    if (Vector3.Distance(flat, spot) > 2.2f)
                        continue;
                    rubbish.gameObject.SetActive(false);
                    HiddenRubbish++;
                    break;
                }
            }
        }

        static void HideWordLabels()
        {
            foreach (var mesh in FindObjectsByType<TextMesh>(FindObjectsSortMode.None))
            {
                if (mesh != null)
                    mesh.gameObject.SetActive(false);
            }
        }

        static void BuildCloud(Vector3 center, string cloudName)
        {
            if (GameObject.Find(cloudName) != null)
                return;

            var root = new GameObject(cloudName);
            root.transform.position = center;
            root.AddComponent<ToxinPatch>();

            var puffs = new[]
            {
                new Vector3(0f, 0.15f, 0f),
                new Vector3(0.28f, 0.22f, 0.12f),
                new Vector3(-0.22f, 0.18f, -0.16f),
                new Vector3(0.05f, 0.32f, -0.2f),
            };
            foreach (var offset in puffs)
            {
                var puff = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                puff.name = "ToxinPuff";
                puff.transform.SetParent(root.transform, false);
                puff.transform.localPosition = offset;
                puff.transform.localScale = Vector3.one * Random.Range(0.28f, 0.42f);
                var col = puff.GetComponent<Collider>();
                if (col != null)
                    Destroy(col);
                var renderer = puff.GetComponent<Renderer>();
                renderer.sharedMaterial = CloudMaterial(new Color(0.25f, 0.9f, 0.28f, 0.22f));
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            }

            var lightGo = new GameObject("ToxinLight");
            lightGo.transform.SetParent(root.transform, false);
            var light = lightGo.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = new Color(0.4f, 1f, 0.35f);
            light.intensity = 0.8f;
            light.range = 3.2f;
            light.shadows = LightShadows.None;

            var motes = new GameObject("ToxinMotes");
            motes.transform.SetParent(root.transform, false);
            var ps = motes.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.startColor = new Color(0.55f, 1f, 0.4f, 0.7f);
            main.startSize = 0.06f;
            main.startLifetime = 5f;
            main.startSpeed = 0.12f;
            main.maxParticles = 90;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            var emission = ps.emission;
            emission.rateOverTime = 16f;
            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.45f;
            var velocity = ps.velocityOverLifetime;
            velocity.enabled = true;
            velocity.y = 0.22f;
            var rendererPs = motes.GetComponent<ParticleSystemRenderer>();
            rendererPs.renderMode = ParticleSystemRenderMode.Billboard;
            var shader = Shader.Find("Universal Render Pipeline/Particles/Unlit")
                         ?? Shader.Find("Sprites/Default");
            if (shader != null)
            {
                var mat = new Material(shader);
                var circle = SoftCircleTexture(32);
                if (mat.HasProperty("_BaseMap"))
                    mat.SetTexture("_BaseMap", circle);
                if (mat.HasProperty("_MainTex"))
                    mat.SetTexture("_MainTex", circle);
                rendererPs.material = mat;
            }
        }

        static void KillNearbyFish(Vector3 center)
        {
            var fish = new List<AnimalWander>();
            foreach (var wander in FindObjectsByType<AnimalWander>(FindObjectsSortMode.None))
            {
                if (wander.GetComponent<SurveyAnimal>() != null)
                    continue;
                if (wander.GetComponentInParent<SurveyAnimal>() != null)
                    continue;
                fish.Add(wander);
            }

            fish.Sort((a, b) =>
            {
                var da = (a.transform.position - center).sqrMagnitude;
                var db = (b.transform.position - center).sqrMagnitude;
                return da.CompareTo(db);
            });

            var spots = new[]
            {
                center + new Vector3(0.8f, -0.35f, 0.4f),
                center + new Vector3(-1.1f, -0.4f, -0.3f),
                center + new Vector3(0.2f, -0.3f, -1.2f),
            };

            var count = Mathf.Min(3, fish.Count);
            for (var i = 0; i < count; i++)
            {
                var wander = fish[i];
                var go = wander.gameObject;
                wander.enabled = false;
                go.name = "DeadFish_" + i;
                go.transform.position = spots[i];
                go.transform.Rotate(0f, 0f, 180f, Space.World);
                TintDead(go);
            }
        }

        static void TintDead(GameObject go)
        {
            var dead = new Color(0.35f, 0.42f, 0.32f);
            foreach (var renderer in go.GetComponentsInChildren<Renderer>(true))
            {
                var mats = renderer.materials;
                for (var i = 0; i < mats.Length; i++)
                {
                    var mat = mats[i];
                    if (mat == null)
                        continue;
                    if (mat.HasProperty("_BaseColor"))
                        mat.SetColor("_BaseColor", dead);
                    else
                        mat.color = dead;
                    if (mat.HasProperty("_EmissionColor"))
                        mat.SetColor("_EmissionColor", Color.black);
                }
            }
        }

        static Material CloudMaterial(Color color)
        {
            var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            var mat = new Material(shader);
            if (mat.HasProperty("_BaseColor"))
                mat.SetColor("_BaseColor", color);
            else
                mat.color = color;
            if (mat.HasProperty("_Surface"))
            {
                mat.SetFloat("_Surface", 1f);
                mat.SetFloat("_Blend", 0f);
                mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                mat.SetInt("_ZWrite", 0);
                mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                mat.EnableKeyword("_ALPHABLEND_ON");
                mat.renderQueue = 3000;
            }

            return mat;
        }

        static Texture2D SoftCircleTexture(int size)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            var center = (size - 1) * 0.5f;
            for (var y = 0; y < size; y++)
            {
                for (var x = 0; x < size; x++)
                {
                    var d = Vector2.Distance(new Vector2(x, y), new Vector2(center, center)) / center;
                    var a = Mathf.Clamp01(1f - d);
                    a *= a;
                    tex.SetPixel(x, y, new Color(0.6f, 1f, 0.45f, a));
                }
            }

            tex.Apply();
            return tex;
        }
    }
}
