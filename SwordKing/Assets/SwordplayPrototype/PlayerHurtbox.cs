using UnityEngine;

namespace SwordplayPrototype
{
    // Enemy attacks should resolve these trigger volumes, not the movement controller.
    // For overlap-based attacks use QueryTriggerInteraction.Collide, and deduplicate by Owner.
    public class PlayerHurtbox : MonoBehaviour
    {
        public SwordplayArena Owner { get; private set; }
        public bool IsLowerBody { get; private set; }
        public BoxCollider Volume { get; private set; }

        public void Initialize(SwordplayArena owner, bool isLowerBody, BoxCollider volume)
        {
            Owner = owner; IsLowerBody = isLowerBody; Volume = volume;
        }
        public bool TryHit(float damage)
        {
            return Owner != null && Volume != null && Volume.enabled
                && Owner.TryReceiveDamage(damage, IsLowerBody);
        }
        void OnDrawGizmos()
        {
            if (Owner == null || !Owner.showHurtboxes || Volume == null) return;
            Gizmos.color = Volume.enabled ? (IsLowerBody ? Color.yellow : Color.cyan) : Color.gray;
            Matrix4x4 previous = Gizmos.matrix;
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawWireCube(Volume.center, Volume.size);
            Gizmos.matrix = previous;
        }
    }
}
