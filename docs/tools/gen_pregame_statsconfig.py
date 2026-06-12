"""
Author the IO_Training-parity PreGame stat-config layer:
  - 8 PlayerStat_PreGame_<Stat> (AssetStat: definition + value=AssetNumber->PreGame_Final)
  - 1 StatsConfig_PreGame (groups those 8 PlayerStats)
Output: Assets/_Main/Data/ModifierAndPreGame/Player/StatsConfig/
Run from project root:  python docs/tools/gen_pregame_statsconfig.py
"""
import os, sys, io, json, uuid
sys.stdout = io.TextIOWrapper(sys.stdout.buffer, encoding='utf-8')

PRE = json.load(open('docs/created-guids-pregame-infra.json', encoding='utf-8'))

SCRIPT_ASSETSTAT   = 'a81f815e768953f46a87d795774ccae8'  # Luzart.AssetStat
SCRIPT_STATSCONFIG = '6532e7bf15b799441925602f097c513b'  # Luzart.StatsConfig

# Stat -> AssetStatDefinition guid (from Data/StatDefinitions / existing Stat_Player_* defs)
STATDEF = {
    'HPMax':     'f4742bfd14a464611bef2137b2db1e63',
    'ATK':       'd23f4e56a2b9f4891b81b18fe885bd9b',
    'Speed':     '1546ad6be185440d1aa7b52b80a091ed',
    'Cooldown':  '4dd538c3a70d24748837e476a326131d',
    'FireSpeed': 'afaff64959479453abb513b76ead9cc0',
    'Heal':      'ee22559ac3e17478a90b836734970b7b',
    'Armor':     '33ab417a8a2f141f2b5643f267df250d',
    'RangeFind': '202dc84ee937243c6a9b56374e56765d',
}
STATS = ['HPMax','ATK','Speed','Cooldown','FireSpeed','Heal','Armor','RangeFind']

FOLDER = 'Assets/_Main/Data/ModifierAndPreGame/Player/StatsConfig'
CREATED = {}

def write_asset(fname, body):
    os.makedirs(FOLDER, exist_ok=True)
    path = os.path.join(FOLDER, fname).replace(os.sep, '/')
    guid = uuid.uuid4().hex
    with open(path, 'w', encoding='utf-8', newline='\n') as f: f.write(body)
    with open(path + '.meta', 'w', encoding='utf-8', newline='\n') as f:
        f.write(f'fileFormatVersion: 2\nguid: {guid}\nNativeFormatImporter:\n'
                f'  externalObjects: {{}}\n  mainObjectFileID: 11400000\n'
                f'  userData: \n  assetBundleName: \n  assetBundleVariant: \n')
    return guid

def header(sg, name, cid):
    return ('%YAML 1.1\n%TAG !u! tag:unity3d.com,2011:\n--- !u!114 &11400000\n'
            'MonoBehaviour:\n  m_ObjectHideFlags: 0\n  m_CorrespondingSourceObject: {fileID: 0}\n'
            '  m_PrefabInstance: {fileID: 0}\n  m_PrefabAsset: {fileID: 0}\n  m_GameObject: {fileID: 0}\n'
            f'  m_Enabled: 1\n  m_EditorHideFlags: 0\n  m_Script: {{fileID: 11500000, guid: {sg}, type: 3}}\n'
            f'  m_Name: {name}\n  m_EditorClassIdentifier: {cid}\n')

# 1) PlayerStat_PreGame_<Stat>
for s in STATS:
    name = f'PlayerStat_PreGame_{s}'
    final_g = PRE[f'Number_Player_PreGame_Final_{s}']
    def_g = STATDEF[s]
    b = header(SCRIPT_ASSETSTAT, name, 'Assembly-CSharp::Luzart.AssetStat')
    b += f'  _id: {name}\n'
    b += '  definition: {fileID: 11400000, guid: ' + def_g + ', type: 2}\n'
    b += '  value:\n    mode: 1\n    constant: 0\n    asset: {fileID: 11400000, guid: ' + final_g + ', type: 2}\n'
    CREATED[name] = write_asset(name + '.asset', b)
    print(f'  {name}')

# 2) StatsConfig_PreGame
b = header(SCRIPT_STATSCONFIG, 'StatsConfig_PreGame', 'Assembly-CSharp::Luzart.StatsConfig')
b += '  _id: StatsConfig_PreGame\n  assetStats:\n'
for s in STATS:
    b += '  - {fileID: 11400000, guid: ' + CREATED[f'PlayerStat_PreGame_{s}'] + ', type: 2}\n'
CREATED['StatsConfig_PreGame'] = write_asset('StatsConfig_PreGame.asset', b)
print('  StatsConfig_PreGame')

json.dump(CREATED, open('docs/created-guids-pregame-statsconfig.json', 'w', encoding='utf-8'),
          ensure_ascii=False, indent=1)
print(f'\nDone. {len(CREATED)} SOs (8 PlayerStat_PreGame + 1 StatsConfig_PreGame).')
