using UnityEngine;

namespace SwordKing
{
    public sealed class IceWorldAtmosphere : MonoBehaviour
    {
        BrokenGateLevel level;
        AudioSource steps;
        AudioClip[] crunch;
        Vector3 previous;
        float travelled;
        public void Initialize(BrokenGateLevel owner)
        {
            level=owner;
            var snow=new GameObject("Falling snow").AddComponent<ParticleSystem>();
            snow.transform.SetParent(transform,false); snow.transform.position=new Vector3(0,14,0);
            snow.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
            var main=snow.main; main.startLifetime=10; main.startSpeed=0; main.startSize=new ParticleSystem.MinMaxCurve(.035f,.09f);
            main.startColor=new Color(.95f,.98f,1); main.maxParticles=6000; main.simulationSpace=ParticleSystemSimulationSpace.World;
            var shape=snow.shape; shape.shapeType=ParticleSystemShapeType.Box; shape.scale=new Vector3(44,2,52);
            var emission=snow.emission; emission.rateOverTime=owner.Player.LevelDefinition.snowfallRate;
            var velocity=snow.velocityOverLifetime; velocity.enabled=true; velocity.space=ParticleSystemSimulationSpace.World;
            velocity.x=new ParticleSystem.MinMaxCurve(.15f,.45f); velocity.y=new ParticleSystem.MinMaxCurve(-1.8f,-1.2f); velocity.z=new ParticleSystem.MinMaxCurve(-.15f,.15f);
            snow.GetComponent<ParticleSystemRenderer>().sharedMaterial=owner.World.Material(Color.white,true);
            snow.Play();
            steps=gameObject.AddComponent<AudioSource>(); steps.playOnAwake=false; steps.volume=owner.Player.LevelDefinition.snowFootstepVolume;
            crunch=new AudioClip[4];
            for(int n=0;n<crunch.Length;n++)
            {
                int count=11025; var samples=new float[count]; var random=new System.Random(320+n); float smooth=0;
                for(int i=0;i<count;i++)
                {
                    float t=i/44100f; float noise=(float)random.NextDouble()*2-1;
                    smooth=Mathf.Lerp(smooth,noise,.18f);
                    float envelope=Mathf.Sin(Mathf.PI*i/(count-1))*Mathf.Exp(-t*12);
                    samples[i]=(smooth*.75f+noise*.18f)*envelope;
                }
                crunch[n]=AudioClip.Create("Soft snow crunch "+n,count,1,44100,false); crunch[n].SetData(samples,0);
            }
            previous=owner.Player.PlayerTransform.position;
        }
        void Update()
        {
            if(level==null) return;
            Vector3 p=level.Player.PlayerTransform.position, delta=p-previous; previous=p;
            if(level.InputBlocked || level.Player.IsRolling || level.Player.LowerBodyProtected || delta.magnitude>2)
            { travelled=0; return; }
            if(!Physics.Raycast(p+Vector3.up*.2f,Vector3.down,.5f,~0,QueryTriggerInteraction.Ignore)) {travelled=0;return;}
            delta.y=0; travelled+=delta.magnitude;
            if(travelled>=1.6f)
            {
                travelled%=1.6f; steps.pitch=Random.Range(.93f,1.07f);
                steps.PlayOneShot(crunch[Random.Range(0,crunch.Length)]);
            }
        }
        void OnDestroy() { if(crunch!=null) foreach(var clip in crunch) if(clip!=null) Destroy(clip); }
    }
}
