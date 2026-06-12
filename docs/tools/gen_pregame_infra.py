"""
Generate the PreGame modifier+aggregation pipeline for 8 player stats, mirroring
the existing InGame layer but REUSING the shared ModDef_Player_*_<Stat> definitions.
Output: Assets/_Main/Data/ModifierAndPreGame/Player/<Stat>/...
Run from project root:  python docs/tools/gen_pregame_infra.py
"""
import os, sys, io, json, uuid
sys.stdout = io.TextIOWrapper(sys.stdout.buffer, encoding='utf-8')

INGAME = json.load(open('docs/created-guids-modifier-infra.json', encoding='utf-8'))

SG = {  # script guids (identical to gen_modifier_infra.py)
    'AssetModifier_ContributeToAggregatedNumber': 'c6e54ce9a0886de4fbf23f641cc5ade3',
    'AssetNumber_Constant':      '538fdae1998489b498206b2d9d675495',
    'AssetNumber_Aggregation':   'b691dab8d6c2f16449978ce0d28b190c',
    'AssetNumber_SimpleBoosted': '8657fc305e8686c429dcfb1d5b10d381',
}
STATS = [('HPMax',1000.0),('ATK',10.0),('Speed',4.0),('Cooldown',1.0),
         ('FireSpeed',10.0),('Heal',0.0),('Armor',0.0),('RangeFind',8.0)]
CREATED = {}

def gen(): return uuid.uuid4().hex
def write_asset(folder, fname, body):
    os.makedirs(folder, exist_ok=True)
    path = os.path.join(folder, fname).replace(os.sep, '/')
    guid = gen()
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
def ref(g): return ('{fileID: 11400000, guid: ' + g + ', type: 2}') if g else '{fileID: 0}'

def make_constant(name, folder, value):
    b = header(SG['AssetNumber_Constant'], name, 'Assembly-CSharp::Luzart.AssetNumber_Constant')
    b += f'  _id: {name}\n  value: {value}\n'
    CREATED[name] = write_asset(folder, name + '.asset', b)
def make_aggregation(name, folder, mode, numbers):
    b = header(SG['AssetNumber_Aggregation'], name, 'Assembly-CSharp::Luzart.AssetNumber_Aggregation')
    b += f'  _id: {name}\n  aggregationMode: {mode}\n'
    if not numbers:
        b += '  numbers: []\n'
    else:
        b += '  numbers:\n'
        for (m, c, g) in numbers:
            b += f'  - mode: {m}\n    constant: {c}\n    asset: {ref(g)}\n'
    CREATED[name] = write_asset(folder, name + '.asset', b)
def make_simple_boosted(name, folder, base_g, add_g, mul_g):
    b = header(SG['AssetNumber_SimpleBoosted'], name, 'Assembly-CSharp::Luzart.AssetNumber_SimpleBoosted')
    b += f'  _id: {name}\n'
    b += f'  baseNumber:\n    mode: 1\n    constant: 0\n    asset: {ref(base_g)}\n'
    b += f'  addNumber:\n    mode: 1\n    constant: 0\n    asset: {ref(add_g)}\n'
    b += f'  multiplyNumber:\n    mode: 1\n    constant: 0\n    asset: {ref(mul_g)}\n'
    b += '  powNumber:\n    mode: 0\n    constant: 1\n    asset: {fileID: 0}\n'
    CREATED[name] = write_asset(folder, name + '.asset', b)
def make_modifier(name, folder, def_g, contrib_g):
    b = header(SG['AssetModifier_ContributeToAggregatedNumber'], name,
               'Assembly-CSharp::Luzart.AssetModifier_ContributeToAggregatedNumber')
    b += f'  _id: {name}\n  definition: {ref(def_g)}\n  factors: []\n  contributionNumber: {ref(contrib_g)}\n'
    CREATED[name] = write_asset(folder, name + '.asset', b)

base_folder = 'Assets/_Main/Data/ModifierAndPreGame/Player'
for stat, base_value in STATS:
    sf = f'{base_folder}/{stat}'
    make_constant(f'Number_Player_PreGame_Base_{stat}', sf, base_value)
    make_aggregation(f'Number_Player_PreGame_TotalAddNormal_{stat}', sf, 0, [])
    make_aggregation(f'Number_Player_PreGame_TotalAddRatio_{stat}', sf, 0, [])
    addratio_g = CREATED[f'Number_Player_PreGame_TotalAddRatio_{stat}']
    make_aggregation(f'Number_Player_PreGame_TotalAddSubRatio_{stat}', sf, 0,
                     [(0, 1, None), (1, 0, addratio_g)])
    make_aggregation(f'Number_Player_PreGame_TotalMultiply_{stat}', sf, 1, [(0, 1, None)])
    make_simple_boosted(f'Number_Player_PreGame_Final_{stat}', sf,
                        CREATED[f'Number_Player_PreGame_Base_{stat}'],
                        CREATED[f'Number_Player_PreGame_TotalAddNormal_{stat}'],
                        CREATED[f'Number_Player_PreGame_TotalAddSubRatio_{stat}'])
    # REUSE existing ModDef guids from the InGame index
    make_modifier(f'Mod_Player_PreGame_{stat}_AddNormal', sf,
                  INGAME[f'ModDef_Player_AddNormal_{stat}'],
                  CREATED[f'Number_Player_PreGame_TotalAddNormal_{stat}'])
    make_modifier(f'Mod_Player_PreGame_{stat}_AddRatio', sf,
                  INGAME[f'ModDef_Player_AddRatio_{stat}'],
                  CREATED[f'Number_Player_PreGame_TotalAddRatio_{stat}'])
    make_modifier(f'Mod_Player_PreGame_{stat}_Multiply', sf,
                  INGAME[f'ModDef_Player_Multiply_{stat}'],
                  CREATED[f'Number_Player_PreGame_TotalMultiply_{stat}'])
    print(f'  {stat}: 9 SOs')

json.dump(CREATED, open('docs/created-guids-pregame-infra.json', 'w', encoding='utf-8'),
          ensure_ascii=False, indent=1)
print(f'\nDone. {len(CREATED)} PreGame SOs created.')
