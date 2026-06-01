"""Fix unwanted physics push between Enemy / Player / Projectile.

Issues observed:
  1. Enemy pushes Player — Zombie has 1 non-trigger BoxCollider2D for the body.
     Wapeon (Player) is all-trigger but Zombie body still resolves physics on other
     Dynamic rigidbodies in the scene.
  2. Player pushes bullet / Enemy pushes bullet — bullets have Dynamic Rigidbody2D,
     so any 1-frame collider contact can apply unwanted velocity.

Fixes:
  A. Zombie.prefab: change every Collider2D's m_IsTrigger to 1 — enemies pass through
     each other (Survivor.io swarm style) and through player. Damage still fires via
     OnTriggerEnter2D.
  B. All Pf_*.prefab bullets: change Rigidbody2D m_BodyType to 2 (Kinematic) so even
     if physics tries to push them, the rigidbody is immovable by external forces.

Idempotent — re-run is safe.
"""
import re, pathlib

ROOT = pathlib.Path(r"D:\Unity Training\survivorIOSource\DATN-GoGoSurvival")

ENEMY_PREFABS = [
    ROOT / "Assets/_Main/Perfabes/Normal/Zombie.prefab",
]
BULLET_PREFABS = list((ROOT / "Assets/_Main/Perfabes/Projectiles").glob("Pf_*.prefab"))


def fix_triggers(path: pathlib.Path) -> int:
    """Set every `m_IsTrigger: 0` to `m_IsTrigger: 1` in collider blocks."""
    text = path.read_text(encoding="utf-8")
    new_text, n = re.subn(r"^(\s+)m_IsTrigger: 0$", r"\g<1>m_IsTrigger: 1", text, flags=re.MULTILINE)
    if n > 0:
        path.write_text(new_text, encoding="utf-8", newline="\n")
    return n


def fix_kinematic(path: pathlib.Path) -> int:
    """Inside Rigidbody2D blocks, change m_BodyType: 0 (Dynamic) to 2 (Kinematic).

    BodyType lives inside Rigidbody2D blocks specifically. Match the block then
    edit m_BodyType line.
    """
    text = path.read_text(encoding="utf-8")
    # Match Rigidbody2D block: starts with "Rigidbody2D:" until next "--- !u!"
    pattern = re.compile(r"(^Rigidbody2D:.*?)(?=^---\s*!u!|\Z)", re.MULTILINE | re.DOTALL)
    total = 0

    def repl(match):
        nonlocal total
        block = match.group(1)
        new_block, n = re.subn(r"^(\s+)m_BodyType:\s*0\s*$", r"\g<1>m_BodyType: 2", block, flags=re.MULTILINE)
        total += n
        return new_block

    new_text = pattern.sub(repl, text)
    if total > 0:
        path.write_text(new_text, encoding="utf-8", newline="\n")
    return total


print("=== A. Enemy -> all triggers ===")
for p in ENEMY_PREFABS:
    if not p.exists():
        print(f"  MISSING {p.relative_to(ROOT)}")
        continue
    n = fix_triggers(p)
    print(f"  {p.relative_to(ROOT)}: {n} non-trigger -> trigger")

print("\n=== B. Bullets -> Kinematic Rigidbody2D ===")
for p in BULLET_PREFABS:
    n = fix_kinematic(p)
    print(f"  {p.relative_to(ROOT)}: {n} Dynamic -> Kinematic")
