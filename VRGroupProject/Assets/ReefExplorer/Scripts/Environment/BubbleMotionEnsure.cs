using UnityEngine;

namespace ReefExplorer.Environment
{
    /// <summary>
    /// Makes sure scene bubble props actually move (Editor builds used to place them static).
    /// </summary>
    public sealed class BubbleMotionEnsure : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            if (GameObject.Find("ResearchStation") == null)
                return;
            if (FindAnyObjectByType<BubbleMotionEnsure>() != null)
                return;

            var host = new GameObject("BubbleMotionEnsure");
            host.AddComponent<BubbleMotionEnsure>();
        }

        void Start()
        {
            var count = 0;
            foreach (var t in FindObjectsByType<Transform>(FindObjectsInactive.Exclude))
            {
                if (t == null)
                    continue;
                var n = t.name;
                if (!n.Contains("Bubble") && !n.Contains("bubble") && !n.Contains("Water_buble"))
                    continue;
                if (t.GetComponent<BubbleDrift>() != null)
                    continue;

                var drift = t.gameObject.AddComponent<BubbleDrift>();
                drift.speed = Random.Range(0.14f, 0.3f);
                drift.wobble = Random.Range(0.18f, 0.4f);
                drift.resetY = Random.Range(3.5f, 5.2f);
                drift.basePos = t.position;
                count++;
            }

            if (count > 0)
                Debug.Log($"[ReefExplorer] Enabled bubble motion on {count} props.");
        }
    }
}
