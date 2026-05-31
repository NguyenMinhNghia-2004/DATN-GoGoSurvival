"""Slice 6+7: Populate passive skill upgrade configs (12 passives x 5 levels = 60 SOs)
+ wire ZPs_*.upgradeConfigs.

Passive Skills from GDD `Pasive_Skills.json`:
  Each passive is a single-stat % bonus, 5 tiers.
Strategy: store value in upgradeConfig.stats with appropriate Luzart StatType.
Runtime modifier application is OUT OF SCOPE tonight (needs AssetModifier infra).
"""
import os, sys, io, json, uuid, re

sys.stdout = io.TextIOWrapper(sys.stdout.buffer, encoding='utf-8')

INDEX = json.load(open('docs/asset-guid-index.json', encoding='utf-8'))
NEW_INDEX = json.load(open('docs/created-guids.json', encoding='utf-8'))

# Merge: pull stat-def guids out of INDEX (some are pre-existing)
ASSET_GUIDS = {}
for name in ('StatDef_ATK','StatDef_HPMax','StatDef_Cooldown','StatDef_FireSpeed','StatDef_RangeFind',
             'StatDef_Speed','StatDef_TiLeChiMang','StatDef_SatThuongChiMang','StatDef_Armor',
             'StatDef_Heal','StatDef_Luck','StatDef_XPMultiplier',
             'StatDef_RadiusCollider','StatDef_AmountProjectile','StatDef_TimeBreak',
             'StatDef_RadiusExplosion','StatDef_ATKMultiplierExplosion','StatDef_TimeDelayExplosion'):
    e = INDEX.get(name) or {}
    if e: ASSET_GUIDS[name] = e['guid']
# fallback to created
for k, v in NEW_INDEX.items():
    ASSET_GUIDS.setdefault(k, v)

# Skill config and behavior guids
PASSIVE_NAMES = [
    'ZPs_HiPowerMagnet', 'ZPs_FitnessGuide', 'ZPs_AmmoThruster', 'ZPs_HEFuel',
    'ZPs_EnergyDrink', 'ZPs_ExoBracer', 'ZPs_EnergyCube', 'ZPs_OilBond',
    'ZPs_RoninOyoroi', 'ZPs_SportsShoes', 'ZPs_HiPowerBullet', 'ZPs_KogaNinjaScroll',
]
for n in PASSIVE_NAMES:
    e = INDEX.get(n)
    if e: ASSET_GUIDS[n] = e['guid']

UPGRADE_SCRIPT = '62a382ef75172ce4b8729e9e67b9363a'  # ZSkillUpgradeConfig

# (key, statDef, values[5])
PASSIVES = {
    'ZPs_HiPowerMagnet':   ('StatDef_Luck',           [1, 2, 3, 4, 5],          '+100% / +200% / +300% / +400% / +500% pickup range'),
    'ZPs_FitnessGuide':    ('StatDef_HPMax',          [0.2, 0.4, 0.6, 0.8, 1.0],'+20% .. +100% Max HP'),
    'ZPs_AmmoThruster':    ('StatDef_FireSpeed',      [0.1, 0.2, 0.3, 0.4, 0.5],'+10% .. +50% bullet speed'),
    'ZPs_HEFuel':          ('StatDef_RangeFind',      [0.1, 0.2, 0.3, 0.4, 0.5],'+10% .. +50% range'),
    'ZPs_EnergyDrink':     ('StatDef_Heal',           [0.01,0.02,0.03,0.04,0.05],'Restore 1% .. 5% HP /5s'),
    'ZPs_ExoBracer':       ('StatDef_Heal',           [0.1, 0.2, 0.3, 0.4, 0.5],'Over-time +10% .. +50% (no StatType yet)'),
    'ZPs_EnergyCube':      ('StatDef_Cooldown',       [0.08,0.16,0.24,0.32,0.40],'Attack CD -8% .. -40%'),
    'ZPs_OilBond':         ('StatDef_Luck',           [0.08,0.16,0.24,0.32,0.40],'Gold gain +8% .. +40% (uses Luck slot)'),
    'ZPs_RoninOyoroi':     ('StatDef_Armor',          [0.1, 0.2, 0.3, 0.4, 0.5],'Damage taken -10% .. -50%'),
    'ZPs_SportsShoes':     ('StatDef_Speed',          [0.1, 0.2, 0.3, 0.4, 0.5],'Movement Speed +10% .. +50%'),
    'ZPs_HiPowerBullet':   ('StatDef_ATK',            [0.1, 0.2, 0.3, 0.4, 0.5],'ATK +10% .. +50%'),
    'ZPs_KogaNinjaScroll': ('StatDef_XPMultiplier',   [0.08,0.16,0.24,0.32,0.40],'EXP gain +8% .. +40%'),
}

