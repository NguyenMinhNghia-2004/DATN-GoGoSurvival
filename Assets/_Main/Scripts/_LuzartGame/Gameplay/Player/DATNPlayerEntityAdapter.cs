using System.Collections.Generic;
using UnityEngine;

namespace Luzart
{
    /// <summary>
    /// Bridges DATN's existing legacy Player GameObject into the Luzart framework as an
    /// `IEntity` / `PlayerCharacter` with a real <see cref="SkillControllerBehavior"/>.
    ///
    /// Per-entity model:
    ///   - This adapter creates one <see cref="DATNPlayerCharacter"/> POCO.
    ///   - The character owns its own <see cref="StatsBehavior"/> (HP/ATK/Speed) and
    ///     <see cref="SkillControllerBehavior"/> (skills configured in <see cref="startingSkills"/>).
    ///   - Each skill is a separate <see cref="IZSkill"/> instance. The skill ticks every frame
    ///     and fires its behavior (e.g. CreateProjectile) according to its cooldown.
    /// </summary>
    public class DATNPlayerEntityAdapter : AbstractMonoBehaviorContent
    {
        [Header("Stats (optional — leave null for zero defaults)")]
        [SerializeField] private StatsConfig statsConfigOverride;

        [Header("Starting skills (drag ZSkillConfig assets here)")]
        [Tooltip("Skills this player is born with. E.g. ZSk_Kunai. Picked from " +
                 "Assets/_Main/Data/Skills/Configs/. SkillControllerBehavior will tick these.")]
        [SerializeField] private List<ZSkillConfig> startingSkills = new List<ZSkillConfig>();

        private DATNPlayerCharacter _character;

        public DATNPlayerCharacter Character => _character;

        public override void DoInject(IDomain domain)
        {
            base.DoInject(domain);
            _character = new DATNPlayerCharacter(
                statsConfigOverride,
                string.IsNullOrEmpty(_id) ? "DATNPlayer" : _id,
                startingSkills);
            domain.Add<PlayerCharacter>(_character, _character.Id);
        }

        public override void DoInitialize()
        {
            base.DoInitialize();
            _character.Inject(_domain);
            _character.Initialize();
            // Seed transform once at boot.
            if (_character.Transform != null)
                _character.Transform.SetPosition(transform.position);
        }

        public override void DoStart()
        {
            base.DoStart();
            _character.Start();
        }

        protected override void DoUpdate(float dt)
        {
            base.DoUpdate(dt);
            if (_character == null) return;
            // Sync legacy Unity transform → framework transform.
            // Future-direction: when MoveMonoBehavior owns movement, reverse the direction
            // (framework drives Unity transform via the MoveBehavior).
            if (_character.Transform != null)
                _character.Transform.SetPosition(transform.position);
            _character.OnUpdate(dt);
        }

        public override void DoStop()
        {
            base.DoStop();
            _character?.Stop();
        }

        public override void DoTerminate()
        {
            base.DoTerminate();
            _character?.Terminate();
        }
    }

    /// <summary>
    /// Lightweight PlayerCharacter that does NOT require a pre-authored EntityBluePrint+StatsConfig.
    /// Use this for DATN's legacy player which has its own HP/ATK pipeline in <c>PlayerManager</c>.
    ///
    /// Future: when you author proper <see cref="StatsConfig"/> + <see cref="EntityBluePrint"/>,
    /// swap this for the real <see cref="PlayerCharacter"/> via <see cref="EntityBluePrint"/>.
    /// </summary>
    public class DATNPlayerCharacter : PlayerCharacter
    {
        private readonly StatsConfig _maybeStats;
        private readonly List<ZSkillConfig> _startingSkills;
        private SkillControllerBehavior _skillController;
        public SkillControllerBehavior SkillController => _skillController;

        public DATNPlayerCharacter(StatsConfig statsConfigOverride, string id, List<ZSkillConfig> startingSkills) : base(null)
        {
            _maybeStats = statsConfigOverride;
            _startingSkills = startingSkills ?? new List<ZSkillConfig>();
            Id = id;
        }

        public override void Initialize()
        {
            // Intentionally do NOT call `base.Initialize()` (needs entityBluePrint).
            if (_maybeStats != null && _maybeStats.AssetStats != null)
                InitStats(_maybeStats.AssetStats);

            // Per-entity skill controller. Each ZSkillConfig becomes one IZSkill instance owned by THIS player.
            _skillController = new SkillControllerBehavior(this);
            foreach (var cfg in _startingSkills)
                if (cfg != null) _skillController.AddSkill(cfg);
            AddBehavior(_skillController);
        }
    }
}
