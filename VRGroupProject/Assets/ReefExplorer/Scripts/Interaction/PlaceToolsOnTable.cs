using System.Collections;
using UnityEngine;

namespace ReefExplorer.Interaction
{
    /// <summary>
    /// Keeps mission tools resting on the station console at start so they do not fall under the table.
    /// </summary>
    public sealed class PlaceToolsOnTable : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            if (FindAnyObjectByType<PlaceToolsOnTable>() != null)
                return;
            if (GameObject.Find("ResearchStation") == null)
                return;

            var go = new GameObject("PlaceToolsOnTable");
            go.AddComponent<PlaceToolsOnTable>();
        }

        void Start()
        {
            var practice = GameObject.Find("PracticeBuoy");
            if (practice != null)
                practice.SetActive(false);

            StartCoroutine(ArrangeWhenReady());
        }

        IEnumerator ArrangeWhenReady()
        {
            yield return null;
            yield return null;
            yield return null;
            yield return null;
            Arrange();
        }

        public static void Arrange()
        {
            var console = GameObject.Find("Console");
            if (console != null)
            {
                console.transform.localScale = new Vector3(3.8f, 0.18f, 1.25f);
                console.transform.localPosition = new Vector3(0f, 0.95f, -2.05f);
            }

            // Front row, with a clear gap between each tool.
            Put("PowerCell", new Vector3(-1.45f, 1.28f, -1.65f));
            Put("Scanner", new Vector3(-0.45f, 1.12f, -1.65f));
            Put("SampleBottle", new Vector3(0.55f, 1.14f, -1.65f));
            Put("ToxinDisposalTool", new Vector3(1.5f, 1.05f, -1.65f));

            // Back pair sits just behind the front row, in the gaps.
            Put("RecommendationMarker", new Vector3(-0.95f, 1.12f, -2.2f));
            var analyser = Put("SampleAnalyser", new Vector3(0.05f, 1.16f, -2.2f));
            if (analyser != null)
                analyser.transform.localScale = new Vector3(0.22f, 0.2f, 0.22f);
            Put("SampleCrate", new Vector3(1.05f, 1.05f, -2.2f));
            Put("VehiclePowerPack_1", new Vector3(-1.62f, 1.46f, -2.42f));

            var anchor = GameObject.Find("PowerCell_RespawnAnchor");
            var cell = GameObject.Find("PowerCell");
            if (anchor != null && cell != null)
                anchor.transform.SetPositionAndRotation(cell.transform.position, cell.transform.rotation);
        }

        static GameObject Put(string name, Vector3 worldPos)
        {
            var go = GameObject.Find(name);
            if (go == null || !go.activeInHierarchy)
                return null;
            go.transform.SetPositionAndRotation(worldPos, Quaternion.identity);
            var body = go.GetComponent<Rigidbody>();
            if (body != null)
                RigidbodyUtil.ParkKinematic(body);
            return go;
        }
    }
}
