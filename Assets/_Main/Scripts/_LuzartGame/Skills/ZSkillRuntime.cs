using UnityEngine;

namespace Luzart
{
    /// <summary>
    /// Survivor.io-style per-skill GameObject. Child of <c>Player/Skills/</c>. Hosts a single
    /// <see cref="ZSkillConfig"/> as a runtime <see cref="ZSkill"/> — the plain-class skill that
    /// builds its <see cref="IZSkillBehavior"/> list from <c>config.BehaviorConfigs</c> — and
    /// ticks it every frame.
    ///
    /// <para>This is the GameObject-child realization of the framework's <see cref="ZSkill"/>
    /// (the deliberate deviation from the reference's plain-class <c>SkillControllerBehavior</c>
    /// model: in GoGo Survival each weapon is a thing in the scene under the player). The hosted
    /// <see cref="ZSkill"/> owns the cooldown/target/spawn logic via its behaviors; this
    /// MonoBehaviour just drives its lifecycle (Start on Bind, Update each frame, Dispose on
    /// destroy) and exposes <see cref="Skill"/> for the level-up flow.</para>
    /// </summary>
    public class ZSkillRuntime : MonoBehaviour
    {
        [SerializeField] private ZSkillConfig _config;
        private IEntity _owner;
        private ZSkill _skill;
        private bool _bound;

        public ZSkillConfig Config => _config;
        public IEntity Owner => _owner;
        /// <summary>The hosted skill (null until <see cref="Bind"/>). Used by the level-up flow
        /// to read level / call <c>UpgradeSkill()</c>.</summary>
        public IZSkill Skill => _skill;
        public bool IsBound => _bound;

        /// <summary>Bind to an owning entity + config. Constructs the <see cref="ZSkill"/> (which
        /// instantiates its behaviors from <c>config.BehaviorConfigs</c> and refreshes their
        /// level-0 numbers) and starts it. Called once after Instantiate by
        /// <see cref="LuzartPlayerEntityRoot"/> (starting skills) or the level-up flow.</summary>
        public void Bind(IEntity owner, ZSkillConfig config)
        {
            if (_bound)
            {
                Debug.LogWarning($"[ZSkillRuntime] Already bound: {name}");
                return;
            }
            if (owner == null || config == null)
            {
                Debug.LogWarning($"[ZSkillRuntime] Bind skipped — owner or config null on {name}");
                return;
            }
            _owner = owner;
            _config = config;
            // ZSkill ctor builds the IZSkillBehavior list from config.BehaviorConfigs; each
            // behavior's ctor calls RefreshNumbers() so its level-0 upgrade config is ready.
            _skill = new ZSkill(owner, config);
            ((IBehavior)_skill).Start();
            _bound = true;
        }

        // Cached lookup — ClassicModeController never changes identity at runtime.
        private ClassicModeController _classicMode;
        private bool _classicLookedUp;

        private void Update()
        {
            if (!_bound || _skill == null) return;

            // Gate ticking on the run actually being active. At MainMenu / Pause /
            // post-end states the player exists but the skill shouldn't be searching
            // for targets or firing — also avoids NRE spam when CollisionManager/
            // SpatialHash aren't initialized yet (pre-Play).
            if (!_classicLookedUp)
            {
                _classicMode = SceneRootManager.Instance != null
                    ? SceneRootManager.Instance.Domain?.Get<ClassicModeController>()
                    : null;
                _classicLookedUp = true;
            }
            if (_classicMode != null && !_classicMode.IsPlaying) return;

            ((IBehavior)_skill).Update(Time.deltaTime);
        }

        private void OnDestroy()
        {
            if (_skill != null)
            {
                ((IBehavior)_skill).OnDestroy();
                ((System.IDisposable)_skill).Dispose();
                _skill = null;
            }
            _bound = false;
        }
    }
}
