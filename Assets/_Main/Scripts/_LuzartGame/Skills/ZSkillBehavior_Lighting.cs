using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
namespace Luzart
{
    public class ZSkillBehavior_Lighting : ZSkillBehavior<ZSkillBehaviorConfig_Lighting>
    {
        private TargetProvider _targetProvider;
        protected override void DoBind()
        {
            _targetProvider = _domain?.Get<TargetProvider>();
        }
        public override void Attack()
        {
            base.Attack();
            float range = (float)_zSkillUpgradeConfig.GetStat(StatType.RangeFind).Value;
            using var pool = new ListPoolHelper<IEntity>(null);
            var listEntity = pool.List;
            listEntity = _targetProvider.GetTargetsInRange(_owner,range);
            if (listEntity == null || listEntity.Count == 0)
            {
                return;
            }
            SpawnProjectile(listEntity).Forget();
        }
        private async UniTask SpawnProjectile(List<IEntity> listEntity)
        {
            int length = (int)_zSkillUpgradeConfig.GetStat(StatType.AmountProjectile).Value;
            float timeBreak = (float)_zSkillUpgradeConfig.GetStat(StatType.TimeBreak).Value;
            for (int i = 0; i < length; i++)
            {
                _cancellationTokenSource.Token.ThrowIfCancellationRequested();
                Vector2 pos = _owner.Transform.Position.Value + UnityEngine.Random.insideUnitCircle * UnityEngine.Random.Range(0f, 3f);
                var projectile = SpawnProjectileEntity(_behaviorConfig.ProjectileConfig);
                if (listEntity != null && listEntity.Count > 0)
                {
                    pos = listEntity[0].Transform.Position.Value;
                    if (listEntity.Count > 1)
                    {
                        listEntity.RemoveAt(0);
                    }
                    if (projectile is ITargetProjectile targetProjectile)
                    {
                        targetProjectile.SetTarget(listEntity[0]);
                        targetProjectile.SetSpeed(100f);
                    }
                }
                projectile.Transform.Position.Set(pos);
                _entityManager.Add(projectile);
                if (timeBreak > 0)
                {
                    await UniTask.WaitForSeconds(timeBreak,cancellationToken: _cancellationTokenSource.Token);
                }
            }
        }
    }
}