def so_header(script_guid, name, class_id):
    return (f'%YAML 1.1\n%TAG !u! tag:unity3d.com,2011:\n--- !u!114 &11400000\n'
            f'MonoBehaviour:\n  m_ObjectHideFlags: 0\n  m_CorrespondingSourceObject: {{fileID: 0}}\n'
            f'  m_PrefabInstance: {{fileID: 0}}\n  m_PrefabAsset: {{fileID: 0}}\n  m_GameObject: {{fileID: 0}}\n'
            f'  m_Enabled: 1\n  m_EditorHideFlags: 0\n  m_Script: {{fileID: 11500000, guid: {script_guid}, type: 3}}\n'
            f'  m_Name: {name}\n  m_EditorClassIdentifier: {class_id}\n')

def write_asset(folder, fname, yaml_body, sub_guid=None):
    os.makedirs(folder, exist_ok=True)
    path = os.path.join(folder, fname).replace(os.sep, '/')
    meta_path = path + '.meta'
    guid = sub_guid or uuid.uuid4().hex
    with open(path, 'w', encoding='utf-8', newline='\n') as f: f.write(yaml_body)
    with open(meta_path, 'w', encoding='utf-8', newline='\n') as f:
        f.write(f'fileFormatVersion: 2\nguid: {guid}\nNativeFormatImporter:\n  externalObjects: {{}}\n  mainObjectFileID: 11400000\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n')
    return guid

# === Slice 6: Generate 60 upgrade configs ===
folder = 'Assets/_Main/Data/Skills/Upgrades/Passive'
created = {}
print('=== SLICE 6: Passive skill upgrade configs ===')
for ps_name, (sd_name, values, descr_template) in PASSIVES.items():
    sd_guid = ASSET_GUIDS.get(sd_name)
    if not sd_guid:
        print(f'  !! missing {sd_name}, skip {ps_name}')
        continue
    up_prefix = ps_name.replace('ZPs_', 'ZPsUp_')
    for lvl, val in enumerate(values, start=1):
        asset_name = f'{up_prefix}_Lv{lvl}'
        target_path = f'{folder}/{asset_name}.asset'
        if os.path.exists(target_path):
            continue
        body = so_header(UPGRADE_SCRIPT, asset_name, 'Assembly-CSharp::Luzart.ZSkillUpgradeConfig')
        body += f'  _id: {asset_name}\n'
        body += '  skillInfor:\n    sprite: {fileID: 0}\n'
        body += f'    skillName: {ps_name.replace("ZPs_", "")} ' + ('★' * lvl) + '\n'
        body += f'    skillDetail: {descr_template}\n'
        body += '  stats:\n'
        body += f'  - definition: {{fileID: 11400000, guid: {sd_guid}, type: 2}}\n'
        body += f'    value:\n      mode: 0\n      constant: {val}\n      asset: {{fileID: 0}}\n'
        body += '  modifierFactors: []\n'
        g = write_asset(folder, asset_name + '.asset', body)
        created[asset_name] = g
    print(f'  + {ps_name} Lv1..Lv5 ({sd_name})')

# === Slice 7: Wire ZPs_*.upgradeConfigs ===
print('\n=== SLICE 7: Wire ZPs_*.upgradeConfigs ===')
for ps_name in PASSIVES.keys():
    sk_path = INDEX.get(ps_name, {}).get('path') or f'Assets/_Main/Data/Skills/Configs/{ps_name}.asset'
    if not os.path.exists(sk_path):
        print(f'  !! skip missing {sk_path}')
        continue
    with open(sk_path, 'r', encoding='utf-8') as f: yaml = f.read()
    up_prefix = ps_name.replace('ZPs_', 'ZPsUp_')
    refs = []
    for lvl in range(1, 6):
        n = f'{up_prefix}_Lv{lvl}'
        g = created.get(n) or INDEX.get(n, {}).get('guid')
        if g:
            refs.append(f'  - {{fileID: 11400000, guid: {g}, type: 2}}')
    if not refs:
        print(f'  !! no upgrade refs for {ps_name}')
        continue
    block = '\n'.join(refs)
    new_yaml = re.sub(r'upgradeConfigs:\s*\[\]', f'upgradeConfigs:\n{block}', yaml)
    if new_yaml != yaml:
        with open(sk_path, 'w', encoding='utf-8', newline='\n') as f: f.write(new_yaml)
        print(f'  wired {ps_name}.upgradeConfigs -> 5 levels')
    else:
        print(f'  (no-change) {ps_name}')

# Save created guids for index
with open('docs/created-guids-passives.json', 'w', encoding='utf-8') as f:
    json.dump(created, f, ensure_ascii=False, indent=1)
print(f'\nDone. {len(created)} new passive upgrade configs created.')
