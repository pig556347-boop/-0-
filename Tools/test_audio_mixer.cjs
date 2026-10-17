/* Behavioral checks for the production Web Audio bridge without a Unity editor. */
const assert=require('node:assert/strict');
const fs=require('node:fs');
const vm=require('node:vm');
const path=require('node:path');
const root=path.resolve(__dirname,'..');
const timers=[],errors=[],requests=[],contexts=[];
class Param {
  constructor(){this.value=1;}
  setValueAtTime(value){this.value=value;}
  setTargetAtTime(value){this.value=value;}
  linearRampToValueAtTime(value){this.value=value;}
  cancelScheduledValues(){}
}
class Node {
  constructor(context){this.context=context;this.gain=new Param();this.playbackRate=new Param();}
  connect(node){this.connected=node;return node;}
  disconnect(){this.connected=null;}
  start(when=0,offset=0){this.started=true;this.offset=offset;}
  stop(){this.stopped=true;if(this.onended)this.onended();}
}
class Context {
  constructor(){this.currentTime=0;this.state='running';this.destination=new Node(this);contexts.push(this);}
  createGain(){return new Node(this);}
  createWaveShaper(){return new Node(this);}
  createBufferSource(){return new Node(this);}
  createDynamicsCompressor(){const n=new Node(this);for(const k of ['threshold','knee','ratio','attack','release'])n[k]=new Param();return n;}
  decodeAudioData(array){const b=Buffer.from(array);return Promise.resolve({duration:(b.length-44)/b.readUInt32LE(28)});}
  resume(){this.state='running';return Promise.resolve();}
  close(){this.state='closed';return Promise.resolve();}
}
const listeners=new Map();
const scope={LibraryManager:{library:{}},mergeInto:Object.assign,window:{AudioContext:Context},URL,
  document:{baseURI:'https://example.com/KICK/',addEventListener:(k,v)=>listeners.set(k,v),removeEventListener:k=>listeners.delete(k)},
  fetch:async url=>{requests.push(String(url));const bytes=fs.readFileSync(path.join(root,'Assets/Resources/Audio',path.basename(String(url))));return {ok:true,arrayBuffer:async()=>bytes.buffer.slice(bytes.byteOffset,bytes.byteOffset+bytes.length)};},
  console:{error:e=>errors.push(String(e))},setTimeout:fn=>timers.push(fn)};
vm.createContext(scope);vm.runInContext(fs.readFileSync(path.join(root,'Assets/Plugins/WebGL/KickBackAudio.jslib'),'utf8'),scope);
const api=scope.LibraryManager.library,s=scope.KickBackAudioState=api.$KickBackAudioState;
async function settle(){for(let i=0;i<12;i++)await Promise.resolve();}
async function flush(){await settle();while(timers.length)timers.shift()();await settle();}
(async()=>{
  api.KickBackAudio_Music(0);await flush();
  assert.equal(s.mode,0);assert(s.source.started);assert.equal(s.source.loop,true);
  const lobby=s.source;api.KickBackAudio_Music(0);await flush();assert.equal(s.source,lobby,'same mode must not restart the loop');
  api.KickBackAudio_Music(2);await flush();assert.equal(s.mode,2);assert(lobby.stopped);assert(s.source.buffer.duration>29);
  assert(requests.some(r=>r.endsWith('/Audio/BossMusic.wav')));
  for(let i=0;i<9;i++){api.KickBackAudio_Play(7,1,1,1);await settle();}
  assert.equal(s.voices.length,6,'effects are bounded to six voices');
  api.KickBackAudio_Play(3,1,1,3);await settle();assert.equal(s.voices.length,6);assert(s.voices.some(v=>v.priority===3),'parry replaces a low-priority voice');
  s.voices.slice().forEach(v=>s.stopVoice(v));assert.equal(s.voices.length,0);
  for(let i=0;i<6;i++){api.KickBackAudio_Play(3,1,1,3);await settle();}
  const protectedVoices=s.voices.slice();api.KickBackAudio_Play(1,1,1,1);await settle();
  assert(protectedVoices.every(v=>!v.source.stopped),'ordinary swings cannot steal critical parry voices');
  s.context.currentTime=3.75;api.KickBackAudio_Pause(1);assert.equal(s.source,null);assert.equal(s.voices.length,0);assert.equal(s.offset,3.75);
  api.KickBackAudio_Play(7,1,1,1);await settle();assert.equal(s.voices.length,0,'paused combat produces no late effects');
  api.KickBackAudio_Pause(0);assert.equal(s.source.offset,3.75,'resume preserves music position');
  api.KickBackAudio_Volumes(.3,.6,.8);assert(Math.abs(s.music.gain.value-.18)<1e-9);assert(Math.abs(s.effects.gain.value-.24)<1e-9);
  api.KickBackAudio_Volumes(1,0,0);assert.equal(s.music.gain.value,0);assert.equal(s.effects.gain.value,0);
  api.KickBackAudio_Close();assert.equal(s.context,null);assert.equal(listeners.size,0);assert.equal(s.voices.length,0);
  api.KickBackAudio_Music(1);api.KickBackAudio_Close();await flush();assert.equal(errors.length,0,'closing during decode must not produce runtime errors');
  console.log('Audio bridge: behavioral checks passed (music selection, bounded voices, priorities, pause/resume, independent mute, teardown).');
})().catch(e=>{console.error(e);process.exitCode=1;});
