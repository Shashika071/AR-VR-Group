using UnityEngine;

namespace ReefExplorer.Environment
{
    /// <summary>
    /// Spreads mission zones, props, and seabed so the reef feels like open sea,
    /// not one crowded patch. Runs automatically on Play.
    /// </summary>
    public sealed class ReefLayoutRuntimeSpreader : MonoBehaviour
    {
        // Station stays at origin. Zones fan out into distinct reef regions.
        static readonly Vector3 CoralZone = new(-30f, 0f, 24f);
        static readonly Vector3 TurtleZone = new(4f, 0f, 52f);
        static readonly Vector3 RayZone = new(34f, 0f, 30f);
        static readonly Vector3 SamplePoint = new(16f, 0.6f, 40f);

        static readonly Vector3[] ReefPatches =
        {
            new(0f, 0f, 10f),      // near station path
            new(-30f, 0f, 24f),    // coral garden
            new(4f, 0f, 52f),      // turtle meadow
            new(34f, 0f, 30f),     // ray flats
            new(-12f, 0f, 38f),    // mid-left open water
            new(20f, 0f, 18f),     // mid-right shoal
        };

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            if (GameObject.Find("ResearchStation") == null)
                return;
            // New polished scene layout — do not re-scatter props/zones.
            if (GameObject.Find("ReefVisuals_v2") != null)
                return;
            if (FindAnyObjectByType<ReefLayoutRuntimeSpreader>() != null)
                return;

            var host = new GameObject("ReefLayoutRuntimeSpreader");
            host.AddComponent<ReefLayoutRuntimeSpreader>();
        }

        void Start()
        {
            Apply();
        }

        void Apply()
        {
            if (GameObject.Find("ReefVisuals_v2") != null)
                return;

            Move("Zone_Coral", CoralZone);
            Move("Zone_Turtle", TurtleZone);
            Move("Zone_Ray", RayZone);
            Move("SampleZone", SamplePoint);

            ExpandSeabed();
            ExpandBounds();
            SoftenUnderwaterFog();
            ExpandParticles();
            RedistributeProps("Rock_", 3.5f, 0.25f);
            RedistributeProps("Plant_", 2.8f, 0.45f);
            RedistributeProps("Coral_", 2.2f, 0.35f);
            RefreshAnimalWanderCenters();

            Debug.Log("[ReefExplorer] Reef layout spread: zones are far apart across open water.");
        }

        static void ExpandParticles()
        {
            var particles = GameObject.Find("UnderwaterParticles");
            if (particles == null)
                return;

            particles.transform.position = new Vector3(0f, 2f, 30f);
            var ps = particles.GetComponent<ParticleSystem>();
            if (ps == null)
                return;

            var shape = ps.shape;
            shape.scale = new Vector3(70f, 6f, 70f);
            var main = ps.main;
            main.maxParticles = 220;
            var emission = ps.emission;
            emission.rateOverTime = 18f;
        }

        static void Move(string name, Vector3 pos)
        {
            var go = GameObject.Find(name);
            if (go == null)
                return;
            go.transform.position = pos;
        }

        static void ExpandSeabed()
        {
            var seabed = GameObject.Find("Seabed");
            if (seabed == null)
                return;

            // Plane default size is 10x10; scale 14 ≈ 140m walkable sea floor.
            seabed.transform.position = new Vector3(0f, 0f, 28f);
            seabed.transform.localScale = new Vector3(14f, 1f, 14f);
        }

        static void ExpandBounds()
        {
            SetBound("Bound_North", new Vector3(0f, 3f, 72f), new Vector3(100f, 8f, 1f));
            SetBound("Bound_South", new Vector3(0f, 3f, -12f), new Vector3(100f, 8f, 1f));
            SetBound("Bound_East", new Vector3(52f, 3f, 28f), new Vector3(1f, 8f, 90f));
            SetBound("Bound_West", new Vector3(-52f, 3f, 28f), new Vector3(1f, 8f, 90f));
        }

        static void SetBound(string name, Vector3 pos, Vector3 scale)
        {
            var go = GameObject.Find(name);
            if (go == null)
                return;
            go.transform.position = pos;
            go.transform.localScale = scale;
        }

        static void SoftenUnderwaterFog()
        {
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            // Light fog so distant reef still reads, but mid-distance feels underwater.
            RenderSettings.fogDensity = 0.028f;
            RenderSettings.fogColor = new Color(0.04f, 0.26f, 0.36f);
            RenderSettings.ambientLight = new Color(0.1f, 0.26f, 0.34f);
        }

        static void RedistributeProps(string prefix, float radius, float y)
        {
            var all = Object.FindObjectsByType<Transform>();
            var index = 0;
            foreach (var t in all)
            {
                if (t == null || !t.name.StartsWith(prefix))
                    continue;
                if (t.parent != null && t.parent.name.StartsWith(prefix))
                    continue;

                var patch = ReefPatches[index % ReefPatches.Length];
                var angle = (index * 47f) * Mathf.Deg2Rad;
                var dist = 1.2f + (index % 5) * (radius * 0.35f);
                t.position = new Vector3(
                    patch.x + Mathf.Cos(angle) * dist,
                    y,
                    patch.z + Mathf.Sin(angle) * dist);
                index++;
            }
        }

        static void RefreshAnimalWanderCenters()
        {
            foreach (var wander in Object.FindObjectsByType<AnimalWander>())
            {
                if (wander == null)
                    continue;

                // Keep survey animals near their (now moved) zone; widen swim area a bit.
                var animal = wander.GetComponent<ReefExplorer.Interaction.SurveyAnimal>();
                if (animal != null)
                {
                    var fieldC = typeof(AnimalWander).GetField("center",
                        System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    var fieldE = typeof(AnimalWander).GetField("extents",
                        System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    fieldC?.SetValue(wander, wander.transform.position);
                    fieldE?.SetValue(wander, new Vector3(4.5f, 0.7f, 4.5f));
                }
            }
        }
    }
}
