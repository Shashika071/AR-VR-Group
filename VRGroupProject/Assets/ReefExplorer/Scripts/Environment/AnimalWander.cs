using UnityEngine;

namespace ReefExplorer.Environment
{
    public sealed class AnimalWander : MonoBehaviour
    {
        [SerializeField] Vector3 center;
        [SerializeField] Vector3 extents = new Vector3(3f, 0.6f, 3f);
        [SerializeField] float speed = 0.6f;
        [SerializeField] float turnSpeed = 1.2f;

        Vector3 target;

        void Start()
        {
            if (center == Vector3.zero)
                center = transform.position;
            PickTarget();
        }

        void Update()
        {
            var to = target - transform.position;
            if (to.sqrMagnitude < 0.05f)
            {
                PickTarget();
                return;
            }

            var desired = Quaternion.LookRotation(to.normalized, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, desired, turnSpeed * Time.deltaTime);
            transform.position += transform.forward * (speed * Time.deltaTime);
        }

        void PickTarget()
        {
            target = center + new Vector3(
                Random.Range(-extents.x, extents.x),
                Random.Range(-extents.y, extents.y),
                Random.Range(-extents.z, extents.z));
        }

        void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0.3f, 0.8f, 1f, 0.35f);
            Gizmos.DrawWireCube(center == Vector3.zero ? transform.position : center, extents * 2f);
        }
    }
}
