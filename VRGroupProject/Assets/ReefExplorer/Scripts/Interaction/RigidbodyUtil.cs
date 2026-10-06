using UnityEngine;

namespace ReefExplorer.Interaction
{
    public static class RigidbodyUtil
    {
        /// <summary>
        /// Clears velocity only when the body is non-kinematic (Unity warns otherwise).
        /// </summary>
        public static void Stop(Rigidbody body)
        {
            if (body == null || body.isKinematic)
                return;

            body.linearVelocity = Vector3.zero;
            body.angularVelocity = Vector3.zero;
        }

        /// <summary>
        /// Park an object safely on a surface: stop physics, then make kinematic.
        /// </summary>
        public static void ParkKinematic(Rigidbody body)
        {
            if (body == null)
                return;

            if (!body.isKinematic)
            {
                body.linearVelocity = Vector3.zero;
                body.angularVelocity = Vector3.zero;
            }

            body.isKinematic = true;
            body.detectCollisions = true;
        }
    }
}
