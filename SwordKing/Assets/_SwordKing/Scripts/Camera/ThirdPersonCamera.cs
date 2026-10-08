using UnityEngine;

namespace SwordKing
{
    [RequireComponent(typeof(Camera))]
    public sealed class ThirdPersonCamera : MonoBehaviour
    {
        [SerializeField, Min(.4f)] float followDistance = 5.5f;
        [SerializeField, Min(.01f)] float collisionRadius = .2f;
        [SerializeField] float targetHeight = 1.4f;
        public void Follow(Transform targetTransform, CharacterController playerCollider, float yaw, float pitch)
        {
            Vector3 target = targetTransform.position + Vector3.up * targetHeight;
            Vector3 direction = Quaternion.Euler(pitch, yaw, 0) * Vector3.back;
            float distance = followDistance;
            foreach (var hit in Physics.SphereCastAll(target, collisionRadius, direction, distance, ~0, QueryTriggerInteraction.Ignore))
                if (hit.collider != playerCollider && !hit.collider.transform.IsChildOf(targetTransform))
                    distance = Mathf.Min(distance, Mathf.Max(.4f, hit.distance - .1f));
            transform.position = target + direction * distance;
            transform.LookAt(target);
        }
    }
}
