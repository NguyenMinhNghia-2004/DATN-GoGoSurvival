"""Build a name -> {path, guid} index of all .meta-backed assets under Assets/."""
import os, sys, io, json, re

sys.stdout = io.TextIOWrapper(sys.stdout.buffer, encoding='utf-8')

ROOT = 'Assets'
INDEX_PATH = 'docs/asset-guid-index.json'

index = {}
for root, dirs, files in os.walk(ROOT):
    if 'TextMesh Pro' in root:
        continue
    for f in files:
        if not f.endswith('.meta'):
            continue
        meta_path = os.path.join(root, f)
        asset_path = meta_path[:-5].replace(os.sep, '/')
        try:
            with open(meta_path, 'r', encoding='utf-8') as fp:
                content = fp.read()
        except Exception:
            continue
        m = re.search(r'^guid:\s*([a-f0-9]+)', content, re.M)
        if not m:
            continue
        guid = m.group(1)
        name = os.path.splitext(os.path.basename(asset_path))[0]
        # prefer first found, but allow lookup-by-path too
        if name not in index:
            index[name] = {'path': asset_path, 'guid': guid}
        # also store by full path so collisions can still be resolved
        index['__by_path__/' + asset_path] = {'path': asset_path, 'guid': guid, 'name': name}

os.makedirs(os.path.dirname(INDEX_PATH), exist_ok=True)
with open(INDEX_PATH, 'w', encoding='utf-8') as fp:
    json.dump(index, fp, ensure_ascii=False, indent=1)

bare = {k: v for k, v in index.items() if not k.startswith('__by_path__/')}
print(f'indexed {len(bare)} assets (plus {len(index)-len(bare)} path entries) -> {INDEX_PATH}')

targets = ['Bullet', 'Bullet1', 'Bullet2', 'Bullet3', 'Bullet4', 'Bullet5', 'Rocket', 'Brick',
           'Aiguille', 'huixuanbiao', 'banzhaun', 'Liulian', 'ranshaodan', 'FireBall',
           'StatDef_ATK', 'StatDef_HPMax', 'StatDef_Cooldown', 'StatDef_FireSpeed', 'StatDef_RangeFind',
           'StatDef_Speed', 'ZSk_Kunai', 'ZSk_Kunai_Behavior', 'SkillDatabase']
print('\n-- key target lookups --')
for t in targets:
    print(f'  {t}: {bare.get(t, "NOT FOUND")}')
