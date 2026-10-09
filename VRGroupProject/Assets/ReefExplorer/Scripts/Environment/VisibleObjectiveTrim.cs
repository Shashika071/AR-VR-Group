using ReefExplorer.Interaction;
using UnityEngine;

namespace ReefExplorer.Environment
{
    /// <summary>
    /// The checklist used to count every site. Two sample circles and one coral marker
    /// are the ones in the dive, so extra copies are turned off.
    /// </summary>
    public sealed class VisibleObjectiveTrim : MonoBehaviour
    {
        const int SampleKeep = 2;
        const int CoralKeep = 1;

        void Start() => Apply();

        public static void Apply()
        {
            TrimSamples();
            TrimCoral();
        }

        static void TrimSamples()
        {
            var zones = FindObjectsByType<SampleZone>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            if (zones.Length <= SampleKeep)
                return;

            System.Array.Sort(zones, (a, b) =>
                Dist2(a.transform.position).CompareTo(Dist2(b.transform.position)));

            for (var i = 0; i < zones.Length; i++)
            {
                if (zones[i] != null)
                    zones[i].gameObject.SetActive(i < SampleKeep);
            }
        }

        static void TrimCoral()
        {
            var points = FindObjectsByType<CoralSurveyPoint>(FindObjectsSortMode.None);
            if (points.Length <= CoralKeep)
                return;

            var garden = new Vector3(-6.5f, 0f, 11f);
            System.Array.Sort(points, (a, b) =>
                (a.transform.position - garden).sqrMagnitude.CompareTo((b.transform.position - garden).sqrMagnitude));

            for (var i = CoralKeep; i < points.Length; i++)
            {
                if (points[i] != null)
                    points[i].gameObject.SetActive(false);
            }
        }

        static float Dist2(Vector3 p)
        {
            var dx = p.x;
            var dz = p.z - 10f;
            return dx * dx + dz * dz;
        }
    }
}
