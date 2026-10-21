"""Private offline JSON-lines speech worker. No network or server socket."""
import contextlib
import json
import sys
import tempfile
import wave
from pathlib import Path

sys.stdin.reconfigure(encoding='utf-8')
sys.stdout.reconfigure(encoding='utf-8')
root=Path(__file__).resolve().parent
models=root/'models'
english=None
arabic=None

def synthesize(text,language,target):
    global english,arabic
    import numpy as np
    import onnxruntime as ort
    options=ort.SessionOptions()
    options.intra_op_num_threads=2
    options.inter_op_num_threads=1
    if language=='ar':
        from piper import PiperVoice,SynthesisConfig
        from piper.config import PiperConfig
        if arabic is None:
            config=PiperConfig.from_dict(json.loads((models/'ar_JO-kareem-medium.onnx.json').read_text(encoding='utf-8')))
            arabic=PiperVoice(ort.InferenceSession(str(models/'ar_JO-kareem-medium.onnx'),sess_options=options,providers=['CPUExecutionProvider']),config)
        with wave.open(str(target),'wb') as wav:
            arabic.synthesize_wav(text,wav,syn_config=SynthesisConfig(length_scale=1.05))
    else:
        import soundfile as sf
        from kokoro_onnx import Kokoro
        if english is None:
            session=ort.InferenceSession(str(models/'kokoro-v1.0.int8.onnx'),sess_options=options,providers=['CPUExecutionProvider'])
            english=Kokoro.from_session(session,str(models/'voices-v1.0.bin'))
        samples,rate=english.create(text,voice='af_heart',speed=.96,lang='en-us')
        samples=np.asarray(samples,dtype=np.float32)
        peak=np.max(np.abs(samples))
        if peak>0:samples*=min(1.8,.82/peak)
        sf.write(target,samples,rate,subtype='PCM_16')

for line in sys.stdin:
    try:
        request=json.loads(line)
        text=request['text'].strip()
        language=request.get('language','en')
        if not text or len(text)>2000 or language not in ('en','ar'):raise ValueError('Unsupported speech request')
        target=Path(request['output']).resolve()
        if target.suffix!='.wav':raise ValueError('Speech output must be WAV')
        target.parent.mkdir(parents=True,exist_ok=True)
        temporary=target.parent/'speech-temp'
        temporary.mkdir(exist_ok=True)
        tempfile.tempdir=str(temporary)
        partial=target.with_suffix('.partial.wav')
        with contextlib.redirect_stdout(sys.stderr):synthesize(text,language,partial)
        partial.replace(target)
        print(json.dumps({'ok':True,'output':str(target)},ensure_ascii=False),flush=True)
    except Exception as exc:
        print(json.dumps({'ok':False,'error':str(exc)},ensure_ascii=False),flush=True)
