"""Publish an already tested release. Uses GitHub CLI or Git Credential Manager, never prints credentials."""
from pathlib import Path
import argparse, json, subprocess, urllib.request, urllib.error, mimetypes
root=Path(__file__).resolve().parent.parent
parser=argparse.ArgumentParser();parser.add_argument('--publish',action='store_true');args=parser.parse_args()
manifest=json.loads((root/'releases/update-manifest.json').read_text(encoding='utf-8'))
repo=manifest['repository'];version=manifest['version'];base=f'https://api.github.com/repos/{repo}'
credential=subprocess.run(['git','credential','fill'],input='protocol=https\nhost=github.com\n\n',text=True,capture_output=True,check=True)
values=dict(line.split('=',1) for line in credential.stdout.splitlines() if '=' in line)
token=values.get('password');assert token,'No GitHub credential available'
def api(url,body=None,method=None,content_type='application/json'):
 data=json.dumps(body).encode() if isinstance(body,dict) else body
 request=urllib.request.Request(url,data=data,method=method,headers={'Authorization':f'Bearer {token}','User-Agent':'LittleNotebook-release','Accept':'application/vnd.github+json','Content-Type':content_type,'X-GitHub-Api-Version':'2022-11-28'})
 with urllib.request.urlopen(request,timeout=300) as response:return json.load(response)
try:release=api(base+f'/releases/tags/v{version}')
except urllib.error.HTTPError as error:
 if error.code!=404:raise
 release=api(base+'/releases',dict(tag_name='v'+version,name='Little Notebook '+version,draft=True,body=(root/'RELEASE_NOTES.md').read_text(encoding='utf-8') if (root/'RELEASE_NOTES.md').exists() else 'Little Notebook '+version+'. Download the setup installer below.'))
files=[root/f'releases/LittleNotebook-Setup-{version}.exe',root/'releases/update-manifest.json']+sorted((root/'releases/update-packs').glob('*.zip'))
existing={asset['name'] for asset in release['assets']}
for path in files:
 if path.name in existing:continue
 print('Uploading',path.name,flush=True)
 api(release['upload_url'].split('{')[0]+'?name='+path.name,path.read_bytes(),'POST',mimetypes.guess_type(path.name)[0] or 'application/octet-stream')
if args.publish:release=api(base+f"/releases/{release['id']}",dict(draft=False),'PATCH')
print(('Published: ' if args.publish else 'Draft: ')+release['html_url'])
