mergeInto(LibraryManager.library, {
  $KickBackAudioState: {
    context:null,buffers:{},source:null,musicVoice:null,generation:0,voices:[],
    paused:false,offset:0,startedAt:0,musicBuffer:null,mode:-1,duckUntil:0,closed:false,
    names:['Hit','Whoosh','Block','Parry','Down','Menu','Success','PunchHit','KickHit','HeavyHit','UltimateHit','KickWhoosh','HeavyWhoosh','DodgeWhoosh','UltimateRise'],
    init:function(){
      if(this.context)return;
      this.closed=false;this.context=new (window.AudioContext||window.webkitAudioContext)();
      this.music=this.context.createGain();this.effects=this.context.createGain();
      this.duck=this.context.createGain();this.duck.connect(this.music);
      // Separate music and effect buses preserve the independent user settings.
      this.music.connect(this.context.destination);
      this.compressor=this.context.createDynamicsCompressor();
      this.compressor.threshold.value=-9;this.compressor.knee.value=9;this.compressor.ratio.value=10;
      this.compressor.attack.value=.002;this.compressor.release.value=.085;
      this.limiter=this.context.createWaveShaper();var ceiling=new Float32Array(2049);
      for(var i=0;i<ceiling.length;i++){var sample=i*2/(ceiling.length-1)-1;ceiling[i]=.82*Math.tanh(sample/.82);}
      this.limiter.curve=ceiling;this.limiter.oversample='2x';
      this.effects.connect(this.compressor);this.compressor.connect(this.limiter);this.limiter.connect(this.context.destination);
      var self=this;
      this.unlock=function(){if(self.context&&self.context.state==='suspended')self.context.resume().catch(function(){});};
      document.addEventListener('pointerdown',this.unlock,true);document.addEventListener('keydown',this.unlock,true);
    },
    load:function(name){
      this.init();var self=this,context=this.context;
      if(!this.buffers[name])this.buffers[name]=fetch(new URL('Audio/'+name+'.wav',document.baseURI),{cache:'no-cache'})
        .then(function(r){if(!r.ok)throw new Error(name+' HTTP '+r.status);return r.arrayBuffer();})
        .then(function(data){if(self.context!==context)return null;return context.decodeAudioData(data);})
        .catch(function(error){if(self.context!==context)return null;throw error;});
      return this.buffers[name];
    },
    stopVoice:function(voice){
      if(!voice)return;
      try{voice.source.stop();}catch(_){}
      voice.source.disconnect();if(voice.envelope)voice.envelope.disconnect();
    },
    stopMusic:function(fade){
      var voice=this.musicVoice;this.musicVoice=null;this.source=null;
      if(!voice)return;
      if(fade&&this.context&&!this.paused){
        var now=this.context.currentTime;voice.envelope.gain.cancelScheduledValues(now);
        voice.envelope.gain.setValueAtTime(voice.envelope.gain.value,now);
        voice.envelope.gain.linearRampToValueAtTime(0,now+.1);
        var self=this;setTimeout(function(){self.stopVoice(voice);},110);
      }else this.stopVoice(voice);
    },
    startMusic:function(){
      if(!this.context||this.paused||!this.musicBuffer||this.source)return;
      var source=this.context.createBufferSource(),envelope=this.context.createGain(),now=this.context.currentTime;
      source.buffer=this.musicBuffer;source.loop=true;source.connect(envelope);envelope.connect(this.duck);
      envelope.gain.setValueAtTime(0,now);envelope.gain.linearRampToValueAtTime(1,now+.22);
      this.musicVoice={source:source,envelope:envelope};this.source=source;this.startedAt=now;
      source.start(0,this.offset%this.musicBuffer.duration);this.unlock();
    },
    hitDuck:function(){
      if(!this.context)return;var now=this.context.currentTime;
      this.duck.gain.cancelScheduledValues(now);this.duck.gain.setValueAtTime(.78,now);
      this.duck.gain.setValueAtTime(.78,now+.08);this.duck.gain.linearRampToValueAtTime(1,now+.24);
    },
    fail:function(error){console.error('KICK_BACK_AUDIO_ERROR '+error);}
  },
  KickBackAudio_Music__deps:['$KickBackAudioState'],
  KickBackAudio_Music:function(mode){
    var s=KickBackAudioState;s.init();if(s.mode===mode&&s.musicBuffer)return;
    s.mode=mode;var generation=++s.generation;s.stopMusic(true);s.offset=0;s.musicBuffer=null;
    s.names.forEach(function(name){s.load(name).catch(s.fail);});
    s.load(mode===2?'BossMusic':mode===1?'BattleMusic':'LobbyMusic').then(function(buffer){
      if(!buffer||!s.context||generation!==s.generation)return;
      s.musicBuffer=buffer;setTimeout(function(){if(generation===s.generation)s.startMusic();},120);
    }).catch(s.fail);
  },
  KickBackAudio_Pause__deps:['$KickBackAudioState'],
  KickBackAudio_Pause:function(paused){
    var s=KickBackAudioState;s.init();s.paused=!!paused;
    if(s.paused){
      if(s.source&&s.musicBuffer)s.offset=(s.offset+s.context.currentTime-s.startedAt)%s.musicBuffer.duration;
      s.stopMusic(false);s.voices.slice().forEach(function(v){s.stopVoice(v);});s.voices=[];
    }else s.startMusic();
  },
  KickBackAudio_Play__deps:['$KickBackAudioState'],
  KickBackAudio_Play:function(id,gain,pitch,priority){
    var s=KickBackAudioState;s.init();if(s.paused||!s.names[id])return;
    var generation=s.generation,name=s.names[id],requestedAt=s.context.currentTime;
    s.load(name).then(function(buffer){
      if(!buffer||!s.context||s.paused||generation!==s.generation||s.context.currentTime-requestedAt>.18)return;
      if(s.voices.length>=6){
        var eligible=s.voices.filter(function(v){return v.priority<=priority;});
        eligible.sort(function(a,b){return a.priority-b.priority||a.started-b.started;});
        if(!eligible.length)return;
        var oldest=eligible[0];s.voices.splice(s.voices.indexOf(oldest),1);s.stopVoice(oldest);
      }
      var source=s.context.createBufferSource(),envelope=s.context.createGain();
      source.buffer=buffer;source.playbackRate.value=Math.max(.9,Math.min(1.1,pitch));
      envelope.gain.value=gain;source.connect(envelope);envelope.connect(s.effects);
      var voice={source:source,envelope:envelope,priority:priority,started:s.context.currentTime};s.voices.push(voice);
      source.onended=function(){var i=s.voices.indexOf(voice);if(i>=0)s.voices.splice(i,1);source.disconnect();envelope.disconnect();};
      if(name==='Hit'||/Hit$/.test(name)||name==='Parry'||name==='Down')s.hitDuck();
      source.start();s.unlock();
    }).catch(s.fail);
  },
  KickBackAudio_Volumes__deps:['$KickBackAudioState'],
  KickBackAudio_Volumes:function(master,music,effects){
    var s=KickBackAudioState;s.init();var now=s.context.currentTime;
    // Short ramps prevent zipper clicks while dragging sliders; zero settles well before mute verification.
    s.music.gain.setTargetAtTime(master*music,now,.015);s.effects.gain.setTargetAtTime(master*effects,now,.015);
  },
  KickBackAudio_Close__deps:['$KickBackAudioState'],
  KickBackAudio_Close:function(){
    var s=KickBackAudioState;++s.generation;if(!s.context)return;
    document.removeEventListener('pointerdown',s.unlock,true);document.removeEventListener('keydown',s.unlock,true);
    s.stopMusic(false);s.voices.slice().forEach(function(v){s.stopVoice(v);});s.voices=[];
    s.context.close().catch(function(){});s.context=null;s.source=null;s.buffers={};
    s.paused=false;s.offset=0;s.musicBuffer=null;s.mode=-1;s.closed=true;
  }
});
