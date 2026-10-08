using UnityEngine;

namespace SwordKing
{
    [RequireComponent(typeof(Camera))]
    public sealed class ThirdPersonCamera : MonoBehaviour
    {
        [SerializeField, Min(.4f)] float followDistance = 5.5f;
        [SerializeField, Min(.01f)] float collisionRadius = .2f;
        [SerializeField] float targetHeight = 1.4f;
        float shakeUntil, shakeDuration, shakeAmount;
        public void AddImpact(float amount, float duration)
        {
            shakeAmount = Mathf.Max(shakeAmount, Mathf.Clamp(amount, 0, 1.5f));
            shakeDuration = Mathf.Max(.01f, duration); shakeUntil = Time.unscaledTime + shakeDuration;
        }
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
            // Rotational feedback keeps the collision-resolved camera position outside walls.
            if (Time.timeScale > 0 && Time.unscaledTime < shakeUntil)
            {
                float envelope = Mathf.Clamp01((shakeUntil - Time.unscaledTime) / shakeDuration);
                float phase = Time.unscaledTime * 95f;
                transform.rotation *= Quaternion.Euler(Mathf.Sin(phase) * shakeAmount * envelope, Mathf.Cos(phase * 1.37f) * shakeAmount * envelope, 0);
            }
            else if (Time.unscaledTime >= shakeUntil) shakeAmount = 0;

        }
    }
}
