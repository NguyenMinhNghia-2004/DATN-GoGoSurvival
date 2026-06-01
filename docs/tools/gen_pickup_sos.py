"""Generate PickupEffect + DropConfig SO assets for the data-driven pickup system.

Creates:
  Assets/_Main/Data/Pickups/Effects/PickupFX_GrantXP_1.asset
  Assets/_Main/Data/Pickups/Effects/PickupFX_GrantXP_2.asset
  Assets/_Main/Data/Pickups/Effects/PickupFX_GrantCoin_1.asset
  Assets/_Main/Data/Pickups/Configs/Drop_Pickup_XP.asset
  Assets/_Main/Data/Pickups/Configs/Drop_Pickup_XPDouble.asset
  Assets/_Main/Data/Pickups/Configs/Drop_Pickup_Coin.asset

Uses stable hardcoded GUIDs (in the d* family namespace) so subsequent batch-edit
of the 3200 prefab MonoBehaviour blocks can reference them by literal string.
"""
import os, pathlib

ROOT = pathlib.Path(r"D:\Unity Training\survivorIOSource\DATN-GoGoSurvival")
EFX_DIR = ROOT / "Assets/_Main/Data/Pickups/Effects"
CFG_DIR = ROOT / "Assets/_Main/Data/Pickups/Configs"
EFX_DIR.mkdir(parents=True, exist_ok=True)
CFG_DIR.mkdir(parents=True, exist_ok=True)

# Script GUIDs (from .cs.meta)
SCRIPT_GUID_DROPCONFIG    = "90b41465c702f3445a44c2012935bb88"
SCRIPT_GUID_FX_GRANTXP    = "a3a2b8a9de5cbee438fdb5b522d29c72"
SCRIPT_GUID_FX_GRANTCOIN  = "36a7d07879c5f5049881e310bee367d2"

# Stable asset GUIDs we'll create (32 hex chars). Prefix d10p1ckup to make them
# discoverable when grep-ing the project later.
GUID_FX_GRANTXP_1   = "aaaaaaaa000000000000000000000001"
GUID_FX_GRANTXP_2   = "aaaaaaaa000000000000000000000002"
GUID_FX_GRANTCOIN_1 = "aaaaaaaa000000000000000000000003"
GUID_DROP_XP        = "bbbbbbbb000000000000000000000001"
GUID_DROP_XPDOUBLE  = "bbbbbbbb000000000000000000000002"
GUID_DROP_COIN      = "bbbbbbbb000000000000000000000003"

def meta(guid):
    return f"""fileFormatVersion: 2
guid: {guid}
NativeFormatImporter:
  externalObjects: {{}}
  mainObjectFileID: 11400000
  userData:
  assetBundleName:
  assetBundleVariant:
"""

def fx_xp(name, amount, guid):
    return f"""%YAML 1.1
%TAG !u! tag:unity3d.com,2011:
--- !u!114 &11400000
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: 0}}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {{fileID: 11500000, guid: {SCRIPT_GUID_FX_GRANTXP}, type: 3}}
  m_Name: {name}
  m_EditorClassIdentifier:
  _amount: {amount}
"""

def fx_coin(name, amount):
    return f"""%YAML 1.1
%TAG !u! tag:unity3d.com,2011:
--- !u!114 &11400000
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: 0}}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {{fileID: 11500000, guid: {SCRIPT_GUID_FX_GRANTCOIN}, type: 3}}
  m_Name: {name}
  m_EditorClassIdentifier:
  _amount: {amount}
"""

def dropcfg(name, display, effect_guid):
    return f"""%YAML 1.1
%TAG !u! tag:unity3d.com,2011:
--- !u!114 &11400000
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: 0}}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {{fileID: 11500000, guid: {SCRIPT_GUID_DROPCONFIG}, type: 3}}
  m_Name: {name}
  m_EditorClassIdentifier:
  _id: {name}
  _sprite: {{fileID: 0}}
  _material: {{fileID: 0}}
  _scale: {{x: 1, y: 1, z: 1}}
  _visualPrefab: {{fileID: 0}}
  _lifeTime: 10000
  prefab: {{fileID: 0}}
  displayName: {display}
  description: ""
  flyDuration: 0.45
  effects:
  - {{fileID: 11400000, guid: {effect_guid}, type: 2}}
  colliderRadius: 0.3
  pickupRange: 1
  animationConfig: {{fileID: 0}}
  dropValues: []
"""

def write(path, content):
    p = pathlib.Path(path)
    if not p.exists():
        p.write_text(content, encoding="utf-8", newline="\n")
        print("CREATED", p.relative_to(ROOT))
    else:
        print("SKIP   ", p.relative_to(ROOT))

# --- Effects ---
write(EFX_DIR / "PickupFX_GrantXP_1.asset",   fx_xp("PickupFX_GrantXP_1", 1, GUID_FX_GRANTXP_1))
write(EFX_DIR / "PickupFX_GrantXP_1.asset.meta", meta(GUID_FX_GRANTXP_1))
write(EFX_DIR / "PickupFX_GrantXP_2.asset",   fx_xp("PickupFX_GrantXP_2", 2, GUID_FX_GRANTXP_2))
write(EFX_DIR / "PickupFX_GrantXP_2.asset.meta", meta(GUID_FX_GRANTXP_2))
write(EFX_DIR / "PickupFX_GrantCoin_1.asset", fx_coin("PickupFX_GrantCoin_1", 1))
write(EFX_DIR / "PickupFX_GrantCoin_1.asset.meta", meta(GUID_FX_GRANTCOIN_1))

# --- DropConfigs ---
write(CFG_DIR / "Drop_Pickup_XP.asset",       dropcfg("Drop_Pickup_XP",       "XP Diamond",        GUID_FX_GRANTXP_1))
write(CFG_DIR / "Drop_Pickup_XP.asset.meta",  meta(GUID_DROP_XP))
write(CFG_DIR / "Drop_Pickup_XPDouble.asset", dropcfg("Drop_Pickup_XPDouble", "XP Diamond (x2)",   GUID_FX_GRANTXP_2))
write(CFG_DIR / "Drop_Pickup_XPDouble.asset.meta", meta(GUID_DROP_XPDOUBLE))
write(CFG_DIR / "Drop_Pickup_Coin.asset",     dropcfg("Drop_Pickup_Coin",     "Coin",              GUID_FX_GRANTCOIN_1))
write(CFG_DIR / "Drop_Pickup_Coin.asset.meta",meta(GUID_DROP_COIN))

print("\nGUIDs for batch edit:")
print(f"  DROP_XP       = {GUID_DROP_XP}")
print(f"  DROP_XPDOUBLE = {GUID_DROP_XPDOUBLE}")
print(f"  DROP_COIN     = {GUID_DROP_COIN}")
