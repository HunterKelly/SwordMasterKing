using UnityEngine;

namespace SwordplayPrototype
{
    public partial class BrokenGateLevel
    {
        void CreateAudio()
        {
            audioSource=gameObject.AddComponent<AudioSource>(); audioSource.playOnAwake=false; audioSource.spatialBlend=0;
            wind=gameObject.AddComponent<AudioSource>(); wind.playOnAwake=false; wind.loop=true; wind.spatialBlend=0;
            Tone("swing",.13f,220,.65f); Tone("hit",.11f,130,.75f); Tone("heavy",.23f,65,.8f);
            Tone("hurt",.26f,90,.4f); Tone("roll",.28f,110,.8f); Tone("jump",.13f,300,.2f);
            Tone("tellLow",.16f,340,.02f); Tone("tellHigh",.16f,620,.02f);
            Tone("heal",.65f,700,.01f); Tone("reward",.5f,520,.01f);
            Tone("boss",1.2f,55,.15f); Tone("victory",1.8f,440,.01f);
            int count=44100*4; float[] data=new float[count]; var random=new System.Random(58);
            float smooth=0;
            for(int i=0;i<count;i++)
            {
                smooth=Mathf.Lerp(smooth,(float)random.NextDouble()*2-1,.025f);
                float envelope=Mathf.Sin(Mathf.PI*i/(count-1));
                data[i]=smooth*.6f*envelope;
            }
            var ambient=AudioClip.Create("Fortress wind",count,1,44100,false); ambient.SetData(data,0);
            sounds.Add("wind",ambient); wind.clip=ambient; wind.volume=.12f; wind.Play();
        }
        void Tone(string name,float seconds,float frequency,float noise)
        {
            int count=Mathf.CeilToInt(44100*seconds); float[] data=new float[count];
            var random=new System.Random(count+(int)frequency);
            for(int i=0;i<count;i++)
            {
                float t=i/44100f, phase=t/seconds;
                float envelope=Mathf.Min(1,phase*30)*Mathf.Exp(-4*phase);
                float tone=Mathf.Sin(2*Mathf.PI*frequency*t)+.3f*Mathf.Sin(2*Mathf.PI*frequency*1.5f*t);
                data[i]=((1-noise)*tone+noise*((float)random.NextDouble()*2-1))*envelope*.28f;
            }
            // Remove the tail discontinuity.
            for(int i=Mathf.Max(0,count-256);i<count;i++) data[i]*=(count-1-i)/256f;
            var clip=AudioClip.Create(name,count,1,44100,false); clip.SetData(data,0); sounds.Add(name,clip);
        }
        public void PlaySound(string name)
        {
            AudioClip clip;
            if(audioSource!=null && sounds.TryGetValue(name,out clip)) audioSource.PlayOneShot(clip,name=="swing"?.4f:.75f);
        }
    }
}
