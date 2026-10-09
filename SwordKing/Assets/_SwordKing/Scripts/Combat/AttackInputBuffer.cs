namespace SwordKing
{
    // One physical press can produce at most one attack. Later presses replace,
    // rather than stack, the pending input; the attack cooldown remains authoritative.
    public sealed class AttackInputBuffer
    {
        public const float Lifetime = .15f;
        bool pending;
        float expires;
        public void Press(float realTime) { pending=true; expires=realTime+Lifetime; }
        public void Clear() { pending=false; }
        public bool Consume(float realTime, bool ready)
        {
            if(!pending) return false;
            if(realTime>expires) { Clear(); return false; }
            if(!ready) return false;
            Clear(); return true;
        }
    }
}
