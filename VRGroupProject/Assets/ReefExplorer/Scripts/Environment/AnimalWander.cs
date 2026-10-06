using UnityEngine;

namespace ReefExplorer.Environment
{
    public sealed class AnimalWander : MonoBehaviour
    {
        [SerializeField] Vector3 center;
        [SerializeField] Vector3 extents = new Vector3(3f, 0.6f, 3f);
        [SerializeField] float speed = 0.6f;
        [SerializeField] float turnSpeed = 1.2f;
        /// <summary>
        /// New_fish / DeepExploration meshes often have nose on -Y instead of +Z.
        /// Apply (-90,0,0) so LookRotation aims the body, not the belly.
        /// </summary>
        [SerializeField] Vector3 meshEulerOffset;

        Vector3 target;
        int updatePhase;

        void Start()
        {
            if (center == Vector3.zero)
                center = transform.position;
            updatePhase = Random.Range(0, 2);
            PickTarget();
        }

        void Update()
        {
            // Cheap throttle — many fish were causing uneven FPS.
            if ((Time.frameCount + updatePhase) % 3 != 0)
                return;

            var to = target - transform.position;
            if (to.sqrMagnitude < 0.05f)
            {
                PickTarget();
                return;
            }

            if (to.sqrMagnitude < 0.0001f)
                return;

            // Flatten pitch a bit so fish glide level instead of diving steeply.
            var dir = to.normalized;
            dir.y = Mathf.Clamp(dir.y, -0.15f, 0.15f);
            dir.Normalize();

            // Recompute each tick so runtime orientation fixes apply after Start().
            var meshOffsetRot = Quaternion.Euler(meshEulerOffset);
            var desired = Quaternion.LookRotation(dir, Vector3.up) * meshOffsetRot;
            var dt = Time.deltaTime * 3f; // compensate for every-3rd-frame
            transform.rotation = Quaternion.Slerp(transform.rotation, desired, turnSpeed * dt);
            var swimForward = (transform.rotation * Quaternion.Inverse(meshOffsetRot)) * Vector3.forward;
            transform.position += swimForward * (speed * dt);
        }

        void PickTarget()
        {
            target = center + new Vector3(
                Random.Range(-extents.x, extents.x),
                Random.Range(-extents.y, extents.y),
                Random.Range(-extents.z, extents.z));
            // Keep fish above the sand (avoid clipping through seabed).
            target.y = Mathf.Max(0.55f, target.y);
        }

        void LateUpdate()
        {
            if ((Time.frameCount + updatePhase) % 3 != 0)
                return;
            if (transform.position.y < 0.5f)
            {
                var p = transform.position;
                p.y = 0.5f;
                transform.position = p;
            }
        }

        void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0.3f, 0.8f, 1f, 0.35f);
            Gizmos.DrawWireCube(center == Vector3.zero ? transform.position : center, extents * 2f);
        }
    }
}
