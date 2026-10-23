"""Verify the produced PCM files independently of their synthesis code."""
from pathlib import Path
import json, wave
import numpy as np
root=Path(__file__).resolve().parent.parent
tunes=json.loads((root/'content/minigame-melodies.json').read_text(encoding='utf-8'))
def read(name):
 with wave.open(str(root/'assets/minigames'/name),'rb') as file:
  assert file.getnchannels()==1 and file.getsampwidth()==2 and file.getframerate()==22050
  samples=np.frombuffer(file.readframes(file.getnframes()),dtype='<i2').astype(float)/32768
 assert np.max(np.abs(samples))<.96 and np.max(np.abs(samples))>.02,name+' clipped or silent'
 assert np.max(np.abs(samples[-20:]))<.005,name+' ends abruptly'
 return samples
for index,midi in enumerate([60,62,64,65,67,69,71,72]):
 samples=read(f'note-{index}.wav');assert abs(len(samples)/22050-.92)<1/22050
 window=samples[1000:7000]*np.hanning(6000);spectrum=np.abs(np.fft.rfft(window,65536));frequencies=np.fft.rfftfreq(65536,1/22050)
 selection=(frequencies>180)&(frequencies<1600);peak=frequencies[selection][np.argmax(spectrum[selection])]
 expected=440*2**((midi-69)/12);assert min(abs(peak/expected-harmonic) for harmonic in [1,2,3])<.035,('Wrong piano pitch',index,peak,expected)
for tune in tunes:
 samples=read('tune-'+tune['Id']+'.wav');expected=sum(tune['Beats'])*tune['BeatSeconds']+1
 assert abs(len(samples)/22050-expected)<=1/22050,'Replay duration does not match score'
 assert len(samples)>22050*5 and np.sqrt(np.mean(samples[:int((expected-1)*22050)]**2))>.015,'Incomplete or silent arrangement'
read('low.wav');read('clear.wav');read('pulse.wav')
text=f'PASS: {11+len(tunes)} sampled piano files; eight pitches, PCM format, non-clipping peaks, clean tails and all {len(tunes)} complete score/replay durations.\n'
(root/'test-output/minigame-audio.txt').write_text(text,encoding='utf-8');print(text)
