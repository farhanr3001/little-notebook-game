"""Arrange sampled Salamander piano notes and traditional/classical tunes.
Sample: Alexander Holm, CC BY 3.0, tools/samples/piano-C4.mp3.
Scores: Beethoven's Ode to Joy theme; traditional Twinkle / Frère Jacques.
The shared JSON defines both click pitches and the full replay arrangement.
"""
from pathlib import Path
import json, subprocess, wave
import numpy as np

project=Path(__file__).resolve().parent.parent
root=project/'assets/minigames';root.mkdir(parents=True,exist_ok=True)
rate=22050
raw=subprocess.run(['ffmpeg','-v','error','-i',str(project/'tools/samples/piano-C4.mp3'),'-f','f32le','-ac','1','-ar',str(rate),'-'],capture_output=True,check=True).stdout
piano=np.frombuffer(raw,dtype='<f4').astype(np.float64)
start=np.flatnonzero(np.abs(piano)>.001)[0];piano=piano[max(0,start-45):]
piano/=max(.01,np.max(np.abs(piano)))
notes=(60,62,64,65,67,69,71,72)
def tone(midi,duration=.92):
 n=int(duration*rate);signal=np.interp(np.arange(n)*2**((midi-60)/12),np.arange(len(piano)),piano,left=0,right=0)
 attack=min(int(rate*.004),n);release=min(int(rate*.15),n//2)
 signal[:attack]*=np.linspace(0,1,attack);signal[-release:]*=np.linspace(1,0,release)**1.5
 return signal*.72
def write(name,samples):
 assert np.max(np.abs(samples))<=1.0,'Audio clipping: '+name
 with wave.open(str(root/name),'wb') as audio:
  audio.setparams((1,2,rate,0,'NONE','not compressed'));audio.writeframes((samples*28000).astype('<i2').tobytes())
for index,note in enumerate(notes):write(f'note-{index}.wav',tone(note))
write('low.wav',tone(48,.45))
clear=np.zeros(int(rate*1.55))
for offset,note,gain in ((0,60,.7),(.12,64,.7),(.24,67,.7),(.4,72,.7)):
 signal=tone(note,.95)*gain;at=round(offset*rate);clear[at:at+len(signal)]+=signal
write('clear.wav',clear)
pulse=np.zeros(int(rate*.43))
for offset,note in ((0,67),(.075,72)):
 signal=tone(note,.30)*.65;at=round(offset*rate);pulse[at:at+len(signal)]+=signal
write('pulse.wav',pulse)
tunes=json.loads((project/'content/minigame-melodies.json').read_text(encoding='utf-8'))
for tune in tunes:
 duration=sum(tune['Beats'])*tune['BeatSeconds']+1.0;output=np.zeros(int(round(duration*rate)));offset=0
 assert len(tune['Notes'])==len(tune['Beats'])
 for index,(pitch,beats) in enumerate(zip(tune['Notes'],tune['Beats'])):
  length=beats*tune['BeatSeconds'];signal=tone(notes[pitch],min(1.15,length+.22))*(.75 if index%4 else .82)
  at=round(offset*rate);output[at:at+len(signal)]+=signal;offset+=length
 write('tune-'+tune['Id']+'.wav',output)
print(f'Created 11 sampled piano cues and {len(tunes)} complete melody replays.')
