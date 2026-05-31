"""Wire ZSk_*_Behavior.skillBehaviorStats with AmountProjectile + TimeBreak refs.
Bomb-type behaviors also get RadiusExplosion + TimeDelayExplosion.
"""
import os, sys, io, json, re
sys.stdout = io.TextIOWrapper(sys.stdout.buffer, encoding='utf-8')
INDEX = json.load(open('docs/asset-guid-index.json', encoding='utf-8'))

def g(name):
    return INDEX[name]['guid']

AP   = g('StatDef_AmountProjectile')
TB   = g('StatDef_TimeBreak')
RE   = g('StatDef_RadiusExplosion')
TDE  = g('StatDef_TimeDelayExplosion')

# Normal projectile behaviors: just AmountProjectile + TimeBreak
NORMAL = ['ZSk_Kunai_Behavior', 'ZSk_Boomerang_Behavior', 'ZSk_DrillShot_Behavior',
          'ZSk_Durian_Behavior', 'ZSk_Forcefield_Behavior', 'ZSk_Guardian_Behavior',
          'ZSk_SoccerBall_Behavior']
# Bomb-type behaviors: add explosion stats
BOMB   = ['ZSk_Brick_Behavior', 'ZSk_Molotov_Behavior', 'ZSk_RPG_Behavior']

def make_block(defs):
    lines = []
    for d in defs:
        lines.append(f'  - {{fileID: 11400000, guid: {d}, type: 2}}')
    return '\n'.join(lines)

NORMAL_BLOCK = make_block([AP, TB])
BOMB_BLOCK   = make_block([AP, TB, RE, TDE])

changed = 0
for beh_name in NORMAL + BOMB:
    path = INDEX.get(beh_name, {}).get('path')
    if not path or not os.path.exists(path):
        print(f'  !! missing {beh_name}')
        continue
    with open(path, 'r', encoding='utf-8') as f: yaml = f.read()
    block = BOMB_BLOCK if beh_name in BOMB else NORMAL_BLOCK
    new = re.sub(r'skillBehaviorStats:\s*\[\]', f'skillBehaviorStats:\n{block}', yaml)
    if new != yaml:
        with open(path, 'w', encoding='utf-8', newline='\n') as f: f.write(new)
        kind = 'BOMB' if beh_name in BOMB else 'NORMAL'
        print(f'  wired {beh_name} ({kind})')
        changed += 1
    else:
        print(f'  (no-change) {beh_name}')
print(f'\n{changed} behavior SOs wired with skillBehaviorStats')
