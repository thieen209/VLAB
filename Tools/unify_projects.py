"""Trace Unity GUID dependencies, preserve root identities and record every migration."""
from pathlib import Path
import hashlib, json, re, shutil, subprocess

WORK = Path(__file__).resolve().parents[2]
ROOT = WORK / 'VLAB'
GUID = re.compile(rb'guid:\s*([0-9a-f]{32})')
TEXT = {'.unity', '.prefab', '.asset', '.mat', '.controller', '.overrideController', '.anim', '.meta', '.asmdef'}

def index(project):
    result = {}
    for p in (project / 'Assets').rglob('*.meta'):
        match = GUID.search(p.read_bytes())
        if match:
            result[match[1].decode()] = p.with_suffix('')
    return result

def migrate(source, seeds, report):
    src, dst = index(source), index(ROOT)
    todo, visited = list(seeds), set()
    while todo:
        path = todo.pop()
        if path in visited or not path.exists():
            continue
        visited.add(path)
        if path.is_dir():
            todo.extend(path.rglob('*'))
            continue
        if path.suffix == '.meta':
            continue
        rel = path.relative_to(source)
        meta = Path(str(path) + '.meta')
        match = GUID.search(meta.read_bytes()) if meta.exists() else None
        existing = dst.get(match[1].decode()) if match else None
        target = ROOT / rel
        if existing is not None:
            report['reused'].append(str(rel))
        elif target.exists():
            raise RuntimeError(f'Path identity conflict: {rel}')
        else:
            target.parent.mkdir(parents=True, exist_ok=True)
            shutil.copy2(path, target)
            if meta.exists(): shutil.copy2(meta, Path(str(target) + '.meta'))
            if match: dst[match[1].decode()] = target
            report['copied'].append(str(rel))
            # Retain folder GUIDs too, without overwriting a root folder identity.
            for folder in path.parents:
                if folder == source: break
                fm = Path(str(folder) + '.meta')
                tm = ROOT / fm.relative_to(source)
                if fm.exists() and not tm.exists():
                    tm.parent.mkdir(parents=True, exist_ok=True)
                    shutil.copy2(fm, tm)
        # Existing root assets own their dependencies. Do not chase a donor variant.
        if existing is not None:
            continue
        for file in (path, meta):
            if file.exists() and file.suffix in TEXT:
                for g in GUID.findall(file.read_bytes()):
                    dep = src.get(g.decode())
                    if dep is not None and g.decode() not in dst: todo.append(dep)

def main():
    report = {'copied': [], 'reused': []}
    ui = WORK / 'VLAB(UI)'
    chemistry = WORK / 'VLABChemistry'
    migrate(ui, [ui / 'Assets/VLAB/MainMenu'], report)
    migrate(chemistry, [chemistry / 'Assets/ChemistryLab.unity', chemistry / 'Assets/VLABChemistryLab/Scripts', chemistry / 'Assets/VLABChemistryLab/Input/Resources'], report)
    out = ROOT / 'docs/Integration/migration-last-run.json'
    out.parent.mkdir(parents=True, exist_ok=True)
    out.write_text(json.dumps(report, indent=2), encoding='utf-8')
    print(f"Migrated {len(report['copied'])} assets; reused {len(report['reused'])} root assets.")

if __name__ == '__main__': main()
