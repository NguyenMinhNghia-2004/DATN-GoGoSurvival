"""
Generate ItemConfig SOs from GDD with PER-QUALITY variants (correct GDD-driven shape).

For each of 26 equipment items × 5 quality tiers (Normal/Good/Better/Excellent/Epic),
emit one ItemConfig_<Item>_<Quality>.asset. Each has:
  - _eRarity: matches quality (Normal/Good→Rare=0, Better/Excellent→Epic=1, Epic→Legend=2)
  - _eTypeItem: from GDD Category (Weapon=0, Armor=1, Necklace=2, Belt=3, Gloves=4, Shoes=5)
  - upgradeItemConfigs[0..(max_lvl-1)]:
      modifierFactors[0] = AddNormal of base stat (ATK or HPMax),
                          value = base + bonus_per_lvl × level
      modifierFactors[1+] = AddRatio if grade skill text matches "ATK +X%" or "HP +X%" pattern
                            (only for Better+ tiers; lower tiers omit grade skill)

Universal max_lv when GDD only specifies for Kunai: [Normal=10, Good=20, Better=40, Excellent=60, Epic=100]

Writes to Assets/_Main/Data/Items/<Quality>/ItemConfig_<Item>_<Quality>.asset.
Run: python docs/tools/gen_item_configs_v2.py
"""
import os, sys, io, json, uuid, re
sys.stdout = io.TextIOWrapper(sys.stdout.buffer, encoding='utf-8')

PARSED = json.load(open('docs/gdd-dump/equipment_parsed.json', encoding='utf-8'))
INDEX = json.load(open('docs/asset-guid-index.json', encoding='utf-8'))

ITEM_CONFIG_SCRIPT = INDEX['ItemConfig']['guid']

QUALITIES = ['Normal', 'Good', 'Better', 'Excellent', 'Epic']
# Rarity mapping → Luzart ERarity enum {Rare=0, Epic=1, Legend=2}
QUALITY_RARITY = {'Normal': 0, 'Good': 0, 'Better': 1, 'Excellent': 1, 'Epic': 2}
# Default max_levels when GDD only states for Kunai
DEFAULT_MAX_LVL = {'Normal': 10, 'Good': 20, 'Better': 40, 'Excellent': 60, 'Epic': 100}

# GDD Category → Luzart ETypeItem enum
CATEGORY_ETYPE = {
    'Weapon':    0,
    'Armor':     1,
    'Neacklace': 2,  # GDD typo
    'Necklace':  2,
    'Belt':      3,
    'Gloves':    4,
    'Shoes':     5,
}

def g_or_die(name):
    e = INDEX.get(name)
    if not e:
        raise RuntimeError(f'Missing asset GUID: {name}')
    return e['guid']

# Pre-resolve modifier GUIDs (only AddNormal + AddRatio needed)
MOD_GUIDS = {
    ('ATK',   'AddNormal'): g_or_die('Mod_Player_InGame_ATK_AddNormal'),
    ('ATK',   'AddRatio'):  g_or_die('Mod_Player_InGame_ATK_AddRatio'),
    ('HPMax', 'AddNormal'): g_or_die('Mod_Player_InGame_HPMax_AddNormal'),
    ('HPMax', 'AddRatio'):  g_or_die('Mod_Player_InGame_HPMax_AddRatio'),
    ('Heal',  'AddNormal'): g_or_die('Mod_Player_InGame_Heal_AddNormal'),
    ('Cooldown','AddRatio'): g_or_die('Mod_Player_InGame_Cooldown_AddRatio'),
    ('Speed', 'AddRatio'):  g_or_die('Mod_Player_InGame_Speed_AddRatio'),
}
MODDEF_GUIDS = {
    ('ATK',   'AddNormal'): g_or_die('ModDef_Player_AddNormal_ATK'),
    ('ATK',   'AddRatio'):  g_or_die('ModDef_Player_AddRatio_ATK'),
    ('HPMax', 'AddNormal'): g_or_die('ModDef_Player_AddNormal_HPMax'),
    ('HPMax', 'AddRatio'):  g_or_die('ModDef_Player_AddRatio_HPMax'),
    ('Heal',  'AddNormal'): g_or_die('ModDef_Player_AddNormal_Heal'),
    ('Cooldown','AddRatio'): g_or_die('ModDef_Player_AddRatio_Cooldown'),
    ('Speed', 'AddRatio'):  g_or_die('ModDef_Player_AddRatio_Speed'),
}

def gen(): return uuid.uuid4().hex

def so_header(script_guid, name, class_id=''):
    return (f'%YAML 1.1\n%TAG !u! tag:unity3d.com,2011:\n--- !u!114 &11400000\n'
            f'MonoBehaviour:\n  m_ObjectHideFlags: 0\n  m_CorrespondingSourceObject: {{fileID: 0}}\n'
            f'  m_PrefabInstance: {{fileID: 0}}\n  m_PrefabAsset: {{fileID: 0}}\n  m_GameObject: {{fileID: 0}}\n'
            f'  m_Enabled: 1\n  m_EditorHideFlags: 0\n  m_Script: {{fileID: 11500000, guid: {script_guid}, type: 3}}\n'
            f'  m_Name: {name}\n  m_EditorClassIdentifier: {class_id}\n')

def write_asset(folder, fname, body, sub_guid=None):
    os.makedirs(folder, exist_ok=True)
    path = os.path.join(folder, fname).replace(os.sep, '/')
    guid = sub_guid or gen()
    with open(path, 'w', encoding='utf-8', newline='\n') as f: f.write(body)
    with open(path + '.meta', 'w', encoding='utf-8', newline='\n') as f:
        f.write(f'fileFormatVersion: 2\nguid: {guid}\nNativeFormatImporter:\n  externalObjects: {{}}\n  mainObjectFileID: 11400000\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n')
    return guid

