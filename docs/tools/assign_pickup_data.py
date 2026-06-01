"""Batch-assign PickupBase._data on every Diamond/LevelDiamond/Coin MonoBehaviour
across Diamond*.prefab + Leve*.prefab.

For each MonoBehaviour block matching a target script GUID, inject `_data` (DropConfig
ref) + `_legacyFlyDuration` after `m_EditorClassIdentifier:` line. Idempotent — skips
blocks that already have `_data:`.

Mapping:
  Diamond     (34cd61...)   to Drop_Pickup_XP  (regular white)  — for Diamond.prefab default
                              Drop_Pickup_XPDouble for Green/Blue/Red/VIP (legacy "Green" flag)
  LevelDiamond (681ed9...)  to Drop_Pickup_XP  (the 2730 inline map gems)
  Coin        (ee0fceb...)  to Drop_Pickup_Coin (the 476 inline map coins)
"""
import re, pathlib

ROOT = pathlib.Path(r"D:\Unity Training\survivorIOSource\DATN-GoGoSurvival")

GUID_DIAMOND      = "34cd61dbc7186ac4583e66814713b55b"
GUID_LEVELDIAMOND = "681ed9a302e746e488b01c05eb9639c9"
GUID_COIN         = "ee0fceb031db4134fafaa69a2a97b60b"

GUID_DROP_XP       = "bbbbbbbb000000000000000000000001"
GUID_DROP_XPDOUBLE = "bbbbbbbb000000000000000000000002"
GUID_DROP_COIN     = "bbbbbbbb000000000000000000000003"

# (prefab path, script GUID, DropConfig GUID)
TARGETS = [
    # Top-level Diamond prefabs (1 instance each)
    ("Assets/_Main/Perfabes/Normal/Diamond.prefab",      GUID_DIAMOND, GUID_DROP_XP),
    ("Assets/_Main/Perfabes/Normal/DiamondGreen.prefab", GUID_DIAMOND, GUID_DROP_XPDOUBLE),
    ("Assets/_Main/Perfabes/Normal/DiamondBlue.prefab",  GUID_DIAMOND, GUID_DROP_XPDOUBLE),
    ("Assets/_Main/Perfabes/Normal/DiamondRed.prefab",   GUID_DIAMOND, GUID_DROP_XPDOUBLE),
    ("Assets/_Main/Perfabes/Normal/DiamondVIP.prefab",   GUID_DIAMOND, GUID_DROP_XPDOUBLE),
    # Level prefabs with inline gems + coins
    ("Assets/_Main/Perfabes/Levels/Level1.prefab", GUID_LEVELDIAMOND, GUID_DROP_XP),
    ("Assets/_Main/Perfabes/Levels/Level1.prefab", GUID_COIN,         GUID_DROP_COIN),
    ("Assets/_Main/Perfabes/Levels/Leve2.prefab",  GUID_LEVELDIAMOND, GUID_DROP_XP),
    ("Assets/_Main/Perfabes/Levels/Leve2.prefab",  GUID_COIN,         GUID_DROP_COIN),
    ("Assets/_Main/Perfabes/Levels/Leve3.prefab",  GUID_LEVELDIAMOND, GUID_DROP_XP),
    ("Assets/_Main/Perfabes/Levels/Leve3.prefab",  GUID_COIN,         GUID_DROP_COIN),
    ("Assets/_Main/Perfabes/Levels/Leve4.prefab",  GUID_LEVELDIAMOND, GUID_DROP_XP),
    ("Assets/_Main/Perfabes/Levels/Leve4.prefab",  GUID_COIN,         GUID_DROP_COIN),
    ("Assets/_Main/Perfabes/Levels/Leve5.prefab",  GUID_LEVELDIAMOND, GUID_DROP_XP),
    ("Assets/_Main/Perfabes/Levels/Leve5.prefab",  GUID_COIN,         GUID_DROP_COIN),
    ("Assets/_Main/Perfabes/Levels/Leve6.prefab",  GUID_LEVELDIAMOND, GUID_DROP_XP),
    ("Assets/_Main/Perfabes/Levels/Leve6.prefab",  GUID_COIN,         GUID_DROP_COIN),
]


def patch(prefab_text: str, script_guid: str, dropconfig_guid: str) -> tuple[str, int]:
    """For each MonoBehaviour block in `prefab_text` whose m_Script matches `script_guid`,
    insert `_data` + `_legacyFlyDuration` right after the `m_EditorClassIdentifier:` line.
    Skip blocks that already contain `_data:`. Returns (new_text, edits_made)."""
    # Pattern: m_Script line (with target GUID) followed by metadata lines, ending at next MonoBehaviour
    # We match the m_Script line itself, then track until we find m_EditorClassIdentifier,
    # then check if _data exists in the block before next `--- !u!` marker.
    out = []
    lines = prefab_text.split("\n")
    edits = 0
    i = 0
    n = len(lines)
    target_script = f"guid: {script_guid}"
    while i < n:
        line = lines[i]
        out.append(line)
        # Detect m_Script line for our target script
        if "m_Script:" in line and target_script in line:
            # Find end of this MonoBehaviour block (next "--- !u!" or EOF)
            block_end = n
            for j in range(i + 1, n):
                if lines[j].startswith("--- !u!"):
                    block_end = j
                    break
            # Check if _data: already present in this block
            block_lines = lines[i:block_end]
            has_data = any(re.match(r"\s+_data:\s", bl) for bl in block_lines)
            if not has_data:
                # Find m_EditorClassIdentifier line in block, emit subsequent lines but inject after it
                # Continue copying until we hit m_EditorClassIdentifier, then inject
                injected = False
                k = i + 1
                while k < block_end:
                    out.append(lines[k])
                    if not injected and lines[k].lstrip().startswith("m_EditorClassIdentifier:"):
                        out.append(f"  _data: {{fileID: 11400000, guid: {dropconfig_guid}, type: 2}}")
                        out.append("  _legacyFlyDuration: 0.45")
                        injected = True
                        edits += 1
                    k += 1
                i = block_end
                continue
            else:
                # Already patched — copy block as-is
                for k in range(i + 1, block_end):
                    out.append(lines[k])
                i = block_end
                continue
        i += 1
    return "\n".join(out), edits


total = 0
for rel, script_guid, drop_guid in TARGETS:
    path = ROOT / rel
    if not path.exists():
        print(f"MISSING {rel}")
        continue
    text = path.read_text(encoding="utf-8")
    new_text, n = patch(text, script_guid, drop_guid)
    if n > 0:
        path.write_text(new_text, encoding="utf-8", newline="\n")
        print(f"  patched {n:4d} blocks  in {rel}  (script={script_guid[:8]} to drop={drop_guid[:8]})")
        total += n
    else:
        print(f"  no-op            in {rel}  (script={script_guid[:8]})")
print(f"\nTotal edits: {total}")
