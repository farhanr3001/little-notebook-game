"""Isolated real installer, differential updater, rollback and uninstaller checks."""
from pathlib import Path
import ctypes, hashlib, json, shutil, subprocess, time

root=Path(__file__).resolve().parent.parent
area=root/'test-output/install-lifecycle'
assert area.resolve().is_relative_to((root/'test-output').resolve())
app=area/'app'
release_version=json.loads((root/'releases/update-manifest.json').read_text(encoding='utf-8'))['version']
setup=root/f'releases/LittleNotebook-Setup-{release_version}.exe'
report=root/'test-output/install-update.txt'
checks=0
def check(value,message):
 global checks
 checks+=1
 if not value:raise AssertionError(message)
def sha(path):return hashlib.sha256(path.read_bytes()).hexdigest()
def run(args):
 result=subprocess.run([str(x) for x in args],capture_output=True,text=True,timeout=240)
 check(result.returncode==0,f'Command failed ({result.returncode}): {args[0]} {result.stdout[-300:]} {result.stderr[-300:]}')
 return result

try:
 if (app/'unins000.exe').exists():
  run([app/'unins000.exe','/VERYSILENT','/SUPPRESSMSGBOXES','/NORESTART',f'/LOG={area}/preclean.log'])
  check(not (app/'user-data-test').exists(),'Previous test uninstall retained its data')
 app.mkdir(parents=True,exist_ok=True)
 run([setup,'/VERYSILENT','/SUPPRESSMSGBOXES','/NORESTART','/SP-','/TESTINSTALL=1',f'/DIR={app}',f'/LOG={area}/install.log'])
 install_log=(area/'install.log').read_text(encoding='utf-8-sig')
 check('Prerequisite OK: .NET Framework 4.8 or newer' in install_log,'Installed .NET not detected')
 check('Prerequisite OK: Microsoft Visual C++ x64' in install_log,'Installed Visual C++ not detected')
 data=app/'user-data-test';check((app/'installed.ini').exists() and data.is_dir(),'Installer marker/data root missing')
 profile=data/'profile.json';profile.write_text('{"Version":1,"Name":"Preserve this fixture","Coins":1234,"drawing":"keep"}',encoding='utf-8')
 (data/'music').mkdir(exist_ok=True);(data/'music/imported.wav').write_bytes(b'user music fixture')
 (data/'notes.bin').write_bytes(b'notebook fixture\x00\xff')
 preserved={name:sha(data/name) for name in ['profile.json','music/imported.wav','notes.bin']}
 unchanged=app/'assets/paper.png';unchanged_hash=sha(unchanged);unchanged_time=unchanged.stat().st_mtime_ns
 def request(name,items,removed=()):
  stage=data/'updates'/name;(stage/'files').mkdir(parents=True,exist_ok=True);files=[]
  for relative,contents in items:
   path=stage/'files'/relative;path.parent.mkdir(parents=True,exist_ok=True);path.write_bytes(contents)
   files.append(dict(path=relative,size=len(contents),sha256=sha(path),pack='test.zip'))
  previous=json.loads((app/'payload-manifest.json').read_text(encoding='utf-8'));mapping={x['path']:x for x in previous['files']}
  for file in files:mapping[file['path']]=file
  for relative in removed:mapping.pop(relative,None)
  previous.update(version='0.4.1',files=list(mapping.values()))
  (stage/'next-manifest.json').write_text(json.dumps(previous),encoding='utf-8')
  payload=dict(root=str(app),stage=str(stage),version='0.4.1',product='5C35749C-14DE-49BC-AED7-E462359A73A4',data=str(data),process=0,files=files,removed=list(removed))
  target=stage/'request.json';target.write_text(json.dumps(payload),encoding='utf-8');return target
 target=request('success',[('README.md',b'Changed application documentation'),('assets/update-added.txt',b'New managed file')])
 run([app/'LittleNotebook.Update.exe','--apply-test',target])
 check((app/'README.md').read_bytes()==b'Changed application documentation','Changed file not replaced')
 check((app/'assets/update-added.txt').read_bytes()==b'New managed file','New file not added')
 check(sha(unchanged)==unchanged_hash and unchanged.stat().st_mtime_ns==unchanged_time,'Unchanged file was rewritten')
 for relative,digest in preserved.items():check(sha(data/relative)==digest,'User data changed during update: '+relative)
 check('Version=0.4.1' in (app/'installed.ini').read_text(encoding='utf-8-sig'),'Installed version not updated')
 # Lock the second target: first write must roll back, without touching saves.
 locked=app/'assets/locked-test.txt';locked.write_bytes(b'locked original')
 target=request('rollback',[('README.md',b'this must roll back'),('assets/locked-test.txt',b'replacement')])
 kernel=ctypes.windll.kernel32;kernel.CreateFileW.restype=ctypes.c_void_p
 handle=kernel.CreateFileW(str(locked),0x80000000,0,None,3,0x80,None)
 check(handle not in (None,ctypes.c_void_p(-1).value),'Could not create exclusive lock fixture')
 try:failed=subprocess.run([str(app/'LittleNotebook.Update.exe'),'--apply-test',str(target)],capture_output=True,timeout=30)
 finally:kernel.CloseHandle(ctypes.c_void_p(handle))
 check(failed.returncode!=0,'Locked-file update incorrectly succeeded')
 check((app/'README.md').read_bytes()==b'Changed application documentation','Rollback did not restore earlier changed file')
 check(locked.read_bytes()==b'locked original','Rollback damaged locked original')
 for relative,digest in preserved.items():check(sha(data/relative)==digest,'Rollback changed user data')
 target=request('bad-hash',[('README.md',b'verification fixture')]);payload=json.loads(target.read_text());payload['files'][0]['sha256']='0'*64;target.write_text(json.dumps(payload))
 failed=subprocess.run([str(app/'LittleNotebook.Update.exe'),'--apply-test',str(target)],capture_output=True,timeout=30)
 check(failed.returncode!=0 and (app/'README.md').read_bytes()==b'Changed application documentation','Hash mismatch was not rejected before writing')
 # The uninstaller must remove update-added managed files too, plus all test data.
 locked.unlink()
 uninstaller=app/'unins000.exe';run([uninstaller,'/VERYSILENT','/SUPPRESSMSGBOXES','/NORESTART',f'/LOG={area}/uninstall.log'])
 for _ in range(50):
  if not app.exists():break
  time.sleep(.1)
 check(not data.exists(),'Uninstall retained saved states/imports/cache')
 check(not (app/'assets').exists() and not (app/'speech-runtime').exists() and not (app/'content').exists(),'Uninstall retained payload folders')
 check(not (app/'LittleNotebook.exe').exists() and not (app/'LittleNotebook.Update.exe').exists(),'Uninstall retained executables')
 check(not (app/'assets/update-added.txt').exists(),'Uninstall retained update-added file')
 report.write_text(f'PASS: {checks} real installer/update/uninstaller lifecycle checks. Fresh per-user test installation, changed/new files only, unchanged timestamps, byte-identical saves/imports/notes, locked-file rollback, hash rejection, removal of app-owned data and update-added files. Test install was isolated from the real profile.\n',encoding='utf-8')
 print(report.read_text())
except Exception as error:
 report.write_text('FAIL: '+repr(error),encoding='utf-8');raise
