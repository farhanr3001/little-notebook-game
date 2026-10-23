"""Download public domain animal photographs; store source credits with the game."""
from pathlib import Path
import urllib.request,re,html,json,time,io
from PIL import Image
root=Path(__file__).resolve().parent.parent
dest=root/'assets/pictures';dest.mkdir(exist_ok=True)
names=['Youngkitten.JPG','Guinea pig young.jpg',"Kitten sleeping on rabbit's head.jpg",'Kätzchen.JPG','A Guinea pig and its carrot.jpg']
records=[]
for i,name in enumerate(names):
 page='https://commons.wikimedia.org/wiki/File:'+urllib.parse.quote(name)
 def get(url):
  for attempt in range(5):
   try:
    with urllib.request.urlopen(urllib.request.Request(url,headers={'User-Agent':'LittleNotebookGame/0.6 (animal picture puzzle; source attribution bundled)'}),timeout=60) as response:return response.read()
   except urllib.error.HTTPError as error:
    if error.code!=429:raise
    time.sleep(12)
  raise RuntimeError('Image host rate limit: '+url)
 path=dest/('animal-'+str(i)+'.jpg')
 if not path.exists():
  source=get(page).decode('utf-8');url=html.unescape(re.search(r'class="fullImageLink"[^>]*>\s*<a href="([^"]+)',source).group(1))
  image=Image.open(io.BytesIO(get(url))).convert('RGB');image.thumbnail((1000,1000));image.save(path,quality=90)
 records.append(dict(file=path.name,source=page,license='Public domain / CC0; see source page',title=name))
 print(path.name,flush=True)
(dest/'credits.json').write_text(json.dumps(records,ensure_ascii=False,indent=2),encoding='utf-8')
