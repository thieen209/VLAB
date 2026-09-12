"""Static Unity identity audit, with a dependency closure per production scene."""
from pathlib import Path
import json, re, hashlib
import unify_projects as u

idx=u.index(u.ROOT)
packages={}
for p in (u.ROOT/'Library/PackageCache').glob('*/**/*.meta'):
    match=u.GUID.search(p.read_bytes())
    if match: packages[match[1].decode()]=p.with_suffix('')
scenes=re.findall(r'    path: (.+)', (u.ROOT/'ProjectSettings/EditorBuildSettings.asset').read_text())
report={'projects': {},'scenes': {},'duplicate_guids':{},'migrated_assets':[]}
for name in ['VLAB','VLAB(UI)','VLABChemistry']:
    project=u.WORK/name
    report['projects'][name]={
        'unity':(project/'ProjectSettings/ProjectVersion.txt').read_text().splitlines()[0],
        'packages':json.loads((project/'Packages/manifest.json').read_text())['dependencies'],
        'scenes':[str(p.relative_to(project)) for p in (project/'Assets').rglob('*.unity') if not any(x in p.parts for x in ['Samples','TextMesh Pro'])]}
allguid={}
for p in (u.ROOT/'Assets').rglob('*.meta'):
    match=u.GUID.search(p.read_bytes())
    if match:allguid.setdefault(match[1].decode(),[]).append(str(p.relative_to(u.ROOT)))
report['duplicate_guids']={g:paths for g,paths in allguid.items() if len(paths)>1}
for scene in scenes:
    pending=[u.ROOT/scene];visited=set();missing={};scripts=set()
    while pending:
        p=pending.pop()
        if p in visited or not p.exists() or p.is_dir():continue
        visited.add(p)
        if p.suffix not in u.TEXT:continue
        for guid in u.GUID.findall(p.read_bytes()):
            g=guid.decode()
            if g.startswith('0000000000000000'):continue
            if g in idx:
                dep=idx[g];pending.append(dep)
                if dep.suffix=='.cs':scripts.add(str(dep.relative_to(u.ROOT)))
            elif g not in packages:missing.setdefault(g,[]).append(str(p.relative_to(u.ROOT)))
    report['scenes'][scene]={'asset_count':len(visited),'missing_guids':missing,'scripts':sorted(scripts)}
for source in [u.WORK/'VLAB(UI)',u.WORK/'VLABChemistry']:
    for path in (u.ROOT/'Assets').rglob('*'):
        if not path.is_file() or path.suffix=='.meta':continue
        rel=path.relative_to(u.ROOT)
        donor=source/rel
        if donor.exists() and ('MainMenu' in path.parts or 'VLABChemistryLab' in path.parts or path.name=='ChemistryLab.unity' or 'Public' in path.parts):
            report['migrated_assets'].append({'path':str(rel),'source':source.name,'bytes':path.stat().st_size})
out=u.ROOT/'docs/Integration/audit.json';out.parent.mkdir(parents=True,exist_ok=True)
out.write_text(json.dumps(report,indent=2),encoding='utf-8')
print('Duplicate GUIDs:',len(report['duplicate_guids']))
for name,scene in report['scenes'].items(): print(name, 'missing GUIDs:',len(scene['missing_guids']))
print('Root Physics scripts:', '\n'.join(report['scenes']['Assets/VLAB/PhysicsLab/Scenes/PhysicsLab_Base.unity']['scripts']))
