// Original, synthesized school ambience and pursuit score. No downloaded audio.
// An AudioContext is created only by unlock(), called from the Start button.
export class SchoolAudio {
  constructor() {
    this.context=null;this.started=false;this.unlocked=false;this.muted=false;this.volume=.4;
    this.paused=false;this.chaseIntensity=0;this.lastEvent=0;this.stepClock=0;this.beatClock=0;
    this.enemyStepClock=0;this.breathClock=0;this.watchClock=0;this.watchBpm=0;this.watchDistance=Infinity;this.watchEquipped=false;this.voices=new Set();this.continuous=[];
    this.stats={scheduledVoices:0,footsteps:0,stairSteps:0,doorOpen:0,doorClose:0,enemyDoorOpen:0,enemyDoorClose:0,enemyStairSteps:0,stairTransitions:0,chaseBeats:0,caughtStingers:0,processedEvents:0,watchBeats:0,searchSounds:0,itemSounds:0};
    this.chaseTempo=192;this.chasePulse=0;this.monsterClock=0;this.growlCooldown=0;this.wasAlerted=false;this.stats.chasePianoNotes=0;this.stats.chaseBars=0;this.stats.monsterGrowls=0;this.stats.monsterBreaths=0;this.stats.monsterSteps=0;this.musicClock=0;this.musicStep=0;this.musicTheme='indoor';this.musicNotes=0;this.maxVoices=48;this.disposed=false;this._resumePending=false;
    this._visibility=()=>{if(document.hidden)this._silenceAndSuspend()};
    document.addEventListener('visibilitychange',this._visibility);
  }
  async unlock() {
    if(this.disposed)return false;
    try {
      if(!this.context){
        const Constructor=globalThis.AudioContext||globalThis.webkitAudioContext;
        if(!Constructor)return false;
        const c=this.context=new Constructor();
        this.master=c.createGain();this.master.gain.value=0;
        this.compressor=c.createDynamicsCompressor();this.compressor.threshold.value=-20;this.compressor.knee.value=20;this.compressor.ratio.value=4;
        this.compressor.attack.value=.01;this.compressor.release.value=.18;
        this.analyser=c.createAnalyser();this.analyser.fftSize=512;this.samples=new Float32Array(512);
        this.master.connect(this.compressor);this.compressor.connect(this.analyser);this.analyser.connect(c.destination);
        this.noise=this._noiseBuffer(4,0x714535);
        this.ambient=c.createGain();this.ambient.gain.value=0;this.ambient.connect(this.master);
        const wind=c.createBufferSource(),filter=c.createBiquadFilter();wind.buffer=this.noise;wind.loop=true;
        filter.type='lowpass';filter.frequency.value=310;filter.Q.value=.3;
        wind.connect(filter);filter.connect(this.ambient);wind.start();this.continuous.push(wind,filter);
        for(const [frequency,gain] of [[47,0],[71,0]])this._loopTone(frequency,gain,this.master);
        this.chaseBus=c.createGain();this.chaseBus.gain.value=0;this.chaseBus.connect(this.master);
        this._loopTone(55,.12,this.chaseBus);this._loopTone(82.5,.035,this.chaseBus);
        this.breathBus=c.createGain();this.breathBus.gain.value=0;this.breathBus.connect(this.master);
        const breath=c.createBufferSource(),breathFilter=c.createBiquadFilter();breath.buffer=this.noise;breath.loop=true;
        breathFilter.type='bandpass';breathFilter.frequency.value=650;breathFilter.Q.value=.7;
        breath.connect(breathFilter);breathFilter.connect(this.breathBus);breath.start();this.continuous.push(breath,breathFilter);
      }
      await this.context.resume();
      this.unlocked=this.context.state==='running';this.started=true;
      this._masterTarget();return this.unlocked;
    } catch(error) {
      // Audio is optional; an unavailable device must not stop the game.
      this.lastError=String(error?.message||error);return false;
    }
  }
  _noiseBuffer(seconds,seed) {
    const c=this.context,length=Math.floor(c.sampleRate*seconds),buffer=c.createBuffer(1,length,c.sampleRate),out=buffer.getChannelData(0);
    let value=seed|0,low=0;
    for(let i=0;i<length;i++){value^=value<<13;value^=value>>>17;value^=value<<5;const n=(value>>>0)/2147483648-1;low=low*.72+n*.28;out[i]=low;}
    // Match both ends to zero with a short smooth taper: no loop click.
    const edge=Math.floor(c.sampleRate*.045);
    for(let i=0;i<edge;i++){const a=(1-Math.cos(Math.PI*i/edge))*.5;out[i]*=a;out[length-1-i]*=a;}
    return buffer;
  }
  _loopTone(frequency,gain,destination) {
    const c=this.context,o=c.createOscillator(),g=c.createGain();o.type='sine';o.frequency.value=frequency;g.gain.value=gain;
    o.connect(g);g.connect(destination);o.start();this.continuous.push(o,g);
  }
  _track(source,nodes) {
    this.voices.add(source);this.stats.scheduledVoices++;
    source.onended=()=>{this.voices.delete(source);for(const node of nodes)try{node.disconnect()}catch{}};
  }
  _tone(frequency,duration,gain,{end=frequency,type='sine',delay=0}={}) {
    if(!this.context||this.voices.size>=this.maxVoices)return;
    const c=this.context,at=c.currentTime+delay,o=c.createOscillator(),g=c.createGain();
    o.type=type;o.frequency.setValueAtTime(frequency,at);o.frequency.exponentialRampToValueAtTime(Math.max(10,end),at+duration);
    g.gain.setValueAtTime(.0001,at);g.gain.exponentialRampToValueAtTime(Math.max(.0002,gain),at+.012);
    g.gain.exponentialRampToValueAtTime(.0001,at+duration);o.connect(g);g.connect(this.master);
    this._track(o,[o,g]);o.start(at);o.stop(at+duration+.025);
  }
  _noise(duration,gain,frequency=900,delay=0) {
    if(!this.context||this.voices.size>=this.maxVoices)return;
    const c=this.context,at=c.currentTime+delay,s=c.createBufferSource(),filter=c.createBiquadFilter(),g=c.createGain();
    s.buffer=this.noise;filter.type='lowpass';filter.frequency.value=frequency;
    g.gain.setValueAtTime(.0001,at);g.gain.exponentialRampToValueAtTime(Math.max(.0002,gain),at+.009);g.gain.exponentialRampToValueAtTime(.0001,at+duration);
    s.connect(filter);filter.connect(g);g.connect(this.master);this._track(s,[s,filter,g]);
    // Vary the sample offset deterministically without creating another buffer.
    s.start(at,(this.stats.scheduledVoices*.173)%2.5);s.stop(at+duration+.02);
  }
  _piano(midi,gain=.035,decay=1){
    const hz=440*Math.pow(2,(midi-69)/12);this.musicNotes++;
    for(const [harmonic,weight,duration] of [[1,1,2.8],[2,.32,1.7],[3,.12,.8],[4,.055,.4]])this._tone(hz*harmonic,duration*decay,gain*weight,{end:hz*harmonic*.999});
  }
  _step(stairs=false,running=false,enemy=false,delay=0,attenuation=1) {
    const gain=enemy?.075*attenuation:running?.075:.052;if(enemy)this.stats.monsterSteps++;
    this._noise(stairs?.12:.075,gain,stairs?1650:850,delay);
    this._tone(enemy?58:stairs?155:105,stairs?.14:.085,gain*.9,{end:stairs?88:60,delay});
    if(!enemy){this.stats.footsteps++;if(stairs)this.stats.stairSteps++;}
  }
  _door(open,scale=1,enemy=false) {
    if(open){this.stats.doorOpen++;if(enemy)this.stats.enemyDoorOpen++;this._noise(.38,.065*scale,650*scale);this._tone(340,.40,.022*scale,{end:120});this._tone(170,.24,.018*scale,{end:240,delay:.12});}
    else{this.stats.doorClose++;if(enemy)this.stats.enemyDoorClose++;this._noise(.11,.08*scale,1000*scale);this._tone(110,.14,.065*scale,{end:52});}
  }
  _events(events,audible,state={}) {
    for(const event of events||[]){
      const id=Number(event.id??event.serial??0);if(!Number.isFinite(id)||id<=this.lastEvent)continue;
      this.lastEvent=id;this.stats.processedEvents++;
      if(!audible)continue;
      if(event.type==='kitchen-collapse'){this._noise(.9,.13,420);this._tone(48,.8,.12,{end:26});}
      else if(event.type==='kitchen-smash'){this._noise(.18,.065,760);this._tone(65,.18,.05,{end:30});}
      else if(event.type==='locked-door'){this._noise(.15,.04,1600);}
      else if(event.type==='door-open')this._door(true);
      else if(event.type==='door-close')this._door(false);
      else if(['enemy-door','enemy-arrive','enemy-door-open','enemy-door-close'].includes(event.type)){
        const sameSpace=!event.spaceId||event.spaceId===state.spaceId;
        // Behind the door, only an actively linked pursuit is heard, muffled.
        if(!sameSpace&&!(Number(state.chaseIntensity)>.05))continue;
        if(event.kind==='stairs'){this.stats.enemyStairSteps++;this._step(true,false,true);}
        else this._door(event.type==='enemy-door'||event.type==='enemy-door-open',sameSpace?.75:.3,true);
      }
      else if(event.type==='transition'&&event.kind==='stairs'){
        this.stats.stairTransitions++;this._step(true,false,false,0);this._step(true,false,false,.16);
      }else if(event.type==='caught'){
        this.stats.caughtStingers++;this._tone(82,.65,.08,{end:31});this._tone(88,.52,.04,{end:43});this._noise(.28,.045,500);
      }else if(event.type==='search'){
        this.stats.searchSounds++;this._noise(.22,.035,1100);this._tone(175,.17,.018,{end:115});
      }else if(['item-picked','item-dropped','item-used','crafted','equipped','unequipped'].includes(event.type)){
        this.stats.itemSounds++;this._noise(.075,.025,1400);this._tone(event.type==='crafted'?525:320,.13,.018,{end:event.type==='item-dropped'?180:420});
      }else if(event.type==='escaped'){
        for(const [i,note] of [220,277.18,329.63].entries())this._tone(note,1.1,.025,{delay:i*.11});
      }
    }
  }
  _masterTarget() {
    if(!this.context)return;
    const target=this.started&&!this.muted&&!this.paused&&!document.hidden?this.volume:0;
    this.master.gain.setTargetAtTime(target,this.context.currentTime,.025);
  }
  _silenceAndSuspend() {
    if(!this.context)return;
    this.master.gain.cancelScheduledValues(this.context.currentTime);this.master.gain.setValueAtTime(0,this.context.currentTime);
    // A door/step queued before leaving the tab must not play on return.
    for(const source of [...this.voices]){try{source.stop()}catch{}if(source.onended)source.onended();}
    if(this.context.state==='running')this.context.suspend().catch(()=>{});
  }
  setMuted(muted){this.muted=!!muted;this._masterTarget();}
  setVolume(volume){if(Number.isFinite(volume))this.volume=Math.max(0,Math.min(1,volume));this._masterTarget();}
  resetEvents(){this.lastEvent=0;this.stepClock=this.beatClock=this.enemyStepClock=this.watchClock=0;this.watchBpm=0;}
  update(dt,state={}) {
    if(this.disposed)return;
    dt=Math.min(.1,Math.max(0,Number(dt)||0));this.started=!!state.started;
    this.paused=!!state.paused||document.hidden;
    const audible=this.started&&this.unlocked&&!this.paused&&!this.muted&&this.volume>0;
    this._events(state.events,audible,state);
    if(!this.context)return;
    if(this.paused||!this.started){this._silenceAndSuspend();this.stepClock=this.beatClock=this.enemyStepClock=0;return;}
    if(this.context.state==='suspended'&&!this._resumePending){
      this._resumePending=true;this.context.resume().catch(()=>{}).finally(()=>{this._resumePending=false});
    }
    this._masterTarget();
    const chasing=!!state.enemyAlerted&&!state.caught&&!state.won;
    const theme=state.isOutdoor?'outdoor':'indoor';if(this.musicTheme!==theme){this.musicTheme=theme;this.musicStep=0;this.musicClock=0;}
    if(audible&&!chasing&&!state.caught&&!state.won){this.musicClock+=dt;const period=theme==='outdoor'?2.15:1.55;if(this.musicClock>=period){this.musicClock-=period;const pattern=theme==='outdoor'?[45,null,52,48,44,null,47,45]:[57,60,64,63,57,null,56,52];const note=pattern[this.musicStep++%pattern.length];if(note!==null)this._piano(note,theme==='outdoor'?.045:.032);if(this.musicStep%8===1)this._piano(theme==='outdoor'?33:45,.022);}}
    const target=state.caught||state.won?0:Math.max(0,Math.min(1,Number(state.chaseIntensity)||0));
    this.chaseIntensity+=(target-this.chaseIntensity)*(1-Math.exp(-dt*(target>this.chaseIntensity?3.5:1.2)));
    this.chaseBus.gain.setTargetAtTime(this.chaseIntensity*.6,this.context.currentTime,.12);
    this.breathClock+=dt;
    const stamina=Number.isFinite(Number(state.stamina))?Number(state.stamina):100;
    const tired=Math.max(0,Math.min(1,(40-stamina)/40)),breathEnvelope=.35+.65*Math.pow((Math.sin(this.breathClock*3.1)+1)/2,2);
    this.breathBus.gain.setTargetAtTime((state.caught?0:tired)*.075*breathEnvelope,this.context.currentTime,.08);
    this.watchEquipped=!!state.watchEquipped;this.watchDistance=Number(state.enemyDistance);
    this.watchBpm=this.watchEquipped&&this.watchDistance<14&&!state.caught&&!state.won?60+110*(1-Math.max(0,this.watchDistance)/14):0;
    if(!audible){this.stepClock=this.beatClock=this.enemyStepClock=this.watchClock=0;return;}
    if(this.watchBpm>0){
      this.watchClock+=dt;const period=60/this.watchBpm;
      if(this.watchClock>=period){this.watchClock%=period;this.stats.watchBeats++;this._tone(72,.10,.065,{end:43});this._tone(61,.12,.045,{end:39,delay:.10});}
    }else this.watchClock=0;
    const moving=Number(state.speed)>.15&&!state.caught&&!state.won;
    if(moving){
      this.stepClock+=dt;const period=state.isRunning?.285:.435;
      if(this.stepClock>=period){this.stepClock%=period;this._step(!!state.onStairs,!!state.isRunning);}
    }else this.stepClock=0;
    if(state.enemyMoving&&Number(state.enemyDistance)<13){
      this.enemyStepClock+=dt;if(this.enemyStepClock>.52){this.enemyStepClock%=.52;this._step(false,false,true,0,Math.max(.08,1-Number(state.enemyDistance)/15));}
    }else this.enemyStepClock=0;
    // Six eighth-note pulses per bar: three accented quarter-note beats.
    if(chasing){
      if(!this.wasAlerted){this.beatClock=60/this.chaseTempo/2;this.chasePulse=0;}
      this.beatClock+=dt;const pulse=60/this.chaseTempo/2;
      while(this.beatClock>=pulse){
        this.beatClock-=pulse;const step=this.chasePulse%6,bar=Math.floor(this.chasePulse/6);
        const note=[84,87,91,90,86,89][step]+(bar%4===3?1:0);
        this._piano(note,step===0?.065:step%2===0?.046:.032,.22);
        this.stats.chasePianoNotes++;this.lastChaseMidi=note;
        if(step%2===0)this.stats.chaseBeats++;
        if(step===0){this.stats.chaseBars++;this._piano(bar%2?60:57,.04,.32);}
        this.chasePulse++;
      }
    }else{this.beatClock=0;this.chasePulse=0;}
    const distance=Number(state.enemyDistance),near=!!state.enemyPresent&&distance<16&&!state.caught&&!state.won;
    this.growlCooldown=Math.max(0,this.growlCooldown-dt);
    if(near){
      const strength=Math.max(.08,1-distance/18);this.monsterClock+=dt;
      if(chasing&&!this.wasAlerted&&this.growlCooldown===0){
        this.stats.monsterGrowls++;this.growlCooldown=5;
        this._tone(91,.85,.06*strength,{end:38,type:'sawtooth'});
        this._tone(96,.7,.035*strength,{end:43,type:'triangle'});this._noise(.8,.085*strength,430);
      }
      if(this.monsterClock>=(chasing?1.6:3.1)){
        this.monsterClock=0;this.stats.monsterBreaths++;
        this._noise(.75,.065*strength,530);this._tone(47,.55,.035*strength,{end:39,type:'triangle'});
      }
    }else this.monsterClock=0;
    this.wasAlerted=chasing;
  }
  inspect() {
    let rms=0;if(this.analyser&&this.context?.state==='running'){this.analyser.getFloatTimeDomainData(this.samples);for(const value of this.samples)rms+=value*value;rms=Math.sqrt(rms/this.samples.length);}
    const effectiveGain=this.context?.state==='running'&&this.started&&!this.paused&&!this.muted?this.master?.gain.value||0:0;
    return {chaseMeter:'3/4',chaseTempo:this.chaseTempo,lastChaseMidi:this.lastChaseMidi||null,musicTheme:this.musicTheme,musicNotes:this.musicNotes,unlocked:this.unlocked,contextState:this.context?.state||'uninitialized',started:this.started,muted:this.muted,volume:this.volume,paused:this.paused,
      activeVoices:this.voices.size,maxVoices:this.maxVoices,...this.stats,chaseIntensity:this.chaseIntensity,chaseGain:this.chaseBus?.gain.value||0,
      masterGain:this.master?.gain.value||0,effectiveGain,rms,watchEquipped:this.watchEquipped,watchBpm:this.watchBpm,watchDistance:Number.isFinite(this.watchDistance)?this.watchDistance:null,
      ambienceActive:!!this.context&&this.started&&!this.paused&&!this.muted&&this.context.state==='running',lastError:this.lastError||null};
  }
  dispose() {
    if(this.disposed)return;this.disposed=true;
    document.removeEventListener('visibilitychange',this._visibility);
    for(const source of this.voices){try{source.stop()}catch{}}
    for(const node of this.continuous){try{if(node.stop)node.stop();node.disconnect()}catch{}}
    this.voices.clear();if(this.context)this.context.close().catch(()=>{});
  }
}
