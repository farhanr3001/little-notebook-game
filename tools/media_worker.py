"""Single-request UTF-8 audio import worker. JSON progress on stdout; no browser cookies."""
import base64, html, json, re, subprocess, sys, time, urllib.parse, urllib.request
from html.parser import HTMLParser
from pathlib import Path
root=Path(__file__).resolve().parent;media=root/'media'
sys.path.insert(0,str(media/'Lib'))
MAX_BYTES=512*1024*1024
def emit(**message):print(json.dumps(message,ensure_ascii=False),flush=True)
def valid_url(url):
 value=urllib.parse.urlparse(url)
 return value.scheme=='https' and not value.username and not value.password and value.port in (None,443) and value.hostname in {'youtube.com','www.youtube.com','m.youtube.com','music.youtube.com','youtu.be','www.youtu.be','tiktok.com','www.tiktok.com','vm.tiktok.com','vt.tiktok.com'}
def filename(title):
 name=re.sub(r'[\x00-\x1f\\/*?:"<>|]','',title).strip().rstrip('.')[:110] or 'Imported music'
 return '_'+name if re.match(r'^(CON|PRN|AUX|NUL|COM[1-9]|LPT[1-9])(?:\.|$)',name,re.I) else name
class Links(HTMLParser):
 def __init__(self):super().__init__();self.links=[]
 def handle_starttag(self,tag,attrs):
  values=dict(attrs)
  if tag=='a' and 'download_link' in values.get('class',''):self.links.append(values.get('href',''))
def fetch(url,**kwargs):return urllib.request.urlopen(urllib.request.Request(url,headers={'User-Agent':'Mozilla/5.0 (Windows NT 10.0; Win64; x64)'},**kwargs),timeout=20)
def sound_info(url):
 title=''
 try:
  with fetch('https://www.tiktok.com/oembed?url='+urllib.parse.quote(url,safe='')) as response:info=json.load(response);title=info.get('title','').removeprefix('♬ ').strip()
 except Exception:pass
 if not title:
  slug=urllib.parse.unquote(urllib.parse.urlparse(url).path.split('/music/')[-1]);title=re.sub(r'-\d+$','',slug).replace('-',' ').strip() or 'TikTok Sound'
 page='https://ssstik.io/download-tiktok-mp3'
 with fetch(page) as response:
  content=response.read(2*1024*1024).decode('utf-8',errors='replace');cookies='; '.join(c.split(';')[0] for c in response.headers.get_all('Set-Cookie',[]))
 token=re.search(r"s_tt\s*=\s*'([^']+)'",content)
 headers={'User-Agent':'Mozilla/5.0','Content-Type':'application/x-www-form-urlencoded; charset=UTF-8','HX-Request':'true','HX-Current-URL':page,'Referer':page,'Origin':'https://ssstik.io','Cookie':cookies}
 payload=urllib.parse.urlencode(dict(id=url,locale='en',tt=token[1] if token else '')).encode()
 request=urllib.request.Request('https://ssstik.io/abc?url=dl',data=payload,headers=headers)
 with urllib.request.urlopen(request,timeout=25) as response:result=response.read(2*1024*1024).decode('utf-8',errors='replace')
 links=Links();links.feed(result)
 for link in links.links:
  if 'tikcdn.io/ssstik/m/' in link:
   try:link=base64.b64decode(link.split('/ssstik/m/')[-1]).decode('utf-8')
   except Exception:continue
  parsed=urllib.parse.urlparse(html.unescape(link))
  if parsed.scheme=='https' and parsed.hostname and not parsed.username:return title,html.unescape(link)
 raise ValueError('This TikTok sound could not be retrieved. Try a public TikTok video link instead.')
def convert(source,target):
 emit(kind='progress',stage='converting',percent=95)
 subprocess.run([str(media/'ffmpeg.exe'),'-nostdin','-y','-v','error','-i',str(source),'-vn','-c:a','libmp3lame','-q:a','2',str(target)],check=True,capture_output=True,timeout=300,creationflags=0x08000000)
def tiktok_video_info(url):
 with fetch('https://www.tikwm.com/api/?url='+urllib.parse.quote(url,safe='')) as response:info=json.load(response)
 data=info.get('data') or {};direct=data.get('music')
 if info.get('code')!=0 or not direct:raise ValueError('This TikTok video audio is unavailable. Try another public link.')
 return (data.get('music_info') or {}).get('title') or data.get('title') or 'TikTok Sound',direct
