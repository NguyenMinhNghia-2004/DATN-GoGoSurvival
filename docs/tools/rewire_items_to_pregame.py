"""
1) Swap every item's modifier reference guid  Mod_Player_InGame_*  ->  Mod_Player_PreGame_*.
2) Repoint each Number_Player_InGame_Final_<Stat>.baseNumber asset guid
   from Number_Player_InGame_Base_<Stat>  ->  Number_Player_PreGame_Final_<Stat>.
Pure deterministic YAML guid replacement. Run from project root.
"""
import os, sys, io, json, glob, re
sys.stdout = io.TextIOWrapper(sys.stdout.buffer, encoding='utf-8')

INGAME = json.load(open('docs/created-guids-modifier-infra.json', encoding='utf-8'))
PRE    = json.load(open('docs/created-guids-pregame-infra.json', encoding='utf-8'))
STATS = ['HPMax','ATK','Speed','Cooldown','FireSpeed','Heal','Armor','RangeFind']
KINDS = ['AddNormal','AddRatio','Multiply']

# (1) build Mod guid swap map  InGame -> PreGame
mod_swap = {}
for s in STATS:
    for k in KINDS:
        ig = INGAME[f'Mod_Player_InGame_{s}_{k}']
        pg = PRE[f'Mod_Player_PreGame_{s}_{k}']
        mod_swap[ig] = pg

item_files = glob.glob('Assets/_Main/Data/Items/**/ItemConfig_*.asset', recursive=True)
changed_items = 0; total_subs = 0
for path in item_files:
    txt = open(path, encoding='utf-8').read()
    new = txt; subs = 0
    for ig, pg in mod_swap.items():
        if ig in new:
            cnt = new.count(ig)
            new = new.replace(ig, pg)
            subs += cnt
    if new != txt:
        open(path, 'w', encoding='utf-8', newline='\n').write(new)
        changed_items += 1; total_subs += subs
print(f'Items rewired: {changed_items}/{len(item_files)} files, {total_subs} guid subs')

# (2) bridge: InGame Final base  Base_<Stat> -> PreGame Final_<Stat>
bridged = 0
for s in STATS:
    final_path = f'Assets/_Main/Data/ModifierAndInGame/Player/{s}/Number_Player_InGame_Final_{s}.asset'
    base_ig = INGAME[f'Number_Player_InGame_Base_{s}']
    pre_final = PRE[f'Number_Player_PreGame_Final_{s}']
    txt = open(final_path, encoding='utf-8').read()
    # only the baseNumber block references base_ig; swap that single guid
    if base_ig in txt:
        new = txt.replace(base_ig, pre_final)
        open(final_path, 'w', encoding='utf-8', newline='\n').write(new)
        bridged += 1
    else:
        print(f'  WARN: {s} Final did not reference its InGame Base guid (already bridged?)')
print(f'InGame Final bases bridged to PreGame Final: {bridged}/8')
