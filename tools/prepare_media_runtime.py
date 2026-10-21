"""Bundle private URL-import tools; no PATH or system installations required."""
from pathlib import Path
from concurrent.futures import ThreadPoolExecutor
import json, hashlib, shutil, subprocess, sys, urllib.request, zipfile
root=Path(__file__).resolve().parent.parent
media=root/'tools/speech-runtime/media';cache=root/'tools/install-build/media';media.mkdir(parents=True,exist_ok=True);cache.mkdir(parents=True,exist_ok=True)
def get(url,target):
 if not target.exists():
  print('Downloading',target.name,flush=True)
  req=urllib.request.Request(url,headers={'User-Agent':'LittleNotebook-build'})
  with urllib.request.urlopen(req,timeout=180) as response,target.with_suffix('.part').open('wb') as output:shutil.copyfileobj(response,output)
  target.with_suffix('.part').replace(target)
 return target
def deno():
 archive=get('https://github.com/denoland/deno/releases/download/v2.9.8/deno-x86_64-pc-windows-msvc.zip',cache/'deno-2.9.8.zip')
 with zipfile.ZipFile(archive) as z:
  with z.open('deno.exe') as source,(media/'deno.exe').open('wb') as target:shutil.copyfileobj(source,target)
def ffmpeg():
 archive=get('https://www.gyan.dev/ffmpeg/builds/ffmpeg-release-essentials.zip',cache/'ffmpeg-essentials.zip')
 with zipfile.ZipFile(archive) as z:
  for name in z.namelist():
   if name.endswith(('/bin/ffmpeg.exe','/bin/ffprobe.exe','/LICENSE','/LICENSE.txt','/README.txt')):
    with z.open(name) as source,(media/Path(name).name).open('wb') as target:shutil.copyfileobj(source,target)
with ThreadPoolExecutor(max_workers=2) as pool:list(pool.map(lambda task:task(),[deno,ffmpeg]))
requirements=root/'tools/media_requirements.txt'
if not requirements.exists():
 info=json.load(urllib.request.urlopen('https://pypi.org/pypi/yt-dlp/json'))
 requirements.write_text('yt-dlp[default]=='+info['info']['version']+'\n',encoding='utf-8')
if not (media/'Lib/yt_dlp').exists() or not (media/'Lib/yt_dlp_ejs').exists():
 subprocess.run([sys.executable,'-m','pip','install','--index-url','https://pypi.org/simple','--target',str(media/'Lib'),'--python-version','312','--only-binary=:all:','-r',str(requirements),'--disable-pip-version-check'],check=True)
shutil.copy2(root/'tools/media_worker.py',root/'tools/speech-runtime/media_worker.py')
(media/'PROVENANCE.json').write_text(json.dumps({p.name:hashlib.sha256(p.read_bytes()).hexdigest() for p in media.glob('*.exe')},indent=2),encoding='utf-8')
(media/'NOTICES.md').write_text('yt-dlp: Unlicense, https://github.com/yt-dlp/yt-dlp . EJS: https://github.com/yt-dlp/ejs . Deno 2.9.8: MIT, https://github.com/denoland/deno . FFmpeg essentials: GPLv3, original build and source/build configuration: https://www.gyan.dev/ffmpeg/builds/ and https://ffmpeg.org/ . Included package metadata contains component licenses. The private worker source is tools/media_worker.py in the game repository.\n',encoding='utf-8')
print('PASS: bundled yt-dlp/EJS, Deno and FFmpeg/ffprobe ready.',flush=True)
