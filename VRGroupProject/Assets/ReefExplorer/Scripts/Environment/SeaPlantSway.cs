using UnityEngine;

namespace ReefExplorer.Environment
{
    /// <summary>Gentle local sway for seagrass / plant clumps.</summary>
    public sealed class SeaPlantSway : MonoBehaviour
    {
        [SerializeField] float angle = 8f;
        [SerializeField] float speed = 1.1f;
        float phase;
        Quaternion baseRot;

        void Start()
        {
            baseRot = transform.localRotation;
            phase = Random.Range(0f, Mathf.PI * 2f);
            speed *= Random.Range(0.85f, 1.2f);
        }

        void Update()
        {
            if ((Time.frameCount + (int)(phase * 10f)) % 2 != 0)
                return;

            phase += Time.deltaTime * 2f * speed;
            var yaw = Mathf.Sin(phase) * angle;
            var pitch = Mathf.Cos(phase * 0.7f) * (angle * 0.35f);
            transform.localRotation = baseRot * Quaternion.Euler(pitch, yaw, 0f);
        }
    }
}
