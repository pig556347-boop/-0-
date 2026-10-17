"""Rebuild KICK Back's original, sample-free audio palette and loop masters."""
from pathlib import Path
import json
import uuid
import wave
import numpy as np
from scipy.signal import butter, sosfilt

OUT = Path(__file__).resolve().parents[1] / 'Assets/Resources/Audio'
SR = 32000
RNG = np.random.default_rng(304)
AUDIT = {}


def filtered_noise(count, low=80, high=8000):
    noise = RNG.normal(0, 1, count)
    return sosfilt(butter(2, [low, high], fs=SR, btype='bandpass', output='sos'), noise)


def save(name, sound, peak=.68, music=False):
    sound = np.asarray(sound, dtype=np.float64)
    assert np.all(np.isfinite(sound))
    # Remove DC and fade clicks at the outer edge. Musical tails wrap before this.
    sound -= sound.mean(axis=0)
    fade = min(round(SR*.004), len(sound)//4)
    ramp = np.linspace(0, 1, fade)
    sound[:fade] *= ramp[:, None] if sound.ndim == 2 else ramp
    sound[-fade:] *= ramp[::-1, None] if sound.ndim == 2 else ramp[::-1]
    sound *= peak / max(np.max(np.abs(sound)), 1e-9)
    pcm = np.rint(sound*32767).astype('<i2')
    OUT.mkdir(parents=True, exist_ok=True)
    with wave.open(str(OUT/(name+'.wav')), 'wb') as target:
        target.setnchannels(2 if sound.ndim == 2 else 1)
        target.setsampwidth(2)
        target.setframerate(SR)
        target.writeframes(pcm.tobytes())
    meta = OUT/(name+'.wav.meta')
    if not meta.exists():
        meta.write_text('fileFormatVersion: 2\nguid: '+uuid.uuid5(uuid.NAMESPACE_URL, 'kickback/audio/'+name).hex+'\n')
    AUDIT[name] = {'seconds': round(len(sound)/SR, 5), 'channels': 2 if sound.ndim == 2 else 1,
                   'sampleRate': SR, 'peak': round(float(np.max(np.abs(sound))), 5),
                   'rms': round(float(np.sqrt(np.mean(sound**2))), 5),
                   'seamStep': round(float(np.max(np.abs(sound[-1]-sound[0]))), 7),
                   'clippedSamples': int(np.count_nonzero(np.abs(pcm)>=32767)), 'music': music}


def impact(name, kind):
    duration = {'punch': .24, 'kick': .31, 'heavy': .40, 'ultimate': .47, 'down': .60}[kind]
    t = np.arange(round(SR*duration))/SR
    scale = {'punch': 1, 'kick': 1.15, 'heavy': 1.4, 'ultimate': 1.65, 'down': 1.3}[kind]
    freq = {'punch': 155, 'kick': 115, 'heavy': 85, 'ultimate': 72, 'down': 58}[kind]
    # Immediate dry contact followed by low body and short clothing scrape; no gun-like crack.
    body = np.sin(2*np.pi*(freq*t + 2.6*(1-np.exp(-t*65))))*np.exp(-t*(24/scale))
    snap = filtered_noise(len(t), 500, 6500)*np.exp(-t*110)
    cloth = filtered_noise(len(t), 150, 1900)*np.exp(-t*(35/scale))
    sound = .54*body + .36*snap + .24*cloth
    if kind in ('heavy', 'ultimate', 'down'):
        delay = round(.035*SR)
        sound[delay:] += .14*filtered_noise(len(t)-delay, 70, 900)*np.exp(-t[:-delay]*20)
    if kind == 'ultimate':
        sound += .07*np.sin(2*np.pi*365*t)*np.exp(-t*16)
    save(name, np.tanh(sound*1.3), .61 if kind in ('heavy', 'ultimate', 'down') else .54)


for name, kind in [('Hit', 'punch'), ('PunchHit', 'punch'), ('KickHit', 'kick'), ('HeavyHit', 'heavy'),
                   ('UltimateHit', 'ultimate'), ('Down', 'down')]:
    impact(name, kind)

for name, duration, low, high, peak in [('Whoosh', .17, 700, 6800, .34), ('KickWhoosh', .23, 280, 6000, .39),
                                      ('HeavyWhoosh', .29, 180, 4700, .43), ('DodgeWhoosh', .22, 950, 7000, .25)]:
    t = np.arange(round(SR*duration))/SR
    envelope = np.sin(np.pi*t/duration)**2
    save(name, filtered_noise(len(t), low, high)*envelope, peak)

for name, duration in [('Block', .18), ('Parry', .35), ('Menu', .09), ('Success', .43), ('UltimateRise', .38)]:
    t = np.arange(round(SR*duration))/SR
    if name == 'Block':
        sound = .45*filtered_noise(len(t), 350, 4200)*np.exp(-t*65) + .32*np.sin(2*np.pi*265*t)*np.exp(-t*32)
    elif name == 'Parry':
        sound = sum(.15*np.sin(2*np.pi*f*t)*np.exp(-t*d) for f, d in [(860, 14), (1290, 18), (2150, 25)])
        sound += .2*filtered_noise(len(t), 900, 7500)*np.exp(-t*95)
    elif name == 'UltimateRise':
        sound = .3*filtered_noise(len(t), 300, 5200)*np.sin(np.pi*t/duration)**1.6
        sound += .13*np.sin(2*np.pi*(220*t+700*t*t))*np.sin(np.pi*t/duration)**2
    else:
        freqs = [660, 990] if name == 'Menu' else [587.33, 880, 1174.66]
        sound = sum(.12*np.sin(2*np.pi*f*t) for f in freqs)*np.sin(np.pi*t/duration)**2
    save(name, sound, .42 if name == 'Parry' else .35)


def score(name, bpm, boss=False, lobby=False):
    """Original 16-bar tension score (boss) or eight-bar restrained school-action loop."""
    beat = 60/bpm
    bars = 16 if boss else 8
    length = round(bars*4*beat*SR)
    sound = np.zeros((length, 2))

    def add(start, value, pan=0, echo=0):
        value = np.asarray(value)
        idx = (np.arange(len(value)) + round(start*SR)) % length
        stereo = np.column_stack((value*np.sqrt((1-pan)/2), value*np.sqrt((1+pan)/2)))
        np.add.at(sound, idx, stereo)
        if echo:
            np.add.at(sound, (idx+round(beat*.75*SR)) % length, stereo[:, ::-1]*echo)

    def tone(freq, duration, level, decay=3, bright=.12):
        t = np.arange(round(duration*SR))/SR
        attack = np.minimum(t/.012, 1)
        return level*(np.sin(2*np.pi*freq*t)+bright*np.sin(2*np.pi*2*freq*t))*attack*np.exp(-t*decay)

    roots = [73.416, 65.406, 58.270, 65.406] if boss else [110, 87.307, 65.406, 97.999]
    for bar in range(bars):
        root = roots[(bar//2 if boss else bar) % 4]
        # Dark suspended chord bed, lightly spread. The final tail wraps to the first bar.
        for i, ratio in enumerate([2, 2*2**(3/12), 3, 4]):
            t = np.arange(round(4.8*beat*SR))/SR
            env = np.minimum(t/.15, 1)*np.exp(-t/(2.1*beat))
            pad = .024*np.sin(2*np.pi*root*ratio*t)*env
            add(bar*4*beat, pad, (i-1.5)*.3)
        for step in range(8):
            if lobby and step % 2: continue
            offset = step*.5
            # Bass breathes between phrases instead of an unbroken dance rhythm.
            if boss and step == 7 and bar % 4 != 3: continue
            ratio = 1 if step % 4 != 3 else 1.5
            add((bar*4+offset)*beat, tone(root*ratio, beat*.43, .11 if boss else .085, 8, .19), 0)
        kick_beats = [0, 2.5] if boss else [0, 2] if lobby else [0, 1.5, 2.75]
        for b in kick_beats:
            t = np.arange(round(.24*SR))/SR
            drum = np.sin(2*np.pi*(44*t+1.8*(1-np.exp(-t*50))))*np.exp(-t*23)
            add((bar*4+b)*beat, .17*drum)
        if not lobby:
            for b in [1, 3]:
                t = np.arange(round(.19*SR))/SR
                snare = filtered_noise(len(t), 600, 7500)*np.exp(-t*36)
                snare += .25*np.sin(2*np.pi*180*t)*np.exp(-t*40)
                add((bar*4+b)*beat, .07*snare, .1)
            for step in range(8):
                t = np.arange(round(.045*SR))/SR
                add((bar*4+step*.5)*beat, .018*filtered_noise(len(t), 4200, 11500)*np.exp(-t*100), -.25 if step % 2 else .25)
        # Sparse, original four-note motif; no vocals or lifted melody.
        notes = [7, 10, 12, 3] if boss else [12, 7, 3, 10]
        for step, semitone in enumerate(notes):
            if (boss and bar % 4 not in [1, 3]) or (lobby and bar % 2): continue
            add((bar*4+step*.75+.25)*beat, tone(root*2**(semitone/12)*2, beat*.6, .025, 6, .03), -.35+step*.2, .2)
        if boss and bar % 4 == 3:
            for step in range(3):
                t = np.arange(round(.26*SR))/SR
                tom = np.sin(2*np.pi*(75+step*12)*t)*np.exp(-t*22)
                add((bar*4+3+step*.25)*beat, .075*tom, (step-1)*.3)
    # A gentle master ceiling and common music loudness keep scene transitions even.
    sound = np.tanh(sound*1.1)
    target_rms = .065 if boss else .058 if not lobby else .052
    sound *= target_rms / np.sqrt(np.mean(sound**2))
    # Music uses equal RMS mastering; save's peak scaler receives the current master peak.
    save(name, sound, peak=min(float(np.max(np.abs(sound))), .58), music=True)


score('LobbyMusic', 90, lobby=True)
score('BattleMusic', 116)
score('BossMusic', 132, boss=True)
(OUT/'README.md').write_text('''# KICK Back original audio\n\nAll WAV files are composed and synthesized from oscillators and seeded, filtered noise by `Tools/generate_audio.py`. No external samples, commercial recordings, copied melodies, vocals, or third-party music are included. They can be used and modified with this project without third-party attribution.\n\n32 kHz / 16-bit PCM. Music is stereo; combat effects are mono. Lobby: 90 BPM / eight bars. Battle: 116 BPM / eight bars. Dojaehak boss: 132 BPM / sixteen bars with sparse minor motifs, a low pulse and tom fills. Wrapped tails and short seam fades make the masters loop safely.\n\nPunch, kick, heavy, ultimate, guard, parry, dodge and defeat use separate original effects. Music is mastered to comparable RMS levels; music and effect gain are independently configurable. Browser playback requires a user gesture. The mixer limits effect voices, gives parries/finishers priority, ducks music briefly on confirmed impact, and pauses music/effects immediately with Esc.\n''')
(OUT/'audit.json').write_text(json.dumps(AUDIT, indent=2)+'\n')
(OUT/'audit.json.meta').write_text('fileFormatVersion: 2\nguid: '+uuid.uuid5(uuid.NAMESPACE_URL, 'kickback/audio/audit').hex+'\n')
print(json.dumps({'files': len(AUDIT), 'clippedSamples': sum(row['clippedSamples'] for row in AUDIT.values()),
                  'maxPeak': max(row['peak'] for row in AUDIT.values()), 'music': {k: v for k, v in AUDIT.items() if v['music']}}, indent=2))
