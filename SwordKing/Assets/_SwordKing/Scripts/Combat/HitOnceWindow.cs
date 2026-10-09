using System.Collections.Generic;

namespace SwordKing
{
    // A sustained attack may acquire new targets, but each target is hit once.
    public sealed class HitOnceWindow
    {
        readonly HashSet<int> targets = new HashSet<int>();
        bool open;
        float until;
        public void Begin(float now,float duration) { targets.Clear(); until=now+duration; open=true; }
        public bool HasHit(int target) => targets.Contains(target);
        public bool Active(float now) => open && now<until;
        public bool TryHit(int target,float now) => Active(now) && targets.Add(target);
        public void Cancel() { open=false; targets.Clear(); }
    }
}
