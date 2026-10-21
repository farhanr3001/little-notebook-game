"""Make quiet piano UI cues from the Salamander C4 piano recording (Alexander Holm).
Original sample: https://tonejs.github.io/audio/salamander/C4.mp3
The generated game cues are derived notes/chords with short fades and no echo.
"""
import subprocess, wave
from pathlib import Path
import numpy as np
root=Path(__file__).resolve().parents[1]
rate=44100
raw=subprocess.run(['ffmpeg','-v','error','-i',str(root/'tools/samples/piano-C4.mp3'),'-f','f32le','-ac','1','-ar',str(rate),'-'],capture_output=True,check=True).stdout
piano=np.frombuffer(raw,dtype='<f4').astype(np.float64)
# The source has a tiny lead-in. Start at the first audible hammer attack.
start=np.flatnonzero(np.abs(piano)>.001)[0]
piano=piano[max(0,start-100):]
piano/=max(.01,np.max(np.abs(piano)))
def render(events,duration):
    output=np.zeros(int(rate*duration))
    for when,midi,length,gain in events:
        n=int(rate*length);x=np.arange(n)*2**((midi-60)/12)
        signal=np.interp(x,np.arange(len(piano)),piano,left=0,right=0)
        attack=min(int(rate*.006),n);release=min(int(rate*.09),n//2)
        signal[:attack]*=np.linspace(0,1,attack)
        signal[-release:]*=np.linspace(1,0,release)**1.5
        offset=int(when*rate);end=min(len(output),offset+n)
        output[offset:end]+=signal[:end-offset]*gain
    return np.clip(output,-.90,.90)
sequences={
 'typing': ([(0,65,.10,.17)],.12),
 'click': ([(0,67,.19,.26)],.23),
 'correct': ([(0,64,.44,.32),(.10,67,.58,.33)],.76),
 'wrong': ([(0,53,.50,.27),(.02,57,.49,.20)],.57),
 'finish': ([(0,60,.72,.25),(.10,64,.72,.25),(.20,67,.80,.28),(.32,72,.95,.26)],1.40)
}
for name,(events,duration) in sequences.items():
    samples=render(events,duration)
    with wave.open(str(root/'assets'/f'{name}.wav'),'wb') as f:
        f.setnchannels(1);f.setsampwidth(2);f.setframerate(rate);f.writeframes((samples*32767).astype('<i2').tobytes())
print('Created four feedback cues and a subtle typing cue from sampled piano.')
