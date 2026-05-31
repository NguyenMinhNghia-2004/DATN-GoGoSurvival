"""
Overnight SO generator for GoGo Survival.
Generates Unity .asset + .meta files for:
  - Missing AssetStatDefinition SOs (Slice 1)
  - 10 ProjectileConfig SOs (Slice 2)
  - 50 ZSkillUpgradeConfig SOs (Slice 3)
  - Player StatsConfig + AssetStat SOs (Slice 5)
And updates existing ZSk_*.asset + ZSk_*_Behavior.asset to wire refs (Slice 4).

Run from project root: python docs/tools/gen_so.py
"""
import os, sys, io, json, uuid, re

sys.stdout = io.TextIOWrapper(sys.stdout.buffer, encoding='utf-8')

ROOT = '.'  # run from project root
INDEX = json.load(open('docs/asset-guid-index.json', encoding='utf-8'))

# Script GUIDs (verified via build_guid_index.py earlier)
SCRIPT_GUIDS = {
    'AssetStatDefinition':                   '7b790334d46c2614f905156fa4594000',
    'AssetStat':                             'a81f815e768953f46a87d795774ccae8',
    'StatsConfig':                           '6532e7bf15b799441925602f097c513b',
    'NormalProjectileConfig':                '4ccf55ea415b7284e9e332b31e93b44b',
    'LaserProjectileConfig':                 '4514705b2b5707a42a5dc0b1db0bd059',
    'BombProjectileConfig':                  '6482187d153b58f4bb6d87f0f53d1259',
    'BoomerangProjectileConfig':             '54efdf20f540bca4f8767b2f61ba1b9a',
    'LightingProjectileConfig':              'aa9b1c5d47a75814196c9ad01c4276a1',
    'ZSkillUpgradeConfig':                   '62a382ef75172ce4b8729e9e67b9363a',
    'ZSkillConfig':                          '655acc4e29cfe5b46a86227af5f4eb31',
    'ZSkillBehaviorConfig_CreateProjectile': '7e109b527efb2ec4faf6640d07341e96',
    'ZSkillBehaviorConfig_Bomb':             '87b74dc508a319a47866cda538695f55',
    'ZSkillBehaviorConfig_Lighting':         '38c11c6d422f72040928f00d2f62d0d0',
    'ZSkillBehaviorConfig_Stat':             '451289f87e83b2c44a79c95dd055c4ed',
}

# Existing asset GUIDs (looked up)
ASSET_GUIDS = {
    'StatDef_ATK':           'd23f4e56a2b9f4891b81b18fe885bd9b',
    'StatDef_HPMax':         'f4742bfd14a464611bef2137b2db1e63',
    'StatDef_Cooldown':      '4dd538c3a70d24748837e476a326131d',
    'StatDef_FireSpeed':     'afaff64959479453abb513b76ead9cc0',
    'StatDef_RangeFind':     '202dc84ee937243c6a9b56374e56765d',
    'StatDef_Speed':         '1546ad6be185440d1aa7b52b80a091ed',
    'StatDef_TiLeChiMang':   '86b7ee7c3972e4369ae3d44050b06329',
    'StatDef_SatThuongChiMang':'3d78d2f66d7d349a8963c87ea8230aff',
    'StatDef_Armor':         '33ab417a8a2f141f2b5643f267df250d',
    'StatDef_Heal':          'ee22559ac3e17478a90b836734970b7b',
    'StatDef_Luck':          'a0c43d573debe4b3fbca54c7150af2a8',
    'StatDef_XPMultiplier':  '493f66faed04c475986cb2cdbe31c21d',
    # PNG sprite source GUIDs (textures - to be used as Material main texture)
    'spr_Aiguille':       'f50673b214fb52044a54ee81afd79131',
    'spr_huixuanbiao':    'b306cab070c333c47be1d39deab2aef6',
    'spr_banzhaun':       '0166118281bb4ee49b3539595ccb5f3b',
    'spr_Liulian':        '2de6daf9f660b4042885e5555971a673',
    'spr_ranshaodan':     '4a6ab8fb58e321e49a50865dcd6f309a',
    'spr_FireBall':       'd218b9a20a2cbd2428b2c76ed4367673',
    # Existing skill configs
    'ZSk_Kunai':              'e1963f26b5a8a4189b9eee1f747512c2',
    'ZSk_Kunai_Behavior':     '102d01f4629134407bfac8fd0aa7da86',
}

