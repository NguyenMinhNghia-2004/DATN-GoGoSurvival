using System;
using System.Collections.Generic;
using UnityEngine;

namespace Luzart
{
    /// <summary>
    /// Survivor.io-style per-skill MonoBehaviour. Lives as a child GameObject under
    /// <c>Player/Skills/</c> (one GO per equipped skill). Holds:
    /// <list type="bullet">
    /// <item><see cref="_skillConfig"/> — the SO that defines the skill (BehaviorConfigs +
    /// UpgradeConfigs).</item>
    /// <item><see cref="_behaviors"/> — runtime cooldown engines built from the config's
    /// <c>BehaviorConfigs</c>.</item>
    /// <item><see cref="_levelIndex"/> — current upgrade level (0-based).</item>
    /// </list>
    ///
    /// <para>This replaces the older `ZSkill` (plain class) + `ZSkillRuntime` (MonoBehaviour
    /// wrapper) split — one class is enough. <see cref="Bind"/> is called once by
    /// <see cref="LuzartPlayerEntityRoot"/> right after Instantiate to wire the owning entity
    /// and start ticking.</para>
    ///
    /// <para>Update is gated on <see cref="ClassicModeController.IsPlaying"/> — at MainMenu /
    /// Pause / Ended the skill does not tick (no Attack, no target search, no projectile
    /// spawn). Avoids NRE spam from systems that aren't bootstrapped yet (CollisionManager,
    /// SpatialHash).</para>
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ZSkill : MonoBehaviour, IZSkill
    {
        [SerializeField] private ZSkillConfig _skillConfig;

        private IEntity _entity;
        private INumberWithSet _levelIndex = new Number(0);
        private List<IZSkillBehavior> _behaviors;
        private bool _bound;

        // Cached lookup — ClassicModeController never changes identity at runtime.
        private ClassicModeController _classicMode;
        private bool _classicLookedUp;

        // ---- IZSkill members ----
        IEntity IZSkill.Entity => _entity;
        ZSkillConfig IZSkill.Config => _skillConfig;
        INumberWithSet IZSkill.LevelIndex => _levelIndex;
        IReadOnlyList<IZSkillBehavior> IZSkill.Behaviors => _behaviors;
        
        private float _totalDamageDealt;
        float IZSkill.TotalDamageDealt => _totalDamageDealt;
        void IZSkill.RecordDamage(float damage) => _totalDamageDealt += damage;
        
        IEntity IBehavior.Owner => _entity;
        public ZSkillConfig Config => _skillConfig;
        public bool IsBound => _bound;

        /// <summary>Bind to an owning entity + config. Constructs the behavior cooldown engines
        /// from <c>config.BehaviorConfigs</c> and starts them. Called once after AddComponent by
        /// <see cref="LuzartPlayerEntityRoot.SpawnSkill"/> (starting skills) or
        /// <see cref="LuzartPlayerEntityRoot.AddSkill"/> (level-up).</summary>
        public void Bind(IEntity owner, ZSkillConfig config)
        {
            if (_bound)
            {
                Debug.LogWarning($"[ZSkill] Already bound: {name}");
                return;
            }
            if (owner == null || config == null)
            {
                Debug.LogWarning($"[ZSkill] Bind skipped — owner or config null on {name}");
                return;
            }
            _entity = owner;
            _skillConfig = config;
            _levelIndex = new Number(0);
            _behaviors = new List<IZSkillBehavior>();
            try
            {
                foreach (var bc in _skillConfig.BehaviorConfigs)
                {
                    _behaviors.Add(bc.CreateBehavior(this));
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[ZSkill] CreateSkill {_skillConfig.name} Bug : {e}");
            }
            ((IBehavior)this).Start();
            _bound = true;
        }

        private void Update()
        {
            if (!_bound || _behaviors == null) return;

            // Gate ticking on the run being active. At MainMenu / Pause / Ended the player
            // exists but the skill should not search for targets or fire.
            if (!_classicLookedUp)
            {
                _classicMode = SceneRootManager.Instance != null
                    ? SceneRootManager.Instance.Domain?.Get<ClassicModeController>()
                    : null;
                _classicLookedUp = true;
            }
            if (_classicMode != null && !_classicMode.IsPlaying) return;

            // Behaviors are now MonoBehaviour children — Unity drives their Update.
            // ZSkill no longer iterates _behaviors here.
        }

        private void OnDestroy()
        {
            // Behavior GameObjects are children of this — Unity recursively destroys them,
            // and their OnDestroy/DoDispose fires automatically. Nothing else to do here.
            _behaviors = null;
            _bound = false;
        }

        // ---- IBehavior dispatch (interface compat — usually no longer called externally) ----
        void IDisposable.Dispose() { /* OnDestroy handles cleanup */ }
        void IBehavior.Start() { /* Unity Start drives behaviors directly */ }
        void IBehavior.Update(float dt) { /* Unity Update drives behaviors directly */ }
        void IBehavior.OnDestroy() { /* Unity OnDestroy drives behaviors directly */ }

        // ---- IZSkill level / upgrade ----
        void IZSkill.UpgradeSkill()
        {
            _levelIndex.Set(_levelIndex.Value + 1);
        }

        bool IZSkill.IsUpgradeSkill()
        {
            if (_skillConfig == null) return false;
            int levelCurrent = (int)_levelIndex.Value;
            int maxLevel = _skillConfig.UpgradeConfigs.Count;
            if (levelCurrent >= maxLevel - 1) return false;
            return _skillConfig.IsCanUpgradeLevel.Value;
        }
    }
}
