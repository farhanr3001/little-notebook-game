"""Generate offline neural listening clips. Models/libraries are build-time only."""
import argparse
import hashlib
import json
import sys
import time
import urllib.request
from pathlib import Path

root = Path(__file__).resolve().parent.parent
sys.path.insert(0, str(root / 'tools/tts_vendor'))
cache = root / 'tools/tts_models'
cache.mkdir(exist_ok=True)
sources = {
    'kokoro-v1.0.onnx': 'https://github.com/thewh1teagle/kokoro-onnx/releases/download/model-files-v1.1/kokoro-v1.0.onnx',
    'voices-v1.0.bin': 'https://github.com/thewh1teagle/kokoro-onnx/releases/download/model-files-v1.1/voices-v1.0.bin',
}

parser = argparse.ArgumentParser()
parser.add_argument('--download-only', action='store_true')
parser.add_argument('--samples', action='store_true')
args = parser.parse_args()
for name, url in sources.items():
    target = cache / name
    if not target.exists():
        print(f'Downloading {name}', flush=True)
        partial = target.with_suffix(target.suffix + '.part')
        request = urllib.request.Request(url + '?download=' + str(int(time.time())), headers={'User-Agent': 'LittleNotebook-voice-builder'})
        with urllib.request.urlopen(request, timeout=120) as response, partial.open('wb') as output:
            while chunk := response.read(1024 * 1024):
                output.write(chunk)
        partial.replace(target)
        print(f'Saved {name}: {target.stat().st_size / 1048576:.1f} MB', flush=True)
if args.download_only:
    raise SystemExit()

import numpy as np
import onnxruntime as ort
import soundfile as sf
from kokoro_onnx import Kokoro

options = ort.SessionOptions()
options.intra_op_num_threads = 2
options.inter_op_num_threads = 1
session = ort.InferenceSession(str(cache / 'kokoro-v1.0.onnx'), sess_options=options, providers=['CPUExecutionProvider'])
engine = Kokoro.from_session(session, str(cache / 'voices-v1.0.bin'))

def generate(text, voice, lang, path):
    samples, rate = engine.create(text, voice=voice, speed=0.96, lang=lang)
    samples = np.asarray(samples, dtype=np.float32)
    assert len(samples) / rate > .2 and np.isfinite(samples).all(), text
    peak = np.max(np.abs(samples))
    assert peak > .005, f'Silent speech: {text}'
    samples *= min(1.8, .82 / peak)
    fade = min(int(rate * .005), len(samples) // 3)
    samples[:fade] *= np.linspace(0, 1, fade)
    samples[-fade:] *= np.linspace(1, 0, fade)
    samples = np.concatenate([np.zeros(int(rate*.06)), samples, np.zeros(int(rate*.09))])
    sf.write(path, samples, rate, subtype='PCM_16')
    return round(len(samples) / rate, 3)

if args.samples:
    output = root / 'test-output'
    output.mkdir(exist_ok=True)
    for voice, lang in [('af_heart', 'en-us'), ('bf_emma', 'en-gb')]:
        generate('Book. Butterfly. Comfortable. Listen carefully, then choose the correct word.', voice, lang, output / f'voice-{voice}.wav')
        print('Generated voice sample:', voice, flush=True)
else:
    questions = json.loads((root / 'content/questions.json').read_text(encoding='utf-8'))
    words = sorted({q['Speech'].strip() for q in questions if q.get('Speech')})
    output = root / 'assets/speech'
    output.mkdir(exist_ok=True)
    manifest, detail = {}, []
    for i, word in enumerate(words):
        name = hashlib.sha256(word.lower().encode()).hexdigest()[:16] + '.wav'
        duration = generate(word + '.', 'af_heart', 'en-us', output / name)
        manifest[word.lower()] = name
        detail.append({'text': word, 'file': name, 'seconds': duration})
        print(f'{i+1}/{len(words)}: {word} ({duration}s)', flush=True)
    (output / 'manifest.json').write_text(json.dumps(manifest, indent=2, ensure_ascii=False), encoding='utf-8')
    provenance = dict(engine='Kokoro-82M v1.0', voice='af_heart', language='en-us', speed=.96, wrapper='kokoro-onnx 0.6.1', source_urls=sources, model_sha256={n: hashlib.sha256((cache/n).read_bytes()).hexdigest() for n in sources}, clips=detail)
    (output / 'provenance.json').write_text(json.dumps(provenance, indent=2), encoding='utf-8')
    print(f'PASS: {len(words)} neural clips cover every authored Speech field.', flush=True)
