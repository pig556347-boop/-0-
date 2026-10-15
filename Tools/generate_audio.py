from pathlib import Path
import numpy as np, wave, uuid
out=Path(__file__).resolve().parents[1]/'Assets/Resources/Audio'; sr=22050;rng=np.random.default_rng(304)
def save(name,x):
 x=np.asarray(x);x=np.clip(x,-.94,.94)
 with wave.open(str(out/(name+'.wav')),'wb') as w:
  w.setnchannels(1);w.setsampwidth(2);w.setframerate(sr);w.writeframes((x*32767).astype('<i2').tobytes())
 (out/(name+'.wav.meta')).write_text('fileFormatVersion: 2\nguid: '+uuid.uuid5(uuid.NAMESPACE_URL,'kickback/audio/'+name).hex+'\n')
def fx(name,dur,kind):
 t=np.arange(int(sr*dur))/sr;n=rng.normal(0,1,len(t));soft=np.convolve(n,np.ones(9)/9,'same')
 if kind=='hit': x=(.35*np.sin(2*np.pi*(95*t-85*t*t))+.3*soft+.12*n)*np.exp(-t*24)
 elif kind=='whoosh': x=soft*.6*np.sin(np.pi*t/dur)**2
 elif kind=='block': x=(.24*np.sin(2*np.pi*420*t)+.16*np.sin(2*np.pi*753*t)+.16*n)*np.exp(-t*26)
 elif kind=='parry': x=sum(.12*np.sin(2*np.pi*f*t) for f in [900,1350,1800])*np.exp(-t*12)+.09*n*np.exp(-t*60)
 elif kind=='down': x=(.5*np.sin(2*np.pi*62*t)+.25*soft)*np.exp(-t*13)
 else:
  f=660 if kind=='menu' else 784
  x=.17*np.sin(2*np.pi*f*t)*np.sin(np.pi*t/dur)**2
  if kind=='success':x+=.13*np.sin(2*np.pi*1175*t)*np.sin(np.pi*t/dur)**2
 save(name,x)
for row in [('Hit',.26,'hit'),('Whoosh',.22,'whoosh'),('Block',.22,'block'),('Parry',.42,'parry'),('Down',.5,'down'),('Menu',.09,'menu'),('Success',.32,'success')]:fx(*row)
# Eight bars in A minor, loop-aligned tempo; quiet original instrumental sketches.
def music(name,bpm,battle):
 beat=60/bpm;dur=32*beat;t=np.arange(round(dur*sr))/sr;x=np.zeros(len(t))
 def add(start,y):
  i=round(start*sr);m=min(len(y),len(x)-i)
  if m>0:x[i:i+m]+=y[:m]
 for bar,notes in enumerate([[220,261.626,329.628],[174.614,220,261.626],[130.813,164.814,195.998],[195.998,246.942,293.665]]*2):
  u=np.arange(round(4*beat*sr))/sr
  env=np.sin(np.pi*np.arange(len(u))/len(u))**1.4
  pad=sum(np.sin(2*np.pi*f*u)+.15*np.sin(2*np.pi*f*2*u) for f in notes)*.024*env
  add(bar*4*beat,pad)
  for b in range(4):
   u=np.arange(round(beat*.8*sr))/sr
   bass=.10*np.sin(2*np.pi*(notes[0]/2)*u)*np.exp(-u*5)
   add((bar*4+b)*beat,bass)
   if battle or b in [0,2]:
    u=np.arange(round(.20*sr))/sr;add((bar*4+b)*beat,.22*np.sin(2*np.pi*(48*u+6*(1-np.exp(-u*35))))*np.exp(-u*23))
   if battle and b in [1,3]:
    u=np.arange(round(.14*sr))/sr;add((bar*4+b)*beat,rng.normal(0,.10,len(u))*np.exp(-u*32))
  for b in range(8 if battle else 4):
   u=np.arange(round(.045*sr))/sr;add((bar*4+b*(.5 if battle else 1))*beat,rng.normal(0,.025,len(u))*np.exp(-u*95))
 save(name,x)
music('LobbyMusic',90,False);music('BattleMusic',120,True)
(out/'README.md').write_text('''# KICK Back temporary original audio\n\nAll nine WAV recordings are generated from original oscillators and seeded noise by `Tools/generate_audio.py`. No samples, commercial recordings, external melodies or third-party music are used. These project-created assets can be used and modified with this game; no third-party attribution is required.\n\nMono 22,050 Hz / 16-bit PCM. Lobby: 90 BPM, eight bars. Battle: 120 BPM, eight bars. Both are instrumental loop sketches, not final commissioned music. Hit, Whoosh, Block, Parry, Down, Menu and Success are separate effects. Music and effects use independent settings; Master controls both. Browser playback begins after user interaction.\n''')
print('Generated',len(list(out.glob('*.wav'))),'WAV files')
