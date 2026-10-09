using System;

namespace SwordKing
{
    public sealed class StaminaPool
    {
        public float Current { get; private set; }
        public float Maximum { get; }
        readonly float regeneration, delay;
        float waiting;
        public StaminaPool(float maximum=100,float regeneration=60,float delay=.2f)
        { Maximum=Math.Max(1,maximum); this.regeneration=Math.Max(0,regeneration); this.delay=Math.Max(0,delay); Reset(); }
        public void Reset() { Current=Maximum; waiting=0; }
        public bool Spend(float amount)
        {
            amount=Math.Max(0,amount);
            if(Current<amount) return false;
            Current-=amount; waiting=delay; return true;
        }
        public void Drain(float amount)
        {
            if(amount<=0 || Current<=0) return;
            Current=Math.Max(0,Current-amount); waiting=delay;
        }
        public void Tick(float dt,bool canRegenerate)
        {
            if(!canRegenerate || dt<=0) return;
            float remaining=Math.Max(0,dt-waiting);
            waiting=Math.Max(0,waiting-dt);
            Current=Math.Min(Maximum,Current+remaining*regeneration);
        }
    }
}
