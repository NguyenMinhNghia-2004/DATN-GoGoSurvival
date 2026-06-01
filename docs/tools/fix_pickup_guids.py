"""Replace non-hex placeholder GUIDs with valid hex GUIDs across all SOs + prefabs.

The old GUIDs (d10p1ckup...) contained non-hex chars (p,g,k,n) which Unity rejects.
This script does a 1-pass sed-replace across every .asset, .meta, .prefab in the
project root so all references stay consistent.
"""
import pathlib

ROOT = pathlib.Path(r"D:\Unity Training\survivorIOSource\DATN-GoGoSurvival")

REPLACE = {
    # OLD (non-hex)                          : NEW (valid hex)
    "aaaaaaaa000000000000000000000001": "aaaaaaaa000000000000000000000001",  # FX_GrantXP_1
    "aaaaaaaa000000000000000000000002": "aaaaaaaa000000000000000000000002",  # FX_GrantXP_2
    "aaaaaaaa000000000000000000000003": "aaaaaaaa000000000000000000000003",  # FX_GrantCoin_1
    "bbbbbbbb000000000000000000000001":  "bbbbbbbb000000000000000000000001",  # Drop_Pickup_XP
    "bbbbbbbb000000000000000000000002":  "bbbbbbbb000000000000000000000002",  # Drop_Pickup_XPDouble
    "bbbbbbbb000000000000000000000003":  "bbbbbbbb000000000000000000000003",  # Drop_Pickup_Coin
}

# Scan Assets/_Main + docs/tools (also patch the gen + assign scripts so reruns are consistent)
SCOPE_DIRS = ["Assets/_Main", "docs/tools"]
EXTS = (".asset", ".meta", ".prefab", ".py", ".cs")

total_files = 0
total_replacements = 0
for scope in SCOPE_DIRS:
    for path in (ROOT / scope).rglob("*"):
        if not path.is_file() or path.suffix not in EXTS:
            continue
        try:
            text = path.read_text(encoding="utf-8")
        except UnicodeDecodeError:
            continue
        original = text
        for old, new in REPLACE.items():
            text = text.replace(old, new)
        if text != original:
            path.write_text(text, encoding="utf-8", newline="\n")
            replacements = sum(1 for o in REPLACE if o in original)
            total_files += 1
            total_replacements += sum(original.count(o) for o in REPLACE)
            print(f"  patched {path.relative_to(ROOT)}")

print(f"\nFiles changed: {total_files}")
print(f"Total replacements: {total_replacements}")
