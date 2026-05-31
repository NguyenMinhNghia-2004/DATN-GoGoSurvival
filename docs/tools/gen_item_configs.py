"""
Generate 26 Luzart ItemConfig SOs mirroring existing Eq_*.asset (custom EquipmentData class).
Each ItemConfig has upgradeItemConfigs with modifierFactors that reference the
new Mod_Player_InGame_*_AddNormal modifiers from gen_modifier_infra.py.

Slot mapping (EquipmentData slot enum):
  0 = Weapon          → ATK
  1 = Armor           → HPMax
  2 = Necklace        → ATK
  3 = Belt            → HPMax
  4 = Gloves          → ATK
  5 = Shoes           → HPMax

For each item we create 10 upgradeItemConfigs (one per enhancement level, since
EquipmentData.maxEnhanceLevel = 10). Each level's modifierFactor.value uses the
item's atkByQuality[0]/hpByQuality[0] (Normal quality base) + enhanceBonusPerLevel
× level × base (5% per level).

Output: Assets/_Main/Data/Items/ItemConfig_<EqName>.asset (with .meta).
"""
import os, sys, io, json, uuid, re
sys.stdout = io.TextIOWrapper(sys.stdout.buffer, encoding='utf-8')

INDEX = json.load(open('docs/asset-guid-index.json', encoding='utf-8'))

ITEM_CONFIG_SCRIPT = INDEX['ItemConfig']['guid']  # the .cs's guid
print(f'ItemConfig script guid: {ITEM_CONFIG_SCRIPT}')

# Slot → (StatName, ModDef name)  — using AddNormal modifier (flat additive)
# EquipmentData.slot enum is unknown (class deleted). Use data-driven heuristic:
# if atkByQuality has non-zero values, treat as ATK item; otherwise HPMax.
def infer_stat(data):
    atks = data['atkByQuality']
    hps = data['hpByQuality']
    if atks and any(v > 0 for v in atks):
        return 'ATK', 'ATK-item (necklace/gloves/weapon)'
    if hps and any(v > 0 for v in hps):
        return 'HPMax', 'HP-item (armor/belt/shoes)'
    return 'ATK', 'unknown-default'

# kept for documentation; not used now
SLOT_STAT_MAP = {}

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

def parse_eq_yaml(path):
    """Pull slot, atkByQuality, hpByQuality, equipName, enhanceBonusPerLevel from Eq_*.asset YAML."""
    with open(path, 'r', encoding='utf-8') as f: content = f.read()
    def grab(field):
        m = re.search(rf'^\s*{field}:\s*(.*)$', content, re.M)
        return m.group(1).strip() if m else None
    def grab_list(field):
        # find field: then read indented '- N' lines
        m = re.search(rf'^\s*{field}:\s*\n((?:\s*-\s*\S+\n)+)', content, re.M)
        if not m: return []
        lines = m.group(1).split('\n')
        out = []
        for ln in lines:
            ln = ln.strip()
            if ln.startswith('- '):
                try:
                    out.append(float(ln[2:].strip()))
                except: pass
        return out
    return dict(
        equipName=grab('equipName'),
        equipId=grab('equipId'),
        slot=int(grab('slot') or 0),
        atkByQuality=grab_list('atkByQuality'),
        hpByQuality=grab_list('hpByQuality'),
        enhanceBonus=float(grab('enhanceBonusPerLevel') or '0.05'),
        maxEnhance=int(grab('maxEnhanceLevel') or '10'),
    )

created = []
folder = 'Assets/_Main/Data/Items'
eq_dir = 'Assets/_Main/Data/Equipment'

for fname in sorted(os.listdir(eq_dir)):
    if not fname.startswith('Eq_') or not fname.endswith('.asset'): continue
    eq_path = f'{eq_dir}/{fname}'
    eq_name = os.path.splitext(fname)[0]
    item_name = eq_name.replace('Eq_', 'ItemConfig_')
    out_path = f'{folder}/{item_name}.asset'
    if os.path.exists(out_path):
        continue
    data = parse_eq_yaml(eq_path)
    stat_name, slot_label = infer_stat(data)
    mod_name = f'Mod_Player_InGame_{stat_name}_AddNormal'
    moddef_name = f'ModDef_Player_AddNormal_{stat_name}'
    mod_guid = INDEX.get(mod_name, {}).get('guid')
    moddef_guid = INDEX.get(moddef_name, {}).get('guid')
    if not mod_guid or not moddef_guid:
        print(f'  !! missing modifier infra for {stat_name}, skip {item_name}')
        continue

    # Base value (Normal quality, lvl 1) — atkByQuality[0] or hpByQuality[0]
    qual_list = data['atkByQuality'] if stat_name == 'ATK' else data['hpByQuality']
    base_value = qual_list[0] if qual_list else 0
    bonus = data['enhanceBonus']  # per-level multiplier of base

    # ItemConfig YAML body
    body = so_header(ITEM_CONFIG_SCRIPT, item_name, 'Assembly-CSharp::Luzart.ItemConfig')
    body += f'  _id: {item_name}\n'
    body += f'  _name: {data["equipName"] or item_name}\n'
    body += '  _sprite: {fileID: 0}\n'
    # Rarity heuristic: Rare=0, Epic=1, Legend=2 — we can't tell from EquipmentData, default Rare
    body += '  _eRarity: 0\n'
    # Item type — map EquipmentData.slot to Luzart ETypeItem (same enum order)
    item_type = data['slot']
    body += f'  _eTypeItem: {item_type}\n'
    body += '  unlockable: {fileID: 0}\n'
    body += '  assetLevel: {fileID: 0}\n'

    # upgradeItemConfigs: 10 levels, base × (1 + bonus × level)
    body += '  upgradeItemConfigs:\n'
    for lvl in range(data['maxEnhance']):
        value = round(base_value * (1 + bonus * lvl), 2)
        body += '  - modifierFactors:\n'
        body += '    - factor:\n'
        body += f'        definition: {{fileID: 11400000, guid: {moddef_guid}, type: 2}}\n'
        body += f'        value:\n          mode: 0\n          constant: {value}\n          asset: {{fileID: 0}}\n'
        body += f'      modifier: {{fileID: 11400000, guid: {mod_guid}, type: 2}}\n'
    body += '  artifactModifierFactor: []\n'

    g = write_asset(folder, item_name + '.asset', body)
    created.append((item_name, g, stat_name, slot_label, base_value))
    print(f'  + {item_name} ({slot_label}, {stat_name} base={base_value} × ×10 levels)')

with open('docs/created-guids-itemconfigs.json', 'w', encoding='utf-8') as f:
    json.dump({n: g for n, g, *_ in created}, f, ensure_ascii=False, indent=1)

print(f'\nDone. {len(created)} ItemConfig SOs created.')
