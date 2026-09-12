"""Read-only source audit of this project; writes a reproducible inventory report."""
from pathlib import Path
import json
import re

ROOT = Path(__file__).resolve().parents[1]
GUID = re.compile(rb'guid: ([0-9a-f]{32})')
index = {}
duplicates = {}
for folder in ('Assets', 'Packages', 'Library/PackageCache'):
    for meta in (ROOT / folder).rglob('*.meta'):
        match = GUID.search(meta.read_bytes())
        if not match:
            continue
        guid = match[1].decode()
        path = meta.with_suffix('')
        if guid in index and folder == 'Assets':
            duplicates.setdefault(guid, [str(index[guid].relative_to(ROOT))]).append(str(path.relative_to(ROOT)))
        index[guid] = path

report = {'duplicates': duplicates, 'scenes': {}, 'input_actions': [], 'scripts': {}, 'render_assets': []}
for path in (ROOT / 'Assets').rglob('*'):
    if not path.is_file():
        continue
    name = str(path.relative_to(ROOT))
    if path.suffix == '.inputactions':
        report['input_actions'].append(name)
    if path.suffix in ('.unity', '.prefab'):
        data = path.read_bytes()
        refs = set(g.decode() for g in GUID.findall(data) if not g.startswith(b'0000000000000000'))
        scripts = sorted(str(index[g].relative_to(ROOT)) for g in refs if g in index and index[g].suffix == '.cs')
        report['scenes'][name] = {
            'missing_references': sorted(g for g in refs if g not in index),
            'scripts': scripts,
            'canvas_modes': re.findall(r'm_RenderMode: (\d+)', data.decode('utf-8', errors='replace')),
            'objects': re.findall(r'^  m_Name: (.+)$', data.decode('utf-8', errors='replace'), re.M),
        }
    if path.suffix == '.cs' and 'Samples' not in path.parts and 'TextMesh Pro' not in path.parts:
        data = path.read_text(encoding='utf-8-sig')
        findings = [f'{i}: {line.strip()}' for i, line in enumerate(data.splitlines(), 1)
                    if re.search(r'DontDestroyOnLoad|SceneManager\.Load|FindObject|GameObject.Find|BLE|Bluetooth|UUID|Update\(', line)]
        if findings:
            report['scripts'][name] = findings
    if path.suffix in ('.mat', '.shader') or (path.suffix == '.asset' and re.search(r'Pipeline|Render|Volume|Lighting|PostProcess', path.name, re.I)):
        report['render_assets'].append(name)
out = ROOT / 'docs/Production/source-audit.json'
out.parent.mkdir(parents=True, exist_ok=True)
out.write_text(json.dumps(report, ensure_ascii=False, indent=2), encoding='utf-8')
production = [s for s in report['scenes'] if 'Samples' not in s and 'TextMesh Pro' not in s]
missing = {s: report['scenes'][s]['missing_references'] for s in production if report['scenes'][s]['missing_references']}
print(json.dumps({'scene_and_prefab_count': len(production), 'duplicate_guids': duplicates, 'missing_references': missing, 'report': str(out)}, ensure_ascii=False))
