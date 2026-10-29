using System;
using System.Collections.Generic;
using UnityEngine;

namespace SchoolEscape
{
    // Entirely original synthesized audio. No downloaded music or SFX are used.
    // Clips are created in Awake, but every source stays silent until StartAudio.
    public sealed class SchoolAudio : MonoBehaviour
    {
        public bool Started { get; private set; }
        public bool Muted { get; private set; }
        public bool Paused { get; private set; }
        public float Volume { get; private set; } = .4f;
        public int Footsteps { get; private set; }
        public int DoorSounds { get; private set; }
        public int StairSounds { get; private set; }
        public int CaughtSounds { get; private set; }
        public int ActiveVoiceCount
        {
            get { int count=0;foreach(var source in voices)if(source!=null&&source.isPlaying)count++;return count; }
        }
        public bool OnStairs;
        AudioSource ambience, chase, breath;
        AudioSource[] voices;
        AudioClip ambientClip, chaseClip, breathClip, footClip, runClip, stairClip, doorOpenClip, doorCloseClip, caughtClip, heartbeatClip;
        float stepClock, breathClock,heartbeatClock;AudioClip outdoorClip;bool outside,musicRoom;
        bool wasCaught;
        const int Rate = 24000;

        void Awake()
        {
            voices=new AudioSource[10];for(int i=0;i<voices.Length;i++)voices[i]=Source("School sound voice "+i,false);
            ambience=Source("School ambience",true);chase=Source("Original pursuit score",true);breath=Source("Tired breathing",true);
            ambientClip=Clip("Original indoor piano",12,(t,n)=>{int[] notes={57,60,64,63,57,56,52,55};float beat=t%1.5f,hz=440*Mathf.Pow(2,(notes[(int)(t/1.5f)%8]-69)/12f);return .18f*Mathf.Exp(-beat*3)*(Sin(hz*t)+.24f*Sin(hz*2*t)+.1f*Sin(hz*3*t));},0x5314,true);
            outdoorClip=Clip("Original outdoor piano",16,(t,n)=>{int[] notes={45,52,48,44,47,45,40,43};float beat=t%2,hz=440*Mathf.Pow(2,(notes[(int)(t/2)%8]-69)/12f);return .2f*Mathf.Exp(-beat*2)*(Sin(hz*t)+.2f*Sin(hz*2*t));},0x4411,true);
            chaseClip=Clip("Original 3/4 urgent piano 192 BPM",7.5f,(t,n)=>{float step=60f/192/2,beat=t%step;int index=(int)(t/step),note=new[]{84,87,91,90,86,89}[index%6];float hz=440*Mathf.Pow(2,(note-69)/12f);return (index%6==0?.25f:.17f)*Mathf.Exp(-beat*18)*(Sin(hz*t)+.28f*Sin(hz*2*t));},0x6145,true);
            breathClip=Clip("Original tired breath",4,(t,n)=>
            {
                float envelope=Mathf.Pow((1-Mathf.Cos(t*Mathf.PI))/2,2);
                return n*.25f*envelope;
            },0x4193,true);
            footClip=Impact("Original tile footstep",.12f,105,.35f,.26f,0x4931);
            runClip=Impact("Original running footstep",.13f,92,.42f,.31f,0x3527);
            stairClip=Impact("Original stair tread",.19f,156,.40f,.30f,0x3251);
            doorOpenClip=Clip("Original wood door creak",.55f,(t,n)=>
                Envelope(t,.55f)*(.12f*n+.11f*Sin((310-150*t)*t)+.04f*Sin((171+70*t)*t)),0x2253,false);
            doorCloseClip=Impact("Original wood door latch",.22f,85,.5f,.35f,0x4251);
            caughtClip=Clip("Original low caught accent",.8f,(t,n)=>
                Envelope(t,.8f)*(.20f*Sin((77-35*t)*t)+.10f*Sin((83-25*t)*t)+.025f*n),0x3291,false);
            heartbeatClip=Clip("Original smartwatch heartbeat",.29f,(t,n)=>
            {
                float first=Mathf.Exp(-t*42),second=t>.12f?Mathf.Exp(-(t-.12f)*48):0;
                return .40f*first*Sin(56*t)+.27f*second*Sin(68*t);
            },0x7342,false);
            ambience.clip=ambientClip;chase.clip=chaseClip;breath.clip=breathClip;
            ambience.volume=chase.volume=breath.volume=0;
        }
        AudioSource Source(string label,bool loop)
        {
            var child=new GameObject(label);child.transform.SetParent(transform,false);
            var source=child.AddComponent<AudioSource>();source.playOnAwake=false;source.loop=loop;
            source.spatialBlend=0;source.dopplerLevel=0;source.volume=0;return source;
        }
        static float Sin(float cycles) => Mathf.Sin(2*Mathf.PI*cycles);
        static float Envelope(float t,float duration)
        {
            return Mathf.Clamp01(t/.012f)*Mathf.Pow(Mathf.Clamp01(1-t/duration),2);
        }
        static AudioClip Clip(string label,float duration,Func<float,float,float> sample,uint seed,bool loop)
        {
            int length=Mathf.CeilToInt(duration*Rate);var data=new float[length];float noise=0;uint value=seed;
            for(int i=0;i<length;i++)
            {
                value^=value<<13;value^=value>>17;value^=value<<5;
                float raw=value/(float)uint.MaxValue*2-1;noise=noise*.74f+raw*.26f;
                float t=i/(float)Rate,s=sample(t,noise);
                if(loop)
                {
                    // Short, smooth zero crossing at the loop boundary avoids clicks.
                    float edge=Mathf.Min(t,duration-t),fade=Mathf.Clamp01(edge/.04f);
                    s*=fade*fade*(3-2*fade);
                }
                data[i]=Mathf.Clamp(s,-.8f,.8f);
            }
            var clip=AudioClip.Create(label,length,1,Rate,false);clip.SetData(data,0);return clip;
        }
        static AudioClip Impact(string label,float duration,float hz,float noiseGain,float toneGain,uint seed)
        {
            return Clip(label,duration,(t,n)=>Envelope(t,duration)*
                (n*noiseGain+Sin((hz-35*t)*t)*toneGain),seed,false);
        }
        public void StartAudio()
        {
            if(Started)return;Started=true;
            ambience.Play();chase.Play();breath.Play();
            if(Paused){ambience.Pause();chase.Pause();breath.Pause();}
        }
        public void SetMuted(bool muted)
        {
            Muted=muted;
            // Muting is local to this game and never changes AudioListener.volume.
            if(muted){ambience.volume=chase.volume=breath.volume=0;foreach(var voice in voices)voice.Stop();}
        }
        public void SetVolume(float volume){Volume=Mathf.Clamp01(volume);}
        public void SetPaused(bool paused)
        {
            if(Paused==paused)return;Paused=paused;stepClock=0;
            if(!Started)return;
            if(paused){ambience.Pause();chase.Pause();breath.Pause();foreach(var voice in voices)voice.Stop();}
            else{ambience.UnPause();chase.UnPause();breath.UnPause();}
        }
        void Voice(AudioClip clip,float volume,float pitch=1)
        {
            if(!Started||Muted||Paused||clip==null)return;
            foreach(var source in voices)if(!source.isPlaying)
            {source.clip=clip;source.pitch=pitch;source.volume=Mathf.Clamp01(volume*Volume);source.Play();return;}
        }
        public void PlayDoor(bool open){if(!Started||Paused||Muted)return;DoorSounds++;Voice(open?doorOpenClip:doorCloseClip,open?.52f:.60f);}
        public void PlayStairs(){if(!Started||Paused||Muted)return;StairSounds++;Voice(stairClip,.6f);}
        public void SetWatchProximity(bool equipped,float distance)
        {
            if(!equipped||!Started||Paused||Muted||!float.IsFinite(distance)||distance>16){heartbeatClock=0;return;}
            float proximity=Mathf.Clamp01(1-distance/16),interval=Mathf.Lerp(1.35f,.40f,proximity);
            heartbeatClock+=Mathf.Min(Time.unscaledDeltaTime,.05f);
            if(heartbeatClock>=interval){heartbeatClock%=interval;Voice(heartbeatClip,Mathf.Lerp(.22f,.46f,proximity));}
        }
        public void SetState(float speed,bool running,float stamina,bool chasing,bool caught)
        {
            if(!Started||Paused)return;
            float dt=Mathf.Min(Time.unscaledDeltaTime,.1f),level=Muted?0:Volume;
            ambience.volume=Mathf.MoveTowards(ambience.volume,(musicRoom?0:.18f)*level,dt*.3f);
            chase.volume=Mathf.MoveTowards(chase.volume,chasing&&!caught?.44f*level:0,dt*.5f);
            breathClock+=dt;
            float tired=Mathf.Clamp01((40-stamina)/40),envelope=.35f+.65f*Mathf.Pow((1+Mathf.Sin(breathClock*3.1f))/2,2);
            breath.volume=Mathf.MoveTowards(breath.volume,caught?0:tired*.18f*level*envelope,dt*.25f);
            if(caught&&!wasCaught){CaughtSounds++;Voice(caughtClip,.65f);}
            wasCaught=caught;
            if(speed>.15f&&!caught&&!Muted)
            {
                stepClock+=dt;float period=running?.285f:.435f;
                if(stepClock>=period){stepClock%=period;Footsteps++;Voice(OnStairs?stairClip:running?runClip:footClip,running?.5f:.4f);}
            }
            else stepClock=0;
        }
        readonly Dictionary<string,AudioClip> cues=new Dictionary<string,AudioClip>();float roomClock,enemyClock;
        AudioClip Piano(int midi){string id="piano-"+midi;if(!cues.TryGetValue(id,out var clip)){float hz=440*Mathf.Pow(2,(midi-69)/12f);clip=Clip(id,.8f,(t,n)=>Mathf.Exp(-t*5)*.3f*(Sin(hz*t)+.25f*Sin(2*hz*t)+.12f*Sin(3*hz*t)),12,false);cues[id]=clip;}return clip;}
        public void Environment(bool isOutdoor,bool inMusic){musicRoom=inMusic;if(outside==isOutdoor)return;outside=isOutdoor;ambience.clip=outside?outdoorClip:ambientClip;if(Started&&!Paused)ambience.Play();}
        public void RoomPiano(float bpm){roomClock+=Mathf.Min(Time.deltaTime,.05f);float interval=60/bpm/2;if(roomClock>=interval){roomClock%=interval;Voice(Piano(new[]{69,72,76,74,71,67,70,73}[roomNote++%8]),.2f);}}
        int roomNote;
        public void EnemyStep(bool moving,float distance,float dt){if(!moving||distance>15)return;enemyClock+=dt;if(enemyClock>.48f){enemyClock=0;Voice(stairClip,Mathf.Clamp01(1-distance/16)*.65f,.55f);}}
        public void Cue(string id,bool ui=false)
        {
            if(!Started||Muted||Paused&&!ui)return;
            if(!cues.TryGetValue(id,out var clip)){float hz=id=="keypad"?1180:id=="unlock"?180:id=="monster"?60:id=="discord"?220:130;clip=Clip(id,id=="discord"?.8f:.22f,(t,n)=>Mathf.Exp(-t*(id=="discord"?4:15))*(.16f*Sin(hz*t)+(id=="discord"?.15f*Sin(233*t):.12f*n)),772,false);cues[id]=clip;}
            if(ui&&Paused){foreach(var source in voices)if(!source.isPlaying){source.clip=clip;source.pitch=1;source.volume=.45f*Volume;source.Play();break;}}else Voice(clip,.45f);
        }
        public void ResetState(){wasCaught=false;stepClock=0;heartbeatClock=0;}
        void OnDestroy()
        {
            foreach(var clip in cues.Values)if(clip!=null)Destroy(clip);
            foreach(var clip in new[]{ambientClip,outdoorClip,chaseClip,breathClip,footClip,runClip,stairClip,doorOpenClip,doorCloseClip,caughtClip,heartbeatClip})
                if(clip!=null)Destroy(clip);
        }
    }
}
