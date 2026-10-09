using ReefExplorer.Core;
using UnityEngine;

namespace ReefExplorer.Interaction
{
    /// <summary>
    /// Station bin. Rubbish on the map is dropped here.
    /// </summary>
    public sealed class RubbishBin : MonoBehaviour
    {
        static RubbishBin instance;
        Transform hint;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            if (GameObject.Find("ResearchStation") == null)
                return;
            if (FindAnyObjectByType<RubbishBin>() != null)
                return;
            var host = new GameObject("RubbishBinHost");
            host.AddComponent<RubbishBin>();
        }

        void Start() => Build();

        public static void ShowHint(bool show)
        {
            if (instance == null || instance.hint == null)
                return;
            instance.hint.gameObject.SetActive(show);
        }

        public static bool TryDrop(RubbishItem item, Vector3 from)
        {
            if (instance == null || item == null)
                return false;
            if (Vector3.Distance(from, instance.transform.position) > 3.4f)
                return false;
            if (!item.CollectNow())
            {
                MissionEvents.RaiseFeedback("This rubbish is already in the bin.");
                return false;
            }

            ShowHint(false);
            MissionEvents.RaiseFeedback("Rubbish dropped in the bin.");
            return true;
        }

        void Build()
        {
            var station = GameObject.Find("ResearchStation");
            var spot = station != null
                ? station.transform.TransformPoint(new Vector3(2.15f, 0.12f, -0.7f))
                : new Vector3(2.15f, 0.12f, -0.7f);

            transform.position = spot;
            instance = this;
            var bin = new GameObject("RubbishBin");
            bin.transform.SetParent(transform, false);
            bin.transform.localPosition = Vector3.zero;

            Wall(bin.transform, new Vector3(0f, 0.28f, 0f), new Vector3(0.55f, 0.5f, 0.45f), new Color(0.95f, 0.72f, 0.12f));
            Wall(bin.transform, new Vector3(0f, 0.58f, 0.18f), new Vector3(0.55f, 0.08f, 0.06f), new Color(0.55f, 0.38f, 0.05f));

            var lightGo = new GameObject("Glow");
            lightGo.transform.SetParent(bin.transform, false);
            lightGo.transform.localPosition = new Vector3(0f, 0.7f, 0f);
            var light = lightGo.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = new Color(1f, 0.75f, 0.2f);
            light.intensity = 1.4f;
            light.range = 3.2f;
            light.shadows = LightShadows.None;

            hint = new GameObject("DropHint").transform;
            hint.SetParent(bin.transform, false);
            hint.localPosition = new Vector3(0f, 0.02f, 0f);
            var ring = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            ring.name = "Ring";
            ring.transform.SetParent(hint, false);
            ring.transform.localScale = new Vector3(1.4f, 0.03f, 1.4f);
            var col = ring.GetComponent<Collider>();
            if (col != null)
                Destroy(col);
            var renderer = ring.GetComponent<Renderer>();
            if (renderer != null)
            {
                var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
                var mat = new Material(shader);
                var color = new Color(1f, 0.85f, 0.2f);
                if (mat.HasProperty("_BaseColor"))
                    mat.SetColor("_BaseColor", color);
                mat.color = color;
                renderer.sharedMaterial = mat;
            }

            hint.gameObject.SetActive(false);
        }

        static void Wall(Transform parent, Vector3 pos, Vector3 scale, Color color)
        {
            var wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wall.name = "Bin";
            wall.transform.SetParent(parent, false);
            wall.transform.localPosition = pos;
            wall.transform.localScale = scale;
            var renderer = wall.GetComponent<Renderer>();
            if (renderer == null)
                return;
            var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            var mat = new Material(shader);
            if (mat.HasProperty("_BaseColor"))
                mat.SetColor("_BaseColor", color);
            mat.color = color;
            renderer.sharedMaterial = mat;
        }
    }
}
