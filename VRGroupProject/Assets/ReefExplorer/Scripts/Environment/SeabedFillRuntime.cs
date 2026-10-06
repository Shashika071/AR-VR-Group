using UnityEngine;

namespace ReefExplorer.Environment
{
    /// <summary>
    /// Disabled when the Editor tool has built UnderwaterEnvironment_v1.
    /// Prevents runtime grid scatter from undoing intentional reef art.
    /// </summary>
    public sealed class SeabedFillRuntime : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            if (GameObject.Find("ResearchStation") == null)
                return;

            // Prefer the authored underwater build.
            if (GameObject.Find("UnderwaterEnvironment_v1") != null)
            {
                Debug.Log("[ReefExplorer] Using UnderwaterEnvironment_v1 (runtime carpet skipped).");
                return;
            }

            if (FindAnyObjectByType<SeabedFillRuntime>() != null)
                return;

            Debug.LogWarning(
                "[ReefExplorer] No UnderwaterEnvironment_v1 found. " +
                "Run menu: Reef Rescue → Build Underwater Environment");
        }
    }
}
