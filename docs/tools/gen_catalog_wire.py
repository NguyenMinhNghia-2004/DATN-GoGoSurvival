"""Slice 8: Wire SV_SkillCatalog.activeSkills[*].zSkillConfigRef + passiveSkills[*].zSkillConfigRef
to the matching Luzart ZSk_*.asset / ZPs_*.asset.

This bridges the lightweight catalog (used by UI) to the deep Luzart pipeline (runtime).
"""
import os, sys, io, json, re
sys.stdout = io.TextIOWrapper(sys.stdout.buffer, encoding='utf-8')

INDEX = json.load(open('docs/asset-guid-index.json', encoding='utf-8'))

# skillId in catalog -> Luzart config name
MAP = {
    'Sk_Kunai':          'ZSk_Kunai',
    'Sk_Boomerang':      'ZSk_Boomerang',
    'Sk_Brick':          'ZSk_Brick',
    'Sk_DrillShot':      'ZSk_DrillShot',
    'Sk_Durian':         'ZSk_Durian',
    'Sk_Forcefield':     'ZSk_Forcefield',
    'Sk_Guardian':       'ZSk_Guardian',
    'Sk_Molotov':        'ZSk_Molotov',
    'Sk_RPG':            'ZSk_RPG',
    'Sk_SoccerBall':     'ZSk_SoccerBall',
    'Ps_HiPowerMagnet':  'ZPs_HiPowerMagnet',
    'Ps_FitnessGuide':   'ZPs_FitnessGuide',
    'Ps_AmmoThruster':   'ZPs_AmmoThruster',
    'Ps_HEFuel':         'ZPs_HEFuel',
    'Ps_EnergyDrink':    'ZPs_EnergyDrink',
    'Ps_ExoBracer':      'ZPs_ExoBracer',
    'Ps_EnergyCube':     'ZPs_EnergyCube',
    'Ps_OilBond':        'ZPs_OilBond',
    'Ps_RoninOyoroi':    'ZPs_RoninOyoroi',
    'Ps_SportsShoes':    'ZPs_SportsShoes',
    'Ps_HiPowerBullet':  'ZPs_HiPowerBullet',
    'Ps_KogaNinjaScroll':'ZPs_KogaNinjaScroll',
}

cat_path = 'Assets/_Main/Data/Skills/SV_SkillCatalog.asset'
with open(cat_path, 'r', encoding='utf-8') as f: yaml = f.read()
orig = yaml

changed = 0
for sk_id, cfg_name in MAP.items():
    cfg_guid = INDEX.get(cfg_name, {}).get('guid')
    if not cfg_guid:
        print(f'  !! missing {cfg_name}'); continue
    # Find the block for skillId: <sk_id> ... zSkillConfigRef: {fileID: 0}
    # Use a regex that finds the next occurrence of zSkillConfigRef: {fileID: 0} after the sk_id line
    pat = re.compile(
        r'(skillId:\s*' + re.escape(sk_id) + r'.*?zSkillConfigRef:\s*)\{fileID:\s*0\}',
        re.S
    )
    new_yaml, n = pat.subn(
        lambda m: m.group(1) + f'{{fileID: 11400000, guid: {cfg_guid}, type: 2}}',
        yaml, count=1
    )
    if n:
        yaml = new_yaml
        changed += 1
        print(f'  wired {sk_id} -> {cfg_name}')
    else:
        print(f'  (skip) {sk_id} -- already wired or pattern not matched')

if yaml != orig:
    with open(cat_path, 'w', encoding='utf-8', newline='\n') as f: f.write(yaml)
print(f'\nDone. {changed} catalog entries wired to Luzart configs.')
