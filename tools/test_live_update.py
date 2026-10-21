"""Exercise the real published GitHub client/updater without desktop automation."""
from pathlib import Path
import hashlib, json, shutil, subprocess, time
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
 profile.update(Name='Update preservation fixture',Coins=777)
 profile['Settings'].update(Language='en',Music=False,Sound=False,AutoReadQuestions=False,Volume=0,CustomMusic=['data/music/preserve.wav'],MusicTitles={'data/music/preserve.wav':'My preserved music name'})
 (data/'profile.json').write_text(json.dumps(profile,ensure_ascii=False),encoding='utf-8')
 (data/'music').mkdir(exist_ok=True);shutil.copy2(app/'assets/click.wav',data/'music/preserve.wav');(data/'music/preserve.bin').write_bytes(b'preserve imported music')
 music_hash=sha(data/'music/preserve.bin')
 shutil.copy2(root/'test-output/update-baseline/LittleNotebook.exe',app/'LittleNotebook.exe')
 old=json.loads((app/'payload-manifest.json').read_text(encoding='utf-8'))
 old['version']='0.4.0';old['files'].append(dict(path='assets/obsolete.txt',size=8,sha256='0'*64,pack='art.zip'))
 (app/'payload-manifest.json').write_text(json.dumps(old),encoding='utf-8');(app/'assets/obsolete.txt').write_text('obsolete')
 unchanged=app/'assets/paper.png';unchanged_hash=sha(unchanged);unchanged_time=unchanged.stat().st_mtime_ns
 run([app/'LittleNotebook.exe','--live-update-test'])
 wait_for(lambda:(data/'update.log').exists() and f"Updated to {release['version']}" in (data/'update.log').read_text())
 expected=next(f for f in release['files'] if f['path']=='LittleNotebook.exe')
 wait_for(lambda:sha(app/'LittleNotebook.exe')==expected['sha256'])
 assert not (app/'assets/obsolete.txt').exists(),'Obsolete managed file retained'
 assert sha(unchanged)==unchanged_hash and unchanged.stat().st_mtime_ns==unchanged_time,'Unchanged asset rewritten'
 saved=json.loads((data/'profile.json').read_text(encoding='utf-8-sig'))
 for key in ['Name','Character','Coins','Pet','PetColor','Owned','Attempts','Sessions','Notebook']:
  assert saved[key]==profile[key],f'Saved state changed during update: {key}'
 assert saved['Settings']['MusicTitles']==profile['Settings']['MusicTitles'],'Music names changed'
 assert saved['Settings']['CustomMusic']==profile['Settings']['CustomMusic'],'Music library changed'
 assert sha(data/'music/preserve.bin')==music_hash,'Imported music changed'
 downloaded=[p.name for p in (data/'updates').rglob('*.zip')]
 assert downloaded==['app.zip'],f'Unchanged components downloaded: {downloaded}'
 run([app/'LittleNotebook.exe','--live-update-test'])
 result=(data/'live-client-test.txt').read_text(encoding='utf-8-sig');assert 'FAIL:' not in result and 'manual Check for updates reports up to date' in result
 run([app/'unins000.exe','/VERYSILENT','/SUPPRESSMSGBOXES','/NORESTART',f'/LOG={area}/uninstall.log'])
 wait_for(lambda:not app.exists(),15)
 report.write_text('PASS: real GitHub startup update offer, app-component-only download, production updater replacing the older executable and removing obsolete files, preserved character/stars/purchases/attempts/notebook/imported music, unchanged asset timestamp, no popup on current version, manual up-to-date result, and complete uninstall of the updated installation. No desktop automation.\n',encoding='utf-8')
 print(report.read_text())
except Exception as error:
 report.write_text('FAIL: '+repr(error),encoding='utf-8');raise
