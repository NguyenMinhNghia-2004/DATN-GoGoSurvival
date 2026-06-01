using System;
using System.Threading;
using UnityEngine;

namespace Luzart
{
    /// <summary>
    /// Base class for skill behaviors. Each behavior is a MonoBehaviour attached as a child
    /// of the owning ZSkill's GameObject (one GO per behavior — visible in Inspector). The
    /// skill creates its behaviors during Bind via <see cref="ZSkillBehaviorConfig.CreateBehavior"/>
    /// which either Instantiates the config's `_behaviorPrefab` or AddComponent's the concrete
    /// behavior type on a fresh child GameObject.
    ///
    /// <para>Unity drives Update directly — ZSkill does NOT iterate and call behavior.Update.
    /// LevelIndex.Changed is wired in Bind so level-ups still flow through RefreshNumbers.</para>
    /// </summary>
    public abstract class ZSkillBehavior<T> : MonoBehaviour, IZSkillBehavior where T : ZSkillBehaviorConfig
    {
        protected IZSkill _skill;
        protected T _behaviorConfig;
        protected ZSkillUpgradeConfig _zSkillUpgradeConfig;
        protected EntityManager _entityManager;
        protected double _coolDown;
        protected double _coolDownReset = 1;
        protected CancellationTokenSource _cancellationTokenSource;
        protected bool _bound;

        public IZSkill Skill => _skill;
        public T BehaviorConfig => _behaviorConfig;
        IEntity IBehavior.Owner => _skill?.Owner;
        protected IEntity _owner => _skill?.Owner;
        protected IDomain _domain => _skill?.Owner?.MyDomain;

        /// <summary>Called by ZSkillBehaviorConfig.CreateBehavior right after Instantiate/AddComponent.
        /// Stores the owning skill + config, wires LevelIndex.Changed, runs RefreshNumbers.</summary>
        public void Bind(IZSkill skill, T behaviorConfig)
        {
            if (_bound) return;
            _skill = skill;
            _behaviorConfig = behaviorConfig;
            _cancellationTokenSource = new CancellationTokenSource();
            if (_skill != null) _skill.LevelIndex.Changed += LevelIndex_Changed;
            _entityManager = _domain?.Get<EntityManager>();
            RefreshNumbers();
            DoBind();
            _bound = true;
        }

        /// <summary>Hook for derived classes — runs once after Bind sets up state.</summary>
        protected virtual void DoBind() { }

        // Lazy accessor — guarantees Domain lookup is retried if the field was null at Bind time.
        protected EntityManager EntityManager
        {
            get
            {
                if (_entityManager == null && _domain != null) _entityManager = _domain.Get<EntityManager>();
                return _entityManager;
            }
        }

        protected void LevelIndex_Changed(INumber obj) => RefreshNumbers();

        private static readonly System.Collections.Generic.HashSet<string> _missingUpgradesWarned = new();

        protected virtual void RefreshNumbers()
        {
            if (_skill?.Config == null) return;
            var upgrades = _skill.Config.UpgradeConfigs;
            if (upgrades == null || upgrades.Count == 0)
            {
                if (_missingUpgradesWarned.Add(_skill.Config.name))
                    Debug.LogWarning($"[ZSkillBehavior] '{_skill.Config.name}' has no UpgradeConfigs — skill is inert (logged once).");
                return;
            }
            int level = (int)_skill.LevelIndex.Value;
            int clamped = Mathf.Clamp(level, 0, upgrades.Count - 1);
            int preLevel = clamped - 1;
            _zSkillUpgradeConfig = upgrades[clamped];
            if (preLevel >= 0) upgrades[preLevel].TerminalUpgrade();
            _zSkillUpgradeConfig.InitUpgrade();
            if (_skill.Config.ETypeSkill == ETypeSkill.Active)
            {
                _coolDownReset = _zSkillUpgradeConfig.GetStat(StatType.Cooldown).Value;
            }
        }

        // ───── Unity lifecycle drives the tick ─────
        private void Start() => DoStart();
        private void Update()
        {
            if (!_bound) return;
            DoUpdate(Time.deltaTime);
        }
        private void OnDestroy()
        {
            DoOnDestroy();
            DoDispose();
        }

        protected virtual void DoStart() { }

        protected virtual void DoUpdate(float dt)
        {
            if (_zSkillUpgradeConfig == null) return;
            _coolDown += dt;
            if (_coolDown >= _coolDownReset)
            {
                _coolDown = 0;
                Attack();
            }
        }

        protected virtual void DoDispose()
        {
            if (_skill != null) _skill.LevelIndex.Changed -= LevelIndex_Changed;
            if (_cancellationTokenSource != null && !_cancellationTokenSource.IsCancellationRequested)
            {
                _cancellationTokenSource.Cancel();
                _cancellationTokenSource.Dispose();
                _cancellationTokenSource = null;
            }
        }

        protected virtual void DoOnDestroy() { }

        public virtual void Attack() { }

        // ───── IBehavior explicit impl (some framework code may still call these) ─────
        void IBehavior.Start() => DoStart();
        void IBehavior.Update(float dt) => DoUpdate(dt);
        void IBehavior.OnDestroy() => DoOnDestroy();
        void IDisposable.Dispose() => DoDispose();

        protected virtual ProjectileEntity SpawnProjectileEntity(ProjectileConfig projectileConfig)
        {
            if (projectileConfig == null || _owner == null) return null;
            projectileConfig.InitStat(_owner, _zSkillUpgradeConfig);
            var projectile = projectileConfig.CreateProjectile(_owner);
            if (projectile == null) return null;
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
        Stat_HP = 101,
        Stat_Gold = 102,
    }

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
        Runtime_HP = 50,
        Runtime_XP = 51,
        Runtime_Gold = 52,
        Runtime_EnemyKilled = 53,
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
