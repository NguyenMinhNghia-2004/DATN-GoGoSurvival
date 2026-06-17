using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Luzart
{
    /// <summary>
    /// Throws a bomb-arc projectile that, instead of exploding on land, spawns a FirePatch
    /// MonoBehaviour at the land point. The patch tickdamages for its lifetime, then dies.
    /// Used by Molotov.
    /// </summary>
    public class ZSkillBehavior_GroundPatch : ZSkillBehavior<ZSkillBehaviorConfig_GroundPatch>
    {
        private TargetProvider _tp;

        protected override void DoBind()
        {
            _tp = _domain?.Get<TargetProvider>();
        }

        public override void Attack()
        {
            base.Attack();
            if (_zSkillUpgradeConfig == null) return;
            if (_tp == null)
            {
                _tp = _domain?.Get<TargetProvider>()
                    ?? UnityEngine.Object.FindFirstObjectByType<TargetProvider>();
                if (_tp == null) return;
            }
            SpawnAll().Forget();
        }

        private async UniTask SpawnAll()
        {
            int amount = (int)_zSkillUpgradeConfig.GetStat(StatType.AmountProjectile).Value;
            if (amount <= 0) amount = 1;

            for (int i = 0; i < amount; i++)
            {
                _cancellationTokenSource.Token.ThrowIfCancellationRequested();
                ThrowOne(i, amount);
            }
            await UniTask.Yield(); // Just to satisfy async signature if needed
        }

        private void ThrowOne(int index, int totalAmount)
        {
            float range = (float)_zSkillUpgradeConfig.GetStat(StatType.RangeFind).Value;
            double speed = _zSkillUpgradeConfig.GetStat(StatType.FireSpeed).Value;

            Vector3 dir;
            if (totalAmount == 1)
            {
                // Target: nearest enemy if available; else random offset around owner.
                var partner = _tp.GetTarget(_owner, range, _behaviorConfig.SearchTargetType);
                if (partner != null)
                {
                    dir = ((Vector2)(partner.Transform.Position.Value - _owner.Transform.Position.Value)).normalized;
                }
                else
                {
                    Vector2 rand = Random.insideUnitCircle;
                    if (rand.sqrMagnitude < 0.001f) rand = Vector2.right;
                    dir = rand.normalized;
                }
            }
            else
            {
                float angle = (360f / totalAmount) * index;
                dir = new Vector3(Mathf.Cos(angle * Mathf.Deg2Rad), Mathf.Sin(angle * Mathf.Deg2Rad), 0).normalized;
            }

            var projectile = SpawnProjectileEntity(_behaviorConfig.ProjectileConfig);
            if (projectile == null) return;
            projectile.Transform.Position.Set(_owner.Transform.Position.Value);
            if (projectile is BombProjectile bomb)
            {
                // Hook the landing event: instead of standard Explode (instant AoE), spawn
                // the fire patch. We do this by setting Bomb's OnHitGround indirectly —
                // bomb already calls Explode after TimeDelayExplosion. Override by clobbering
                // the behavior with a custom land handler.
                bomb.SetDirectAndSpeedProjectile(speed, dir);
                // Replace Explode → SpawnPatch by chaining: poll bomb death + spawn patch
                // at the bomb's last position. Cheaper than refactoring BombProjectile.
                _ = WatchAndSpawnPatch(bomb);
            }
            EntityManager?.Add(projectile);
        }

        private async UniTask WatchAndSpawnPatch(BombProjectile bomb)
        {
            // Wait for the bomb to die (OnHitGround → delay → Explode → OnDeath). Then
            // spawn the patch at its final position. Inlining this avoids touching
            // BombProjectile to add a generic land-hook callback.
            while (bomb != null && !bomb.IsDead)
            {
                await UniTask.Yield(_cancellationTokenSource.Token);
            }
            if (bomb == null) return;
            Vector3 landPos = bomb.Transform.Position.Value;
            SpawnPatch(landPos);
        }

        private void SpawnPatch(Vector3 pos)
        {
            var prefab = _behaviorConfig.PatchPrefab;
            FirePatch patch;
            GameObject go;

            if (prefab != null)
            {
                go = UnityEngine.Object.Instantiate(prefab, pos, Quaternion.identity);
                patch = go.GetComponent<FirePatch>();
                if (patch == null) patch = go.AddComponent<FirePatch>();
            }
            else
            {
                // No prefab assigned: spawn a bare GameObject so DoT still works (no visual).
                go = new GameObject("FirePatch (auto)");
                go.transform.position = pos;
                patch = go.AddComponent<FirePatch>();
            }

            float radius = (float)_zSkillUpgradeConfig.GetStat(StatType.RadiusExplosion).Value;
            if (radius <= 0) radius = 2f;
            double damage = _zSkillUpgradeConfig.GetStat(StatType.ATK).Value;
            patch.Configure(radius, damage,
                            _behaviorConfig.PatchDuration,
                            _behaviorConfig.PatchTickInterval,
                            _owner is PlayerCharacter);
        }
    }
}
