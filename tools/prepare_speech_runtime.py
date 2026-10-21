"""Download a private portable speech runtime; never changes system Python."""
from pathlib import Path
import json
import shutil
import subprocess
import sys
import time
import urllib.request
import zipfile

root=Path(__file__).resolve().parent.parent
runtime=root/'tools/speech-runtime'
models=runtime/'models'
models.mkdir(parents=True,exist_ok=True)

def download(url,target):
    if target.exists(): return
    target.parent.mkdir(parents=True,exist_ok=True)
    print('Downloading',target.name,flush=True)
    request=urllib.request.Request(url+('?' if '?' not in url else '&')+'cache='+str(int(time.time())),headers={'User-Agent':'LittleNotebook-speech-builder'})
    part=target.with_suffix(target.suffix+'.part')
    with urllib.request.urlopen(request,timeout=120) as response, part.open('wb') as output:
        while chunk:=response.read(1024*1024):output.write(chunk)
    part.replace(target)

python_zip=root/'tools/tts_models/python-3.12.10-embed-amd64.zip'
download('https://www.python.org/ftp/python/3.12.10/python-3.12.10-embed-amd64.zip',python_zip)
if not (runtime/'python.exe').exists():
    with zipfile.ZipFile(python_zip) as z:z.extractall(runtime)
(runtime/'python312._pth').write_text('python312.zip\n.\nLib\n',encoding='ascii')
if not (runtime/'Lib/kokoro_onnx').exists():
    subprocess.run([sys.executable,'-m','pip','install','--index-url','https://pypi.org/simple','--target',str(runtime/'Lib'),'--python-version','312','--only-binary=:all:','-r',str(root/'tools/runtime_requirements.txt'),'--disable-pip-version-check'],check=True)

release=json.load(urllib.request.urlopen(urllib.request.Request('https://api.github.com/repos/thewh1teagle/kokoro-onnx/releases/tags/model-files-v1.0',headers={'User-Agent':'LittleNotebook-speech-builder'}),timeout=30))
assets=release['assets']
quantized=next(x for x in assets if 'int8' in x['name'] and x['name'].endswith('.onnx'))
download(quantized['browser_download_url'],models/'kokoro-v1.0.int8.onnx')
download(next(x for x in assets if x['name']=='voices-v1.0.bin')['browser_download_url'],models/'voices-v1.0.bin')
base='https://huggingface.co/rhasspy/piper-voices/resolve/main/ar/ar_JO/kareem/medium/'
for name in ['ar_JO-kareem-medium.onnx','ar_JO-kareem-medium.onnx.json','MODEL_CARD']:download(base+name,models/name)
shutil.copy2(root/'tools/speech-notices.md',runtime/'THIRD_PARTY_NOTICES.md')
print('PASS: private portable English/Arabic runtime ready.',flush=True)