def parse_grade_skill(text):
    """Return list of (stat_name, kind, value) tuples parsed from grade skill text.
    Examples:
      'ATK +10%'    -> [('ATK', 'AddRatio', 0.10)]
      'HP +15%'     -> [('HPMax', 'AddRatio', 0.15)]
      'Heal 3% HP/5s' -> [('Heal', 'AddNormal', 0.006)]  # 3%/5s = 0.6%/s = 0.006 (per-second)
      'Damage to Elites and Bosses +20%' -> skipped (no generic stat)
    """
    if not text or not isinstance(text, str) or text.strip().lower() == 'n/a':
        return []
    out = []
    # ATK +X%
    m = re.search(r'\bATK\s*\+(\d+(?:\.\d+)?)\s*%', text, re.I)
    if m: out.append(('ATK', 'AddRatio', float(m.group(1)) / 100))
    # HP +X% (Max HP)
    m = re.search(r'\bHP\s*\+(\d+(?:\.\d+)?)\s*%', text, re.I)
    if m: out.append(('HPMax', 'AddRatio', float(m.group(1)) / 100))
    # Heal X% HP/5s -> per-second rate
    m = re.search(r'Heal\s+(\d+(?:\.\d+)?)\s*%\s*HP\s*/\s*5\s*s', text, re.I)
    if m: out.append(('Heal', 'AddNormal', float(m.group(1)) / 100 / 5))
    # Cooldown -X% (e.g. "-20% CD")
    m = re.search(r'-\s*(\d+(?:\.\d+)?)\s*%\s*CD', text, re.I)
    if m: out.append(('Cooldown', 'AddRatio', -float(m.group(1)) / 100))
    # Movement Speed +X%
    m = re.search(r'Movement\s+[Ss]peed\s*\+(\d+(?:\.\d+)?)\s*%', text, re.I)
    if m: out.append(('Speed', 'AddRatio', float(m.group(1)) / 100))
    return out

def safe_filename(s):
    s = re.sub(r'[^A-Za-z0-9]+', '_', s.strip())
    return s.strip('_')

def emit_modifier_factor_yaml(stat, kind, value):
    mod_guid = MOD_GUIDS.get((stat, kind))
    moddef_guid = MODDEF_GUIDS.get((stat, kind))
    if not (mod_guid and moddef_guid): return ''
    return ('    - factor:\n'
            f'        definition: {{fileID: 11400000, guid: {moddef_guid}, type: 2}}\n'
            f'        value:\n          mode: 0\n          constant: {round(value, 4)}\n          asset: {{fileID: 0}}\n'
            f'      modifier: {{fileID: 11400000, guid: {mod_guid}, type: 2}}\n')

count = 0
all_created = []
for item in PARSED:
    name = item['name']
    stat_cat = item['statCategory']  # 'ATK' or 'HP'
    primary_stat = 'HPMax' if stat_cat == 'HP' else 'ATK'
    category = item['category']
    etype = CATEGORY_ETYPE.get((category or '').strip())
    if etype is None:
        print(f'  !! unknown category {category!r} for {name}, skip')
        continue
    safe_name = safe_filename(name)

    for q in QUALITIES:
        q_data = item['quality'][q]
        base = q_data['base']
        bonus = q_data['bonus_per_level']
        max_lvl = q_data['max_level'] or DEFAULT_MAX_LVL[q]
        max_lvl = int(max_lvl)
        if base is None or bonus is None:
            continue

        # If lvl count > 100 (Epic), cap to 100 to avoid huge YAML files.
        max_lvl = min(max_lvl, 100)

        # Grade skill modifier factor (only Better+ tiers per GDD)
        grade_factors = parse_grade_skill(q_data['grade_skill_text']) if q in ('Better', 'Excellent', 'Epic') else []

        asset_name = f'ItemConfig_{safe_name}_{q}'
        folder = f'Assets/_Main/Data/Items/{q}'
        if asset_name in INDEX:
            print(f'  (skip existing) {asset_name}')
            continue

        body = so_header(ITEM_CONFIG_SCRIPT, asset_name, 'Assembly-CSharp::Luzart.ItemConfig')
        body += f'  _id: {asset_name}\n'
        body += f'  _name: {name} ({q})\n'
        body += '  _sprite: {fileID: 0}\n'
        body += f'  _eRarity: {QUALITY_RARITY[q]}\n'
        body += f'  _eTypeItem: {etype}\n'
        body += '  unlockable: {fileID: 0}\n'
        body += '  assetLevel: {fileID: 0}\n'
        body += '  upgradeItemConfigs:\n'

        for lvl in range(max_lvl):
            stat_val = base + bonus * lvl
            body += '  - modifierFactors:\n'
            body += emit_modifier_factor_yaml(primary_stat, 'AddNormal', stat_val)
            for (gs_stat, gs_kind, gs_val) in grade_factors:
                body += emit_modifier_factor_yaml(gs_stat, gs_kind, gs_val)

        body += '  artifactModifierFactor: []\n'
        guid = write_asset(folder, asset_name + '.asset', body)
        all_created.append((asset_name, guid))
        count += 1
    print(f'  ✓ {name} → 5 quality variants')

with open('docs/created-guids-itemconfigs-v2.json', 'w', encoding='utf-8') as f:
    json.dump(dict(all_created), f, ensure_ascii=False, indent=1)
print(f'\nDone. {count} ItemConfig SOs created (26 items × 5 qualities).')