CREATED_GUIDS = {}  # name -> guid map for new assets created here

def gen_guid():
    return uuid.uuid4().hex

def write_asset(folder, fname, yaml_body, register_name=None, sub_guid=None):
    os.makedirs(folder, exist_ok=True)
    path = os.path.join(folder, fname).replace(os.sep, '/')
    meta_path = path + '.meta'
    guid = sub_guid or gen_guid()
    with open(path, 'w', encoding='utf-8', newline='\n') as f:
        f.write(yaml_body)
    with open(meta_path, 'w', encoding='utf-8', newline='\n') as f:
        f.write(f'fileFormatVersion: 2\nguid: {guid}\nNativeFormatImporter:\n  externalObjects: {{}}\n  mainObjectFileID: 11400000\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n')
    if register_name:
        CREATED_GUIDS[register_name] = guid
    return guid

def so_header(script_guid, name, class_id):
    return (f'%YAML 1.1\n%TAG !u! tag:unity3d.com,2011:\n--- !u!114 &11400000\n'
            f'MonoBehaviour:\n  m_ObjectHideFlags: 0\n  m_CorrespondingSourceObject: {{fileID: 0}}\n'
            f'  m_PrefabInstance: {{fileID: 0}}\n  m_PrefabAsset: {{fileID: 0}}\n  m_GameObject: {{fileID: 0}}\n'
            f'  m_Enabled: 1\n  m_EditorHideFlags: 0\n  m_Script: {{fileID: 11500000, guid: {script_guid}, type: 3}}\n'
            f'  m_Name: {name}\n  m_EditorClassIdentifier: {class_id}\n')

# ======================================================================
# SLICE 1: Create 6 missing AssetStatDefinitions
# ======================================================================
def slice_1_stat_defs():
    print('=== SLICE 1: Missing StatDefinitions ===')
    folder = 'Assets/_Main/Data/StatDefinitions'
    sd_script = SCRIPT_GUIDS['AssetStatDefinition']
    # (name, statType enum value, displayName)
    defs = [
        ('StatDef_RadiusCollider', 102, 'Collider Radius'),
        ('StatDef_AmountProjectile', 103, 'Amount Projectile'),
        ('StatDef_TimeBreak', 104, 'Time Break'),
        ('StatDef_RadiusExplosion', 105, 'Explosion Radius'),
        ('StatDef_ATKMultiplierExplosion', 106, 'Explosion ATK Multiplier'),
        ('StatDef_TimeDelayExplosion', 107, 'Explosion Delay'),
    ]
    created = []
    for name, stat_type, display in defs:
        target = f'{folder}/{name}.asset'
        if os.path.exists(target):
            # already exists, reuse guid
            existing_guid = INDEX.get(name, {}).get('guid')
            if existing_guid:
                ASSET_GUIDS[name] = existing_guid
                print(f'  reuse {name} (guid={existing_guid})')
                continue
        body = so_header(sd_script, name, 'Assembly-CSharp::Luzart.AssetStatDefinition')
        body += f'  _id: {name}\n  statType: {stat_type}\n  displayName: {display}\n  statIcon: {{fileID: 0}}\n'
        g = write_asset(folder, name + '.asset', body, register_name=name)
        ASSET_GUIDS[name] = g
        created.append(name)
        print(f'  + {name} (guid={g}, statType={stat_type})')
    return created