def stream_audio(title,direct,folder):
 parsed=urllib.parse.urlparse(direct)
 if parsed.scheme!='https' or not parsed.hostname or parsed.username:raise ValueError('The sound source returned an invalid audio link.')
 source=folder/'source.partial';downloaded=0
 with fetch(direct) as response,source.open('wb') as output:
  expected=int(response.headers.get('Content-Length') or 0)
  if expected>MAX_BYTES:raise ValueError('This audio file is too large to import.')
  while chunk:=response.read(65536):
   downloaded+=len(chunk)
   if downloaded>MAX_BYTES:raise ValueError('This audio file is too large to import.')
   output.write(chunk);emit(kind='progress',stage='downloading',percent=min(90,downloaded*90/expected) if expected else None)
 if not downloaded or expected and downloaded!=expected:raise ValueError('The audio download was incomplete.')
 target=folder/(filename(title)+'.mp3');convert(source,target);source.unlink();return target
def validate_audio(path):
 result=subprocess.run([str(media/'ffprobe.exe'),'-v','error','-select_streams','a:0','-show_entries','stream=codec_name','-of','json',str(path)],check=True,capture_output=True,text=True,timeout=30,creationflags=0x08000000)
 streams=json.loads(result.stdout).get('streams',[])
 if not streams or streams[0].get('codec_name')!='mp3' or path.stat().st_size<512:raise ValueError('The link did not produce a valid MP3 audio file.')
def download(url,folder):
 if not valid_url(url):raise ValueError('Paste a public HTTPS YouTube or TikTok link.')
 folder=Path(folder).resolve();folder.mkdir(parents=True,exist_ok=True)
 emit(kind='progress',stage='connecting',percent=0)
 if 'tiktok.com' in urllib.parse.urlparse(url).hostname and '/music/' in urllib.parse.urlparse(url).path:
  title,direct=sound_info(url);target=stream_audio(title,direct,folder)
 else:
  import yt_dlp
  last=[0.]
  def progress(info):
   if time.monotonic()-last[0]<.2 and info.get('status')!='finished':return
   last[0]=time.monotonic();total=info.get('total_bytes') or info.get('total_bytes_estimate');size=info.get('downloaded_bytes',0)
   emit(kind='progress',stage='downloading',percent=min(90,size*90/total) if total else None,downloaded=size)
  class Logger:
   def debug(self,message):pass
   def warning(self,message):pass
   def error(self,message):pass
  options=dict(format='bestaudio/best',noplaylist=True,ignoreconfig=True,quiet=True,no_warnings=True,logger=Logger(),socket_timeout=20,retries=2,fragment_retries=2,max_filesize=MAX_BYTES,ffmpeg_location=str(media),js_runtimes={'deno':{'path':str(media/'deno.exe')}},cachedir=str(folder.parent.parent/'media-cache'),outtmpl=str(folder/'%(title).110s [%(id)s].%(ext)s'),windowsfilenames=True,restrictfilenames=False,progress_hooks=[progress],postprocessor_hooks=[lambda info:emit(kind='progress',stage='converting',percent=95)],postprocessors=[{'key':'FFmpegExtractAudio','preferredcodec':'mp3','preferredquality':'2'}])
  try:
   with yt_dlp.YoutubeDL(options) as ydl:
    info=ydl.extract_info(url,download=False)
    if not info or info.get('_type')=='playlist' or info.get('is_live'):raise ValueError('Choose one public video or sound, rather than a live stream or playlist.')
    title=info.get('title') or 'Imported music'
    ydl.process_ie_result(info,download=True)
   files=list(folder.glob('*.mp3'))
   if len(files)!=1:raise ValueError('The link did not produce an MP3 file.')
   target=folder/(filename(title)+'.mp3')
   if files[0]!=target:files[0].rename(target)
  except Exception:
   if 'tiktok.com' not in urllib.parse.urlparse(url).hostname:raise
   for partial in folder.iterdir():
    if partial.is_file():partial.unlink()
   title,direct=tiktok_video_info(url);target=stream_audio(title,direct,folder)
 validate_audio(target);return target,title
def main():
 sys.stdin.reconfigure(encoding='utf-8');sys.stdout.reconfigure(encoding='utf-8')
 try:
  request=json.loads(sys.stdin.readline());path,title=download(request['url'],request['output']);emit(kind='done',path=str(path),title=title)
 except Exception as error:emit(kind='error',message=str(error)[-700:]);return 1
 return 0
if __name__=='__main__':raise SystemExit(main())
