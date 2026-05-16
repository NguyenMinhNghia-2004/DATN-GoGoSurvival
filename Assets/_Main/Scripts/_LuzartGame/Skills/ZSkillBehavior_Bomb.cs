using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using UnityEngine;
using UnityEngine.TextCore.Text;
namespace Luzart
{
    public class ZSkillBehavior_Bomb : ZSkillBehavior<ZSkillBehaviorConfig_Bomb>
    {
        public ZSkillBehavior_Bomb(IZSkill skill, ZSkillBehaviorConfig_Bomb behaviorConfig) : base(skill, behaviorConfig)
        {
        }
        public override void Attack()
        {
            base.Attack();
            SpawnProjectile().Forget();
        }
        private async UniTask SpawnProjectile()
        {
            try
            {
                double speed = _zSkillUpgradeConfig.GetStat(StatType.FireSpeed).Value;
                int length = (int)_zSkillUpgradeConfig.GetStat(StatType.AmountProjectile).Value;
                float timeBreak = (float)_zSkillUpgradeConfig.GetStat(StatType.TimeBreak).Value;
                for (int i = 0; i < length; i++)
                {
                    // Check for cancellation before each projectile spawn
                    _cancellationTokenSource.Token.ThrowIfCancellationRequested();
                    var direct = VectorExtensions.GetCircularDirection(Vector2.left, length, i);
                    Vector2 pos = direct * (float)_zSkillUpgradeConfig.GetStat(StatType.RangeFind).Value;
                    // Additional null check for safety
                    if (_owner?.Transform?.Position == null)
                    {
                        Debug.LogWarning("[ZSkillBehavior_Bomb] Owner or Transform is null, stopping projectile spawn");
                        return;
                    }
                    var projectile = SpawnProjectileEntity(_behaviorConfig.ProjectileConfig);
                    if (projectile == null)
                    {
                        Debug.LogWarning("[ZSkillBehavior_Bomb] Failed to spawn projectile");
                        continue;
                    }
                    projectile.Transform.Position.Set(_owner.Transform.Position.Value);
                    float angle = Mathf.Atan2(direct.y, direct.x) * Mathf.Rad2Deg;
                    var quaternion = Quaternion.Euler(new Vector3(0f, 0f, angle - 90f));
                    projectile.Transform.SetRotation(quaternion);
                    if (projectile is INormalProjectile normalProjectile)
                    {
                        normalProjectile.SetDirectAndSpeedProjectile(speed, direct);
                    }
                    _entityManager?.Add(projectile);
                    if (timeBreak > 0)
                    {
                        // Use cancellation token with delay
                        await UniTask.WaitForSeconds(timeBreak, cancellationToken: _cancellationTokenSource.Token);
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // Expected when cancellation is requested - log for debugging if needed
                Debug.Log("[ZSkillBehavior_Bomb] SpawnProjectile task was cancelled");
            }
            catch (Exception ex)
            {
                // Log unexpected exceptions
                Debug.LogError($"[ZSkillBehavior_Bomb] Unexpected error in SpawnProjectile: {ex.Message}\n{ex.StackTrace}");
            }
        }
    }
}