# ======================================================================
# SLICE 2: 10 Projectile Configs
# ======================================================================
def slice_2_projectiles():
    print('\n=== SLICE 2: Projectile Configs ===')
    folder = 'Assets/_Main/Data/Projectiles'

    # (name, archetype script key, sprite guid key, scale, lifetime, stat_defs)
    PROJ = [
        ('ProjCfg_Kunai',      'NormalProjectileConfig',    'spr_Aiguille',    1.0, 5.0,
            ['StatDef_ATK','StatDef_FireSpeed','StatDef_RadiusCollider']),
        ('ProjCfg_Boomerang',  'BoomerangProjectileConfig', 'spr_huixuanbiao', 1.0, 4.0,
            ['StatDef_ATK','StatDef_FireSpeed','StatDef_RadiusCollider']),
        ('ProjCfg_Brick',      'BombProjectileConfig',      'spr_banzhaun',    1.0, 4.0,
            ['StatDef_ATK','StatDef_FireSpeed','StatDef_RadiusCollider','StatDef_RadiusExplosion','StatDef_TimeDelayExplosion']),
        ('ProjCfg_DrillShot',  'LaserProjectileConfig',     'spr_Aiguille',    1.0, 3.0,
            ['StatDef_ATK','StatDef_FireSpeed','StatDef_RadiusCollider']),
        ('ProjCfg_Durian',     'NormalProjectileConfig',    'spr_Liulian',     1.2, 6.0,
            ['StatDef_ATK','StatDef_FireSpeed','StatDef_RadiusCollider']),
        ('ProjCfg_Forcefield', 'NormalProjectileConfig',    'spr_FireBall',    1.5, 2.0,
            ['StatDef_ATK','StatDef_FireSpeed','StatDef_RadiusCollider','StatDef_RangeFind']),
        ('ProjCfg_Guardian',   'NormalProjectileConfig',    'spr_FireBall',    0.6, 10.0,
            ['StatDef_ATK','StatDef_FireSpeed','StatDef_RadiusCollider']),
        ('ProjCfg_Molotov',    'BombProjectileConfig',      'spr_ranshaodan',  1.0, 5.0,
            ['StatDef_ATK','StatDef_FireSpeed','StatDef_RadiusCollider','StatDef_RadiusExplosion','StatDef_TimeDelayExplosion']),
        ('ProjCfg_RPG',        'BombProjectileConfig',      'spr_FireBall',    1.0, 4.0,
            ['StatDef_ATK','StatDef_FireSpeed','StatDef_RadiusCollider','StatDef_RadiusExplosion','StatDef_TimeDelayExplosion']),
        ('ProjCfg_SoccerBall', 'BoomerangProjectileConfig', 'spr_FireBall',    1.0, 5.0,
            ['StatDef_ATK','StatDef_FireSpeed','StatDef_RadiusCollider']),
    ]

    for name, script_key, sprite_key, scale, lifetime, stat_def_names in PROJ:
        target = f'{folder}/{name}.asset'
        if os.path.exists(target):
            existing_guid = INDEX.get(name, {}).get('guid')
            if existing_guid:
                ASSET_GUIDS[name] = existing_guid
                print(f'  skip {name} (exists)')
                continue
        script_guid = SCRIPT_GUIDS[script_key]
        sprite_guid = ASSET_GUIDS[sprite_key]
        body = so_header(script_guid, name, f'Assembly-CSharp::Luzart.{script_key}')
        body += f'  _id: {name}\n'
        body += f'  _sprite: {{fileID: 21300000, guid: {sprite_guid}, type: 3}}\n'
        body += '  _material: {fileID: 0}\n'
        body += f'  _scale: {{x: {scale}, y: {scale}, z: {scale}}}\n'
        body += f'  _lifeTime: {lifetime}\n'
        # ProjectileConfig adds animationConfig + statDefinitions
        body += '  animationConfig: {fileID: 0}\n'
        body += '  statDefinitions:\n'
        for sd in stat_def_names:
            sd_guid = ASSET_GUIDS.get(sd)
            if not sd_guid:
                print(f'  !! missing stat def {sd} for {name}')
                continue
            body += f'  - {{fileID: 11400000, guid: {sd_guid}, type: 2}}\n'
        # BombProjectileConfig also has _gravity, _maxDistance, _explosionEffect
        # BoomerangProjectileConfig adds _acceration, _accerationReturn, _rotationSpeed, _damageCooldown
        # We'll add minimal sensible defaults
        if script_key == 'BombProjectileConfig':
            body += '  _gravity: 9.8\n'
            body += '  _maxDistance: 4\n'
            body += '  _explosionEffect: {fileID: 0}\n'
        if script_key == 'BoomerangProjectileConfig':
            body += '  _acceration: -8\n'
            body += '  _accerationReturn: 12\n'
            body += '  _rotationSpeed: 720\n'
            body += '  _damageCooldown: 0.5\n'
        if script_key == 'LightingProjectileConfig':
            body += '  _lightningRadiusFind: 3\n'
            body += '  _projectileConfigOther: {fileID: 0}\n'
            body += '  _amount: 3\n'
            body += '  _speedProjectileOther: 12\n'
        g = write_asset(folder, name + '.asset', body, register_name=name)
        ASSET_GUIDS[name] = g
        print(f'  + {name} ({script_key}, sprite={sprite_key}, guid={g})')

