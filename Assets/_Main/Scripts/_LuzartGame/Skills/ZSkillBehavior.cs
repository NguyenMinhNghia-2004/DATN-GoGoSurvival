using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Threading;
using Unity.VisualScripting.FullSerializer;
namespace Luzart
{
    public abstract class ZSkillBehavior<T> : IZSkillBehavior where T : ZSkillBehaviorConfig
    {
        protected readonly IZSkill _skill;
        protected readonly T _behaviorConfig;
        protected ZSkillUpgradeConfig _zSkillUpgradeConfig;
        protected EntityManager _entityManager;
        protected double _coolDown;
        protected double _coolDownReset = 1;
        protected CancellationTokenSource _cancellationTokenSource;
        public IZSkill Skill => _skill;
        public T BehaviorConfig => _behaviorConfig;
        IEntity IBehavior.Owner => _skill.Owner;
        protected IEntity _owner => _skill.Owner;
        protected IDomain _domain => _skill.Owner.MyDomain;
        protected ZSkillBehavior(IZSkill skill, T behaviorConfig)
        {
            this._skill = skill;
            this._behaviorConfig = behaviorConfig;
            this._cancellationTokenSource = new CancellationTokenSource();
            this._skill.LevelIndex.Changed += LevelIndex_Changed;
            // EntityManager may not be registered yet at ctor time (Domain bootstrap order).
            // Use the property below to lazy-fetch on first need.
            _entityManager = _domain?.Get<EntityManager>();
            RefreshNumbers();
        }
        // Lazy accessor — guarantees Domain lookup is retried if the field was null at ctor time.
        protected EntityManager EntityManager
        {
            get
            {
                if (_entityManager == null && _domain != null) _entityManager = _domain.Get<EntityManager>();
                return _entityManager;
            }
        }
        protected void LevelIndex_Changed(INumber obj)
        {
            RefreshNumbers();
        }
        private static readonly System.Collections.Generic.HashSet<string> _missingUpgradesWarned = new System.Collections.Generic.HashSet<string>();
        protected virtual void RefreshNumbers()
        {
            var upgrades = _skill.Config.UpgradeConfigs;
            if (upgrades == null || upgrades.Count == 0)
            {
                if (_missingUpgradesWarned.Add(_skill.Config.name))
                    UnityEngine.Debug.LogWarning($"[ZSkillBehavior] '{_skill.Config.name}' has no UpgradeConfigs — skill is inert (logged once).");
                return;
            }
            int level = (int)Skill.LevelIndex.Value;
            int clamped = UnityEngine.Mathf.Clamp(level, 0, upgrades.Count - 1);
            int preLevel = clamped - 1;
            _zSkillUpgradeConfig = upgrades[clamped];
            if (preLevel >= 0)
            {
                var _preZSkillUpgradeConfig = upgrades[preLevel];
                _preZSkillUpgradeConfig.TerminalUpgrade();
            }
            _zSkillUpgradeConfig.InitUpgrade();
            if (Skill.Config.ETypeSkill == ETypeSkill.Active)
            {
                _coolDownReset = _zSkillUpgradeConfig.GetStat(StatType.Cooldown).Value;
            }
        }
        void IDisposable.Dispose()
        {
            DoDispose();
        }
        protected virtual void DoDispose()
        {
            _skill.LevelIndex.Changed -= LevelIndex_Changed;
            if (_cancellationTokenSource != null && !_cancellationTokenSource.IsCancellationRequested)
            {
                _cancellationTokenSource.Cancel();
            }
        }
        void IBehavior.Start()
        {
            DoStart();
        }
        void IBehavior.Update(float dt)
        {
            DoUpdate(dt);
        }
        void IBehavior.OnDestroy()
        {
            DoOnDestroy();
        }
        protected virtual void DoStart()
        {
        }
        protected virtual void DoUpdate(float dt)
        {
            if (_zSkillUpgradeConfig == null) return; // no upgrade rows configured — skill is inert
            _coolDown+= dt;
            if(_coolDown >= _coolDownReset)
            {
                _coolDown = 0;
                Attack();
            }
        }
        protected virtual void DoOnDestroy()
        {
            if (_cancellationTokenSource != null && !_cancellationTokenSource.IsCancellationRequested)
            {
                _cancellationTokenSource.Cancel();
                _cancellationTokenSource.Dispose();
                _cancellationTokenSource = null;
            }
        }
        public virtual void Attack()
        {
        }
        //protected virtual async UniTask<ProjectileEntity> SpawnProjectileEntity(ProjectileConfig projectileConfig)
        //{
        //    if (projectileConfig == null || _owner == null)
        //    {
        //        return null;
        //    }
        //    projectileConfig.InitStat(_owner, _zSkillUpgradeConfig);
        //    var projectile = projectileConfig.CreateProjectile(_owner);
        //    if (projectile == null)
        //    {
        //        return null;
        //    }
        //    projectile.Inject(_owner.MyDomain);
        //    projectile.Initialize();
        //    projectile.StartContent();
        //    return projectile;
        //}
        protected virtual ProjectileEntity SpawnProjectileEntity(ProjectileConfig projectileConfig)
        {
            if (projectileConfig == null || _owner == null)
            {
                return null;
            }
            projectileConfig.InitStat(_owner, _zSkillUpgradeConfig);
            var projectile = projectileConfig.CreateProjectile(_owner);
            if (projectile == null)
            {
                return null;
            }
            projectile.Inject(_owner.MyDomain);
            projectile.Initialize();
            projectile.Start();
            return projectile;
        }
    }
    // ===== Skill Type ENUM =====  
    public enum SkillDefine
    {
        Active_Normal = 0,
        Active_Pistol = 1,
        Active_Laser = 2,
        Active_Boomerang = 3,
        Active_Lightning = 4,
        Active_Bomb = 5,
        // Upgrade Types
        Deactive_ATK = 51,
        Deactive_Amor = 52,
        Deactive_HPMax = 53,
        Deactive_Speed = 54,
        Deactive_FireSpeed = 55,
        Deactive_Cooldown = 56,
        Deactive_Heal = 57,
        Luck = 58,
        TileChiMangUpgrade = 59,
        SatThuongChiMangUpgrade = 60,
        XP = 61,
        // Stat Types
        Stat_HP = 101,
        Stat_Gold = 102,
    }
    // ===== STAT TYPE ENUM =====
    public enum StatType
    {
        HPMax = 0,
        ATK = 1,
        Speed = 2,
        Cooldown = 3,
        FireSpeed = 4,
        TiLeChiMang = 5,
        SatThuongChiMang = 6,
        Armor = 7,
        Luck = 8,
        PhanTramXPTangLen = 9,
        Heal = 10,
        //
        Runtime_HP = 50,
        Runtime_XP = 51,
        Runtime_Gold = 52,
        Runtime_EnemyKilled = 53,
        //
        RangeFind = 101,
        RadiusCollider = 102,
        AmountProjectile = 103,
        TimeBreak = 104,
        RadiusExplosion = 105,
        ATKMultiplierExplosion = 106,
        TimeDelayExplosion = 107,
        TotalSkill = 108,
    }
}