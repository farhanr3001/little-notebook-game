"""Create installer payload and hash-verified update component packs. Never ship saves."""
from pathlib import Path
from zipfile import ZipFile, ZIP_DEFLATED
import argparse, hashlib, json, shutil, re

root=Path(__file__).resolve().parent.parent
parser=argparse.ArgumentParser();parser.add_argument('--source',default='release-build');parser.add_argument('--version',default=re.search(r'Version="([0-9.]+)"',(root/'src/AppInfo.cs').read_text(encoding='utf-8')).group(1));args=parser.parse_args()
source=root/args.source;release=root/'releases';payload=release/'payload';packs=release/'update-packs'
payload.mkdir(parents=True,exist_ok=True);packs.mkdir(parents=True,exist_ok=True)
docs=['README.md','ASSETS.md']
previous=json.loads((payload/'payload-manifest.json').read_text(encoding='utf-8')) if (payload/'payload-manifest.json').exists() else {'files':[]}
for name in docs:shutil.copy2(root/name,source/name)
shutil.copy2(root/'tools/radio-palette-prompts.json',source/'radio-palette-prompts.json')
allowed={'assets','content','speech-runtime'}
files=[p for p in source.rglob('*') if p.is_file() and (p.relative_to(source).parts[0] in allowed or p.parent==source and (p.name in docs or p.name in ['LittleNotebook.exe','LittleNotebook.exe.config','LittleNotebook.Update.exe','radio-palette-prompts.json'])) and '__pycache__' not in p.parts and p.suffix!='.pyc']
assert (source/'LittleNotebook.Update.exe').exists()
def digest(path):
 h=hashlib.sha256()
 with path.open('rb') as f:
  while block:=f.read(1024*1024):h.update(block)
 return h.hexdigest()
def component(relative):
 p=relative.parts
 if p[0]=='assets':return 'minigames-audio' if len(p)>1 and p[1]=='minigames' else 'music' if relative.suffix=='.mp3' else 'listening' if len(p)>1 and p[1]=='speech' else 'art'
 if p[0]=='speech-runtime':
  if len(p)>1 and p[1]=='media':return 'media-tools'
  if p[-1]=='media_worker.py':return 'app'
  if len(p)>1 and p[1]=='models':return 'voice-'+re.sub('[^a-zA-Z0-9-]','-',p[2])
  if len(p)>2 and p[1]=='Lib':return 'runtime-'+re.sub('[^a-zA-Z0-9-]','-',p[2])
  return 'app' if p[-1]=='worker.py' else 'python-core'
 return 'app'
groups={};records=[]
for path in sorted(files):
 relative=path.relative_to(source);dest=payload/relative;dest.parent.mkdir(parents=True,exist_ok=True)
 if not dest.exists() or digest(dest)!=digest(path):shutil.copy2(path,dest)
 pack=component(relative)+'.zip';groups.setdefault(pack,[]).append((path,relative.as_posix()));records.append(dict(path=relative.as_posix(),sha256=digest(path),size=path.stat().st_size,pack=pack))
current={record['path'] for record in records}
for old in previous['files']:
 if old['path'] not in current:
  obsolete=(payload/old['path']).resolve()
  assert obsolete.is_relative_to(payload.resolve())
  if obsolete.is_file():obsolete.unlink()
base=f'https://github.com/farhanr3001/little-notebook-game/releases/download/v{args.version}/'
packages=[]
for name,items in groups.items():
 target=packs/name
 prior=next((p for p in previous.get('packs',[]) if p['name']==name),None)
 old_files=[(f['path'],f['sha256'],f['size']) for f in previous['files'] if f['pack']==name]
 new_files=[(f['path'],f['sha256'],f['size']) for f in records if f['pack']==name]
 reuse=prior and old_files==new_files and target.exists() and digest(target)==prior['sha256']
 if not reuse:
  with ZipFile(target,'w',ZIP_DEFLATED,compresslevel=3) as z:
   for path,relative in items:z.write(path,relative)
  with ZipFile(target) as z:assert z.testzip() is None
 packages.append(dict(name=name,url=base+name,sha256=digest(target),size=target.stat().st_size))
manifest=dict(version=args.version,repository='farhanr3001/little-notebook-game',notes='Ten new minigames: Light Garden, Pocket Queens, Crate Garden, Pebble Solitaire, Thread Untangle, Colour Pour, Codebreaker, Domino Patch, Marble Glide and Flood Garden. Procedural levels, undo, accessible colour labels, mouse and keyboard controls, increasing challenges and saved checkpoints. Picture Patches now has a centred timer, saved times and moves, a records page and a nearby preview eye. Existing progress, notebook and music are preserved.',files=records,packs=packages)
for dest in [payload/'payload-manifest.json',release/'update-manifest.json']:dest.write_text(json.dumps(manifest,ensure_ascii=False,indent=2),encoding='utf-8')
assert not any('profile.json' in x['path'] or x['path'].startswith('data/') for x in records)
print(f'PASS: {len(records)} managed files, {len(packages)} verified component packs. No user saves in payload.')