# ======================================================================
# SLICE 3: 50 ZSkillUpgradeConfig SOs (10 active skills x 5 levels)
# ======================================================================
def slice_3_upgrade_configs():
    print('\n=== SLICE 3: ZSkillUpgradeConfig (active skill levels) ===')
    folder = 'Assets/_Main/Data/Skills/Upgrades'

    # GDD-derived data per skill: list of 5 dicts {ATK, Cooldown (per skill), FireSpeed, Scale, RangeFind, AmountProjectile, descr}
    # Cooldown estimates: Kunai=1.0, Boomerang=1.5, Brick=1.5, DrillShot=2.0, Durian=3.0,
    #                    Forcefield=0.5, Guardian=0.5, Molotov=2.5, RPG=2.0, SoccerBall=1.8
    # ATK scaling: from GDD ATK Multiplier * 10 (so 1.5→15, 6→60 etc.)
    SKILLS = {
        'ZSkUp_Kunai':       {'mults':[1.5, 2.0, 3.0, 4.0, 5.5], 'cooldown':1.0, 'fireSpeed':[10,10,10,10,10],
                              'scale':[1.0]*5, 'amountProj':[1,2,3,4,5], 'desc':['+1 Kunai','+1 Kunai','+1 Kunai','+1 Kunai','+1 Kunai']},
        'ZSkUp_Boomerang':   {'mults':[2.4,2.4,4.8,4.8,6.0], 'cooldown':1.5, 'fireSpeed':[8]*5,
                              'scale':[1.0,1.0,1.0,1.2,1.5], 'amountProj':[1,2,2,2,2], 'desc':['1 boomerang','+1','dmg x2','+size','+size']},
        'ZSkUp_Brick':       {'mults':[1.0,1.0,1.0,1.5,1.5], 'cooldown':1.5, 'fireSpeed':[6]*5,
                              'scale':[1.0]*5, 'amountProj':[1,2,3,4,5], 'desc':['1 brick','+1 brick','+1 brick','+1 brick','+1 brick']},
        'ZSkUp_DrillShot':   {'mults':[1.5,2.0,3.0,4.0,5.5], 'cooldown':2.0, 'fireSpeed':[10,10,20,30,30],
                              'scale':[1.0]*5, 'amountProj':[1,2,2,2,3], 'desc':['1 drill','+1 drill','speed x2','speed+dmg','+1 drill']},
        'ZSkUp_Durian':      {'mults':[6.0,6.0,8.0,10.0,10.0], 'cooldown':3.0, 'fireSpeed':[5]*5,
                              'scale':[1.0,1.0,2.0,2.0,2.0], 'amountProj':[1]*5, 'desc':['1 durian','dmg x2','size x2','+dmg','dmg x2']},
        'ZSkUp_Forcefield':  {'mults':[1.0,1.5,2.0,2.25,2.5], 'cooldown':0.5, 'fireSpeed':[0]*5,
                              'scale':[1.0,1.15,1.3,1.45,1.6], 'amountProj':[1]*5, 'desc':['field','+size +dmg','+size +dmg','+size +dmg','+size +dmg']},
        'ZSkUp_Guardian':    {'mults':[0.5,0.6,0.7,0.9,0.9], 'cooldown':0.5, 'fireSpeed':[10,11.5,13,14.5,16],
                              'scale':[1.0]*5, 'amountProj':[2,3,4,5,6], 'desc':['2 tops','+1 top','+1 top','+1 top','+1 top']},
        'ZSkUp_Molotov':     {'mults':[0.6,0.8,0.9,1.1,1.2], 'cooldown':2.5, 'fireSpeed':[5]*5,
                              'scale':[1.0]*5, 'amountProj':[2,3,4,5,6], 'desc':['2 molotov','+1 molotov','+1','+1','+1']},
        'ZSkUp_RPG':         {'mults':[2.0,4.0,4.0,6.0,6.0], 'cooldown':2.0, 'fireSpeed':[8]*5,
                              'scale':[1.0]*5, 'amountProj':[1,1,2,2,3], 'desc':['rocket','dmg x2','+1 rocket','+dmg','+1 rocket']},
        'ZSkUp_SoccerBall':  {'mults':[1.0,1.0,4.0,5.0,5.0], 'cooldown':1.8, 'fireSpeed':[6]*5,
                              'scale':[1.0]*5, 'amountProj':[1,2,2,2,3], 'desc':['1 ball','+1 ball','speed+dmg','speed+dmg','+1 ball']},
    }

    upgrade_script = SCRIPT_GUIDS['ZSkillUpgradeConfig']

    for skill_name, data in SKILLS.items():
        for lvl in range(5):
            star = '★' * (lvl + 1)
            asset_name = f'{skill_name}_Lv{lvl+1}'
            target = f'{folder}/{asset_name}.asset'
            if os.path.exists(target):
                existing_guid = INDEX.get(asset_name, {}).get('guid')
                if existing_guid:
                    ASSET_GUIDS[asset_name] = existing_guid
                    continue
            body = so_header(upgrade_script, asset_name, 'Assembly-CSharp::Luzart.ZSkillUpgradeConfig')
            body += f'  _id: {asset_name}\n'
            body += '  skillInfor:\n'
            body += f'    sprite: {{fileID: 0}}\n'
            display = skill_name.replace('ZSkUp_', '').replace('_', ' ')
            body += f'    skillName: {display} {star}\n'
            body += f'    skillDetail: {data["desc"][lvl]}\n'
            # stats list
            body += '  stats:\n'
            def add_stat(stat_def_name, value):
                sd_guid = ASSET_GUIDS.get(stat_def_name)
                if not sd_guid: return ''
                return (f'  - definition: {{fileID: 11400000, guid: {sd_guid}, type: 2}}\n'
                        f'    value:\n      mode: 0\n      constant: {value}\n      asset: {{fileID: 0}}\n')
            stats_yaml = ''
            stats_yaml += add_stat('StatDef_ATK',           data['mults'][lvl] * 10.0)  # multiplier x10 → raw dmg
            stats_yaml += add_stat('StatDef_Cooldown',      data['cooldown'])
            stats_yaml += add_stat('StatDef_FireSpeed',     data['fireSpeed'][lvl])
            stats_yaml += add_stat('StatDef_AmountProjectile', data['amountProj'][lvl])
            stats_yaml += add_stat('StatDef_RangeFind',     8.0)  # all skills 8 unit search range
            stats_yaml += add_stat('StatDef_RadiusCollider', 0.3 * data['scale'][lvl])
            body += stats_yaml if stats_yaml else '  []\n'
            body += '  modifierFactors: []\n'
            g = write_asset(folder, asset_name + '.asset', body, register_name=asset_name)
            ASSET_GUIDS[asset_name] = g
        print(f'  + {skill_name} Lv1..Lv5 generated')

