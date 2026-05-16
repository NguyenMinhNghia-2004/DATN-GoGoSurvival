using UnityEngine;

namespace Luzart
{
    /// <summary>
    /// Bridges DATN's legacy <c>EnemyManager</c> MonoBehaviour into the Luzart framework
    /// as an `IEntity` / `EnemyCharacter`. Attached per-enemy-instance.
    ///
    /// Lifecycle:
    ///   Awake → creates lightweight `DATNEnemyCharacter` (no Material/SkillConfig needed yet)
    ///         → registers with Domain via SceneRootManager (if available)
    ///   Update → syncs Unity transform.position to framework Transform
    ///   OnDestroy → unregisters
    ///
    /// Optional: attach a <see cref="DATNEnemyEntityAdapter"/> to your enemy prefab and the
    /// adapter will register the enemy as an IEntity on Spawn. Now skill behaviors that
    /// query `_domain.Get&lt;EntityManager&gt;()` and iterate enemies (e.g., TargetProvider)
    /// see DATN enemies. Existing DATN EnemyManager / health flow continues to work.
    /// </summary>
    public class DATNEnemyEntityAdapter : MonoBehaviour
    {
        [Header("Per-enemy skills (optional — bosses + elites usually have ranged attack skills)")]
        [Tooltip("ZSkillConfigs owned by THIS enemy instance. Each becomes an IZSkill that ticks " +
                 "every frame. Regular zombies leave empty (their attack is collision-only).")]
        [SerializeField] private System.Collections.Generic.List<ZSkillConfig> startingSkills = new System.Collections.Generic.List<ZSkillConfig>();

        private DATNEnemyCharacter _character;
        private EntityManager _entityManager;

        private void Awake()
        {
            if (SceneRootManager.Instance == null) return;
            var domain = SceneRootManager.Instance.Domain;
            if (domain == null) return;

            _entityManager = domain.Get<EntityManager>();
            _character = new DATNEnemyCharacter(name + "_" + GetInstanceID(), startingSkills);
            _character.Inject(domain);
            _character.Initialize();
            _character.Transform.SetPosition(transform.position);
            _character.Start();
            _entityManager?.Add(_character);
        }

        private void Update()
        {
            if (_character == null) return;
            _character.Transform.SetPosition(transform.position);
            _character.OnUpdate(Time.deltaTime);
        }

        private void OnDestroy()
        {
            if (_character == null) return;
            _entityManager?.Remove(_character);
            _character.Stop();
            _character.Terminate();
            _character = null;
        }

        /// <summary>External API: tell the framework this enemy took damage. Returns true if killed.</summary>
        public bool ApplyDamage(double dmg)
        {
            if (_character == null) return false;
            _character.TakeDamage(dmg);
            return _character.IsDead;
        }

        /// <summary>
        /// Apply per-enemy stats from the framework <see cref="StatsConfig"/>.
        /// Called by <see cref="EnemySpawnerManager"/> right after Instantiate so the
        /// IEntity has live stat values (HP, ATK, Speed). Subclasses can listen to
        /// Runtime_HP.Changed to drive DATN's existing health bar / death pipeline.
        /// </summary>
        public void ApplyStatsConfig(System.Collections.Generic.List<IStat> stats)
        {
            if (_character == null || stats == null) return;
            _character.InitStats(stats);
            _character.Stats.RestoreHP();
        }
    }

    /// <summary>
    /// Lightweight EnemyCharacter that does NOT require Material/SkillConfig at construction.
    /// Skip the heavy `EnemyCharacter.Initialize` (which adds RenderBehavior/AnimationBehavior etc.)
    /// — DATN's existing EnemyManager already drives the visual side.
    /// </summary>
    public class DATNEnemyCharacter : EnemyCharacter
    {
        private readonly System.Collections.Generic.List<ZSkillConfig> _startingSkills;
        private SkillControllerBehavior _skillController;
        public SkillControllerBehavior SkillController => _skillController;

        public DATNEnemyCharacter(string id, System.Collections.Generic.List<ZSkillConfig> startingSkills) : base(null, id)
        {
            _startingSkills = startingSkills ?? new System.Collections.Generic.List<ZSkillConfig>();
        }

        public override void Initialize()
        {
            // Intentionally skip base.Initialize() — that adds heavy behaviors
            // (Render/Animation/Collider) which DATN's legacy EnemyManager already handles
            // via the prefab's MonoBehaviour stack (Animator/SpriteRenderer/Collider2D).
            // Stats & Transform are still set up by EntityBase.Inject called earlier.

            // Per-enemy skill controller (only if this enemy has skills, e.g. boss).
            if (_startingSkills.Count > 0)
            {
                _skillController = new SkillControllerBehavior(this);
                foreach (var cfg in _startingSkills)
                    if (cfg != null) _skillController.AddSkill(cfg);
                AddBehavior(_skillController);
            }
        }

        public override void Start()
        {
            // Skip HP-changed subscription — DATN's EnemyManager owns visual HP for now.
            // (Framework Stats.Runtime_HP can still be read by skills via Get<EntityManager>.)
        }
    }
}
