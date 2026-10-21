"""Package a native review build while excluding every personal/test save."""
from pathlib import Path
from shutil import copy2
from zipfile import ZipFile, ZIP_DEFLATED

root = Path(__file__).resolve().parent.parent
release = root / 'Little Notebook v0.3.4'
archive = root / 'Little-Notebook-v0.3.4.zip'
for name in ['README.md', 'PLAN.md', 'ASSETS.md', 'TESTING.md', 'CONTENT.md', 'STRESS_REPORT.md']:
    copy2(root / name, release / name)
copy2(root / 'tools/radio-palette-prompts.json', release / 'radio-palette-prompts.json')
files = [p for p in release.rglob('*') if p.is_file() and p.relative_to(release).parts[0] != 'data' and '__pycache__' not in p.parts and p.suffix != '.pyc']
allowed_roots = {'assets', 'content', 'speech-runtime'}
allowed_files = {'LittleNotebook.exe', 'LittleNotebook.exe.config', 'README.md', 'PLAN.md', 'ASSETS.md', 'TESTING.md', 'CONTENT.md', 'STRESS_REPORT.md', 'radio-palette-prompts.json'}
assert all(p.relative_to(release).parts[0] in allowed_roots | allowed_files for p in files)
assert (release / 'assets/pets.png').exists()
with ZipFile(archive, 'w', ZIP_DEFLATED) as z:
    for path in sorted(files):
        z.write(path, path.relative_to(root))
with ZipFile(archive) as z:
    assert z.testzip() is None
    assert not any(n.startswith(release.name + '/data/') or 'profile.json' in n or 'test-output' in n for n in z.namelist())
    assert any(n.endswith('/assets/pets.png') for n in z.namelist())
    assert sum(n.endswith('.wav') and '/assets/speech/' in n for n in z.namelist()) == 93
    for required in ['python.exe', 'worker.py', 'models/kokoro-v1.0.int8.onnx', 'models/ar_JO-kareem-medium.onnx']:
        assert release.name + '/speech-runtime/' + required in z.namelist()
    assert len(z.namelist()) == len(files)
print(f'PASS: {archive.name}, {archive.stat().st_size / 1048576:.1f} MB, {len(files)} files, archive integrity checked; no personal or test saves.')