# ======================================================================
# SLICE 4: Wire ZSk_*.asset (upgradeConfigs) + ZSk_*_Behavior.asset (projectileConfig)
# ======================================================================
def slice_4_wire_skills():
    print('\n=== SLICE 4: Wire skill configs to upgrade configs + projectile configs ===')
    # Mapping: skill_config -> behavior config -> projectile config
    WIRING = [
        ('ZSk_Kunai',      'ZSk_Kunai_Behavior',      'ProjCfg_Kunai',      'ZSkUp_Kunai'),
        ('ZSk_Boomerang',  'ZSk_Boomerang_Behavior',  'ProjCfg_Boomerang',  'ZSkUp_Boomerang'),
        ('ZSk_Brick',      'ZSk_Brick_Behavior',      'ProjCfg_Brick',      'ZSkUp_Brick'),
        ('ZSk_DrillShot',  'ZSk_DrillShot_Behavior',  'ProjCfg_DrillShot',  'ZSkUp_DrillShot'),
        ('ZSk_Durian',     'ZSk_Durian_Behavior',     'ProjCfg_Durian',     'ZSkUp_Durian'),
        ('ZSk_Forcefield', 'ZSk_Forcefield_Behavior', 'ProjCfg_Forcefield', 'ZSkUp_Forcefield'),
        ('ZSk_Guardian',   'ZSk_Guardian_Behavior',   'ProjCfg_Guardian',   'ZSkUp_Guardian'),
        ('ZSk_Molotov',    'ZSk_Molotov_Behavior',    'ProjCfg_Molotov',    'ZSkUp_Molotov'),
        ('ZSk_RPG',        'ZSk_RPG_Behavior',        'ProjCfg_RPG',        'ZSkUp_RPG'),
        ('ZSk_SoccerBall', 'ZSk_SoccerBall_Behavior', 'ProjCfg_SoccerBall', 'ZSkUp_SoccerBall'),
    ]

    for sk_name, beh_name, proj_name, up_prefix in WIRING:
        sk_path = INDEX.get(sk_name, {}).get('path') or f'Assets/_Main/Data/Skills/Configs/{sk_name}.asset'
        beh_path = INDEX.get(beh_name, {}).get('path') or f'Assets/_Main/Data/Skills/Behaviors/{beh_name}.asset'
        proj_guid = ASSET_GUIDS.get(proj_name)
        if not proj_guid:
            print(f'  !! no projectile guid for {proj_name}, skipping {sk_name}')
            continue
        # --- update behavior: set projectileConfig ---
        if os.path.exists(beh_path):
            with open(beh_path, 'r', encoding='utf-8') as f: yaml = f.read()
            new_yaml = re.sub(r'projectileConfig:\s*\{fileID:\s*0\}',
                              f'projectileConfig: {{fileID: 11400000, guid: {proj_guid}, type: 2}}',
                              yaml)
            if new_yaml != yaml:
                with open(beh_path, 'w', encoding='utf-8', newline='\n') as f: f.write(new_yaml)
                print(f'  wired {beh_name}.projectileConfig -> {proj_name}')
            else:
                print(f'  (no-change) {beh_name} -- projectileConfig probably already set')
        else:
            print(f'  !! behavior file missing: {beh_path}')
        # --- update skill: upgradeConfigs ---
        if os.path.exists(sk_path):
            with open(sk_path, 'r', encoding='utf-8') as f: yaml = f.read()
            up_refs = []
            for lvl in range(1, 6):
                up_name = f'{up_prefix}_Lv{lvl}'
                up_guid = ASSET_GUIDS.get(up_name)
                if up_guid:
                    up_refs.append(f'  - {{fileID: 11400000, guid: {up_guid}, type: 2}}')
            up_block = '\n'.join(up_refs)
            new_yaml = re.sub(r'upgradeConfigs:\s*\[\]',
                              f'upgradeConfigs:\n{up_block}',
                              yaml)
            if new_yaml != yaml:
                with open(sk_path, 'w', encoding='utf-8', newline='\n') as f: f.write(new_yaml)
                print(f'  wired {sk_name}.upgradeConfigs -> 5 levels')
            else:
                print(f'  (no-change) {sk_name} -- upgradeConfigs probably already set')
        else:
            print(f'  !! skill file missing: {sk_path}')

