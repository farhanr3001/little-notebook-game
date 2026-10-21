"""Retrieve three freely downloadable tracks from the artist's public album page."""
import http.cookiejar, json, re, urllib.parse, urllib.request
from pathlib import Path
root = Path(__file__).resolve().parents[1]
base = 'https://purrplecat.itch.io/just-relax'
opener = urllib.request.build_opener(urllib.request.HTTPCookieProcessor(http.cookiejar.CookieJar()))
opener.addheaders = [('User-Agent', 'Mozilla/5.0'), ('Referer', base)]
page = opener.open(base, timeout=30).read().decode()
token = re.search(r'name="csrf_token" value="([^"]+)"', page).group(1)
tracks = [('16782850', 'beach-resort', 'Beach Resort'), ('16782852', 'road-trip', 'Road Trip'), ('16782851', 'look-on-the-bright-side', 'Look on the Bright Side')]
for upload, name, title in tracks:
    request = urllib.request.Request(base + '/file/' + upload + '?source=view_game&as_props=1', data=urllib.parse.urlencode({'csrf_token': token}).encode())
    data = json.loads(opener.open(request, timeout=30).read())
    if 'url' not in data:
        raise RuntimeError('No public download returned for ' + title)
    audio = opener.open(data['url'], timeout=60).read()
    if len(audio) < 100000:
        raise RuntimeError('Unexpected audio response for ' + title)
    (root / 'assets' / (name + '.mp3')).write_bytes(audio)
    print(title + ': ' + str(len(audio)) + ' bytes')
