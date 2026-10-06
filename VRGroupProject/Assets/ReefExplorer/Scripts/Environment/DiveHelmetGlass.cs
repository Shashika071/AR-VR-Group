using UnityEngine;

namespace ReefExplorer.Environment
{
    /// <summary>
    /// Clears any leftover helmet overlays. Hard corner quads were causing
    /// visible cross / quadrant seams over the Game view.
    /// </summary>
    public sealed class DiveHelmetGlass : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            if (GameObject.Find("ResearchStation") == null)
                return;
            if (FindAnyObjectByType<DiveHelmetGlass>() != null)
                return;

            var host = new GameObject("DiveHelmetGlassRuntime");
            host.AddComponent<DiveHelmetGlass>();
        }

        void Start()
        {
            // Remove broken 3D helmet rims and UI overlays that sliced the view.
            DestroyNamed("HelmetGlassOverlay");
            foreach (var cam in FindObjectsByType<Camera>(FindObjectsInactive.Include))
            {
                var old = cam.transform.Find("HelmetGlass");
                if (old != null)
                    Destroy(old.gameObject);
            }
        }

        static void DestroyNamed(string name)
        {
            var go = GameObject.Find(name);
            if (go != null)
                Destroy(go);
        }
    }
}
