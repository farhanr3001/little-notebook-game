"""Exercise the real published GitHub client/updater without desktop automation."""
from pathlib import Path
import hashlib, json, shutil, subprocess, time, urllib.request, zipfile
root=Path(__file__).resolve().parent.parent
area=root/'test-output/live-update';app=area/'app';area.mkdir(parents=True,exist_ok=True)
release=json.loads((root/'releases/update-manifest.json').read_text(encoding='utf-8'))
report=root/'test-output/live-update.txt'
def run(command,timeout=180):
 result=subprocess.run([str(x) for x in command],capture_output=True,timeout=timeout)
 assert result.returncode==0,f'Process failed: {command[0]} ({result.returncode})'
def sha(path):return hashlib.sha256(path.read_bytes()).hexdigest()
def wait_for(test,seconds=90):
 end=time.monotonic()+seconds
 while time.monotonic()<end:
  if test():return
  time.sleep(.25)
 raise AssertionError('Timed out waiting for production updater')
try:
 assert app.resolve().is_relative_to((root/'test-output').resolve())
 if (app/'unins000.exe').exists():run([app/'unins000.exe','/VERYSILENT','/SUPPRESSMSGBOXES','/NORESTART'])
 run([root/f"releases/LittleNotebook-Setup-{release['version']}.exe",'/VERYSILENT','/SUPPRESSMSGBOXES','/NORESTART','/SP-','/TESTINSTALL=1',f'/DIR={app}',f'/LOG={area}/install.log'])
 data=app/'user-data-test'
 profile=json.loads((root/'test-output/revision-profile.json').read_text(encoding='utf-8-sig'))
 for notebook in profile['Notebook']:notebook.setdefault('TracePhoto','')
 profile.update(Name='Update preservation fixture',Coins=777,StackBest=23)
 profile['MiniGames']={key:dict(Level=17 if key=='arrows' else 4,Seed=81623+index,BestCompleted=16 if key=='arrows' else 3,Completed=21,Failures=2,BestSeconds=62.8 if key=='mousedash' else 0,LastSeconds=48.2 if key=='mousedash' else 0,LastMoves=21,BestMoves=19,Results=[dict(Level=3,Moves=21,Seconds=52.1,Date='2026-10-23T10:00:00Z')]) for index,key in enumerate(['arrows','tiles','maze','mousedash','lights','queens','crates','pegs','untangle','pour','code','domino','glide','flood','signal','pulse','lanterns','echo'])}
 profile['Settings'].update(Language='en',Music=False,Sound=False,AutoReadQuestions=False,Volume=0,CustomMusic=['data/music/preserve.wav'],MusicTitles={'data/music/preserve.wav':'My preserved music name'})
 (data/'profile.json').write_text(json.dumps(profile,ensure_ascii=False),encoding='utf-8')
 (data/'music').mkdir(exist_ok=True);shutil.copy2(app/'assets/click.wav',data/'music/preserve.wav');(data/'music/preserve.bin').write_bytes(b'preserve imported music')
 music_hash=sha(data/'music/preserve.bin')
 # Exercise the actual published previous client and piano files, rather than
 # merely changing a version constant in the new executable.
 def download(url):
  assert url.startswith('https://github.com/farhanr3001/little-notebook-game/releases/download/v0.8.1/')
  with urllib.request.urlopen(urllib.request.Request(url,headers={'User-Agent':'LittleNotebook-update-verification'}),timeout=90) as response:return response.read()
 old=json.loads(download('https://github.com/farhanr3001/little-notebook-game/releases/download/v0.8.1/update-manifest.json'))
 current=json.loads((app/'payload-manifest.json').read_text(encoding='utf-8'));old_paths={f['path'] for f in old['files']}
 for record in current['files']:
  if record['path'] not in old_paths:
   target=(app/record['path']).resolve();assert target.is_relative_to(app.resolve())
   if target.is_file():target.unlink()
 for pack in old['packs']:
  if pack['name']!='app.zip':continue
  archive=area/('previous-'+pack['name']);archive.write_bytes(download(pack['url']))
  assert archive.stat().st_size==pack['size'] and sha(archive)==pack['sha256']
  with zipfile.ZipFile(archive) as source:
   for record in old['files']:
    if record['pack']!=pack['name']:continue
    relative=Path(record['path']);assert not relative.is_absolute() and '..' not in relative.parts and relative.parts[0] not in ('data','user-data-test')
    target=(app/relative).resolve();assert target.is_relative_to(app.resolve());target.parent.mkdir(parents=True,exist_ok=True);target.write_bytes(source.read(record['path']));assert sha(target)==record['sha256']
 old['files'].append(dict(path='assets/obsolete.txt',size=8,sha256='0'*64,pack='art.zip'))
 (app/'payload-manifest.json').write_text(json.dumps(old),encoding='utf-8');(app/'assets/obsolete.txt').write_text('obsolete')
 unchanged=app/'assets/paper.png';unchanged_hash=sha(unchanged);unchanged_time=unchanged.stat().st_mtime_ns
 run([app/'LittleNotebook.exe','--live-update-test'])
 wait_for(lambda:(data/'update.log').exists() and f"Updated to {release['version']}" in (data/'update.log').read_text())
 expected=next(f for f in release['files'] if f['path']=='LittleNotebook.exe')
 wait_for(lambda:sha(app/'LittleNotebook.exe')==expected['sha256'])
 assert not (app/'assets/obsolete.txt').exists(),'Obsolete managed file retained'
 assert sha(unchanged)==unchanged_hash and unchanged.stat().st_mtime_ns==unchanged_time,'Unchanged asset rewritten'
 saved=json.loads((data/'profile.json').read_text(encoding='utf-8-sig'))
 for key in ['Name','Character','Coins','Pet','PetColor','Owned','Attempts','Sessions','Notebook','MiniGames','StackBest']:
  assert saved[key]==profile[key],f'Saved state changed during update: {key}'
 assert saved['Settings']['MusicTitles']==profile['Settings']['MusicTitles'],'Music names changed'
 assert saved['Settings']['CustomMusic']==profile['Settings']['CustomMusic'],'Music library changed'
 assert sha(data/'music/preserve.bin')==music_hash,'Imported music changed'
 downloaded=[p.name for p in (data/'updates').rglob('*.zip')]
 assert set(downloaded)=={'app.zip'} and len(downloaded)==1,f'Unchanged components downloaded: {downloaded}'
 assert (app/'assets/minigames/tune-ode.wav').is_file() and (app/'assets/minigames/pulse.wav').is_file() and (app/'content/minigame-melodies.json').is_file(),'New sampled piano/score assets missing'
 run([app/'LittleNotebook.exe','--live-update-test'])
 result=(data/'live-client-test.txt').read_text(encoding='utf-8-sig');assert 'FAIL:' not in result and 'manual Check for updates reports up to date' in result
 run([app/'unins000.exe','/VERYSILENT','/SUPPRESSMSGBOXES','/NORESTART',f'/LOG={area}/uninstall.log'])
 wait_for(lambda:not app.exists(),15)
 report.write_text('PASS: actual published 0.8.1 client accepted the GitHub startup update offer; only the application component downloaded; production updater replacing the older executable and removing obsolete files; preserved character/stars/purchases/attempts/notebook/imported music and minigame checkpoints, Paper Stack high score and Mouse Dash times; unchanged asset timestamp; no popup on current version; manual up-to-date result; complete uninstall of the updated installation. No desktop automation.\n',encoding='utf-8')
 print(report.read_text())
except Exception as error:
 report.write_text('FAIL: '+repr(error),encoding='utf-8');raise
