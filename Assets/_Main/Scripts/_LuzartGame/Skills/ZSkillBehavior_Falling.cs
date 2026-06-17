using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Luzart
{
    /// <summary>
    /// "Fall from above" attack pattern (Brick). For each shot:
    ///   1. Pick a target via TargetProvider (or random offset around player if none).
    ///   2. Spawn the projectile at target.xy + (0, dropHeight).
    ///   3. Set direction = down → gravity from BombProjectileConfig accelerates it.
    ///   4. Bomb's OnHitGround triggers AoE explosion at the target position.
    /// </summary>
    public class ZSkillBehavior_Falling : ZSkillBehavior<ZSkillBehaviorConfig_Falling>
    {
        private TargetProvider _targetProvider;

        protected override void DoBind()
        {
            _targetProvider = _domain?.Get<TargetProvider>();
        }

        public override void Attack()
        {
            base.Attack();
            if (_zSkillUpgradeConfig == null) return;
            // Lazy-resolve / auto-spawn TargetProvider (same defensive pattern as CreateProjectile).
            if (_targetProvider == null)
            {
                _targetProvider = _domain?.Get<TargetProvider>()
                    ?? (SceneRootManager.Instance != null ? SceneRootManager.Instance.Domain?.Get<TargetProvider>() : null)
                    ?? UnityEngine.Object.FindFirstObjectByType<TargetProvider>();
                if (_targetProvider == null) return;
            }
            SpawnFalling().Forget();
        }

        private async UniTask SpawnFalling()
        {
            float range = (float)_zSkillUpgradeConfig.GetStat(StatType.RangeFind).Value;
            int amount = (int)_zSkillUpgradeConfig.GetStat(StatType.AmountProjectile).Value;
            double timeBreak = _zSkillUpgradeConfig.GetStat(StatType.TimeBreak).Value;
            if (amount <= 0) amount = 1;

            for (int i = 0; i < amount; i++)
            {
                _cancellationTokenSource.Token.ThrowIfCancellationRequested();
                // Pick drop position: prefer the i-th nearest enemy; fall back to random
                // offset around player when there aren't enough targets.
                Vector3 dropPos = ResolveDropPosition(range, i);

                var projectile = SpawnProjectileEntity(_behaviorConfig.ProjectileConfig);
                if (projectile == null) continue;
                projectile.Transform.Position.Set(_owner.Transform.Position.Value);
                projectile.Transform.SetRotation(Quaternion.identity);
                if (projectile is BombProjectile bp)
                {
                    float g = 9.8f; // Standard gravity for parabola
                    float dropHeight = _behaviorConfig.DropHeight;
                    if (dropHeight <= 0) dropHeight = 5f;
                    
                    Vector2 startPos = _owner.Transform.Position.Value;
                    float dy = dropPos.y - startPos.y;
                    
                    // Peak is DropHeight above the higher of the two points
                    float peakHeight = Mathf.Max(0, dy) + dropHeight;
                    
                    float vy = Mathf.Sqrt(2f * g * peakHeight);
                    float discriminant = vy * vy - 2f * g * dy;
                    if (discriminant < 0) discriminant = 0;
                    float travelTime = (vy + Mathf.Sqrt(discriminant)) / g;
                    
                    bp.SetParabolaProjectile(startPos, dropPos, travelTime);
                }
                else if (projectile is INormalProjectile np)
                {
                    double speed = _zSkillUpgradeConfig.GetStat(StatType.FireSpeed).Value;
                    np.SetDirectAndSpeedProjectile(speed, Vector3.down);
                }
                EntityManager?.Add(projectile);

                if (timeBreak > 0)
                    await UniTask.WaitForSeconds((float)timeBreak, cancellationToken: _cancellationTokenSource.Token);
            }
        }

        private Vector3 ResolveDropPosition(float range, int index)
        {
            // Cheap multi-target: query once + use index modulo. If no enemies, random offset.
            var partner = _targetProvider.GetTarget(_owner, range, _behaviorConfig.SearchTargetType);
            if (partner != null)
            {
                // Add small random spread so multiple bricks don't stack on one enemy.
                Vector2 spread = index == 0 ? Vector2.zero : Random.insideUnitCircle * 0.8f;
                return partner.Transform.Position.Value + spread;
            }
            // Fallback: random ring around player.
            Vector2 fallback = Random.insideUnitCircle * _behaviorConfig.RandomFallbackRadius;
            return _owner.Transform.Position.Value + fallback;
        }
    }
}
