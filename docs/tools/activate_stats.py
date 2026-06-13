import json, sys, io, re
sys.stdout = io.TextIOWrapper(sys.stdout.buffer, encoding='utf-8')
INGAME = json.load(open('docs/created-guids-modifier-infra.json', encoding='utf-8'))
# Only stats that have BOTH a Stat_Player asset AND a Number_Player_InGame_Final.
STATS = ['HPMax','ATK','Speed','Heal','Armor','RangeFind']
for s in STATS:
    p = f'Assets/_Main/Data/Stats/Player/Stat_Player_{s}.asset'
    try:
        txt = open(p, encoding='utf-8').read()
    except FileNotFoundError:
        print(f'  skip (no asset): {s}'); continue
    final_g = INGAME[f'Number_Player_InGame_Final_{s}']
    new = re.sub(r'  value:\n    mode: \d+\n    constant: [^\n]+\n    asset: \{[^}]*\}',
                 f'  value:\n    mode: 1\n    constant: 0\n    asset: {{fileID: 11400000, guid: {final_g}, type: 2}}',
                 txt, count=1)
    if new != txt:
        open(p, 'w', encoding='utf-8', newline='\n').write(new); print(f'  activated {s}')
    else:
        print(f'  WARN no value block matched: {s}')
