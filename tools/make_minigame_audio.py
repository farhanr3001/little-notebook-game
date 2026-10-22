"""Original soft piano-style tones and a short clearing melody (PCM WAV)."""
from pathlib import Path
import math,random,struct,wave

root=Path(__file__).resolve().parent.parent/'assets/minigames'
root.mkdir(parents=True,exist_ok=True)
rate=22050
def tone(midi,duration=.72):
 frequency=440*2**((midi-69)/12);randomizer=random.Random(midi);samples=[]
 for index in range(int(duration*rate)):
  t=index/rate;attack=min(1,t/.004);value=0
  for partial,weight in enumerate((1,.40,.19,.10,.055,.025),1):
   decay=math.exp(-t*(3.5+partial*1.2));detune=1+.00014*partial*partial
   value+=weight*decay*math.sin(2*math.pi*frequency*partial*detune*t)
  value+=randomizer.uniform(-1,1)*.018*math.exp(-t*95)
  samples.append(value*attack*min(1,(duration-t)/.035)*.48)
 return samples
def write(name,samples):
 with wave.open(str(root/name),'wb') as audio:
  audio.setparams((1,2,rate,0,'NONE','not compressed'))
  audio.writeframes(b''.join(struct.pack('<h',max(-32767,min(32767,round(sample*24000)))) for sample in samples))
notes=(60,62,64,65,67,69,71,72)
for index,note in enumerate(notes):write(f'note-{index}.wav',tone(note))
write('low.wav',tone(48,.48))
melody=[0.0]*int(rate*1.55)
for offset,note,gain in ((0,60,.8),(.14,64,.8),(.28,67,.8),(.44,72,.8),(.64,64,.25),(.64,67,.25)):
 start=round(offset*rate)
 for index,value in enumerate(tone(note,.85)):
  if start+index<len(melody):melody[start+index]+=value*gain
write('clear.wav',melody)
print('Created 10 original piano-style minigame sounds.')
