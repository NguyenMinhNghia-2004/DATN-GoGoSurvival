using Cysharp.Threading.Tasks;
using System.Linq;
using UnityEngine;
using UnityEngine.TextCore.Text;
namespace Luzart
{
    public class ZSkillBehavior_CreateProjectile : ZSkillBehavior<ZSkillBehaviorConfig_CreateProjectile>
    {
        private TargetProvider _targetProvider;
        private Vector3 Direction { get; set; }
        public ZSkillBehavior_CreateProjectile(IZSkill skill, ZSkillBehaviorConfig_CreateProjectile behaviorConfig) : base(skill, behaviorConfig)
        {
        }
        protected override void DoStart()
        {
            base.DoStart();
            _targetProvider = _domain?.Get<TargetProvider>();
        }

        public override void Attack()
        {
            base.Attack();
            if (_zSkillUpgradeConfig == null) return;
            // Lazy-resolve TargetProvider — skill behavior is constructed before
            // SceneRootManager finishes registering scene contents into Domain, so
            // _domain.Get<TargetProvider>() in DoStart may return null. Retry per-Attack.
            if (_targetProvider == null)
            {
                _targetProvider = _domain?.Get<TargetProvider>();
                if (_targetProvider == null) return; // still not registered
            }
            float range = (float)_zSkillUpgradeConfig.GetStat(StatType.RangeFind).Value;
            IEntity partner = _targetProvider.GetTarget(_owner, range, _behaviorConfig.SearchTargetType);
            if (partner != null)
            {
                Direction = (partner.Transform.Position.Value - _owner.Transform.Position.Value).normalized;
            }
            else
            {
                return;
            }
            SpawnProjectile().Forget();
        }
        private async UniTask SpawnProjectile()
        {
            double speed = _zSkillUpgradeConfig.GetStat(StatType.FireSpeed).Value;
            int length = (int)_zSkillUpgradeConfig.GetStat(StatType.AmountProjectile).Value;
            double timeBreakDouble = _zSkillUpgradeConfig.GetStat(StatType.TimeBreak).Value;
            float timeBreak = (float)timeBreakDouble;
            if (length <= 0) length = 1;
            for (int i = 0; i < length; i++)
            {
                _cancellationTokenSource.Token.ThrowIfCancellationRequested();
                var projectile = SpawnProjectileEntity(_behaviorConfig.ProjectileConfig);
                if (projectile == null) continue;
                projectile.Transform.Position.Set(_owner.Transform.Position.Value);
                if (Direction != Vector3.zero)
                {
                    float angle = Mathf.Atan2(Direction.y, Direction.x) * Mathf.Rad2Deg;
                    var quaternion = Quaternion.Euler(new Vector3(0f, 0f, angle - 90f));
                    projectile.Transform.SetRotation(quaternion);
                    if (projectile is INormalProjectile normalProjectile)
                    {
                        normalProjectile.SetDirectAndSpeedProjectile(speed, Direction);
                    }
                }
                EntityManager?.Add(projectile);
                if (timeBreak > 0)
                {
                    await UniTask.WaitForSeconds(timeBreak, cancellationToken: _cancellationTokenSource.Token);
                }
            }
        }
    }
}