# ======================================================================
# SLICE 5: Player StatsConfig + AssetStat SOs
# ======================================================================
def slice_5_player_stats():
    print('\n=== SLICE 5: Player StatsConfig ===')
    folder_stats = 'Assets/_Main/Data/Stats/Player'
    folder_cfg = 'Assets/_Main/Data/Stats/Configs'
    as_script = SCRIPT_GUIDS['AssetStat']
    sc_script = SCRIPT_GUIDS['StatsConfig']
    # (name, statDefName, value)
    PLAYER_STATS = [
        ('Stat_Player_HPMax',     'StatDef_HPMax', 1000.0),
        ('Stat_Player_ATK',       'StatDef_ATK', 10.0),
        ('Stat_Player_Speed',     'StatDef_Speed', 4.0),
        ('Stat_Player_Heal',      'StatDef_Heal', 0.0),
        ('Stat_Player_Armor',     'StatDef_Armor', 0.0),
        ('Stat_Player_Luck',      'StatDef_Luck', 1.0),
        ('Stat_Player_RangeFind', 'StatDef_RangeFind', 8.0),
    ]
    for name, sd_name, val in PLAYER_STATS:
        target = f'{folder_stats}/{name}.asset'
        if os.path.exists(target):
            existing_guid = INDEX.get(name, {}).get('guid')
            if existing_guid:
                ASSET_GUIDS[name] = existing_guid; continue
        sd_guid = ASSET_GUIDS[sd_name]
        body = so_header(as_script, name, 'Assembly-CSharp::Luzart.AssetStat')
        body += f'  _id: {name}\n'
        body += f'  definition: {{fileID: 11400000, guid: {sd_guid}, type: 2}}\n'
        body += f'  value:\n    mode: 0\n    constant: {val}\n    asset: {{fileID: 0}}\n'
        g = write_asset(folder_stats, name + '.asset', body, register_name=name)
        ASSET_GUIDS[name] = g
        print(f'  + {name} = {val}')

    # The container StatsConfig
    cfg_name = 'StatsCfg_Player'
    cfg_path = f'{folder_cfg}/{cfg_name}.asset'
    if os.path.exists(cfg_path):
        existing_guid = INDEX.get(cfg_name, {}).get('guid')
        if existing_guid: ASSET_GUIDS[cfg_name] = existing_guid
        print(f'  (skip) {cfg_name} exists')
        return
    body = so_header(sc_script, cfg_name, 'Assembly-CSharp::Luzart.StatsConfig')
    body += f'  _id: {cfg_name}\n  assetStats:\n'
    for name, _, _ in PLAYER_STATS:
        body += f'  - {{fileID: 11400000, guid: {ASSET_GUIDS[name]}, type: 2}}\n'
    g = write_asset(folder_cfg, cfg_name + '.asset', body, register_name=cfg_name)
    ASSET_GUIDS[cfg_name] = g
    print(f'  + {cfg_name} (assetStats={len(PLAYER_STATS)})')

# ============================================================
# MAIN
# ============================================================
if __name__ == '__main__':
    created = slice_1_stat_defs()
    slice_2_projectiles()
    slice_3_upgrade_configs()
    slice_4_wire_skills()
    slice_5_player_stats()
    # dump map for inspection
    with open('docs/created-guids.json', 'w', encoding='utf-8') as f:
        json.dump(CREATED_GUIDS, f, ensure_ascii=False, indent=1)
    print(f'\nDone. {len(CREATED_GUIDS)} new assets created. Map -> docs/created-guids.json')
