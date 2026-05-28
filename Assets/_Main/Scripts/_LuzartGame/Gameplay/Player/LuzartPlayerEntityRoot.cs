using System.Collections.Generic;
using UnityEngine;

namespace Luzart
{
    /// <summary>
    /// Phase F replacement for <c>DATNPlayerEntityAdapter</c>.
    ///
    /// <para>Creates a <see cref="LuzartPlayerCharacter"/> (a PlayerCharacter that skips
    /// the EntityBluePrint requirement, like DATNPlayerCharacter), registers it into
    /// Domain, and each frame syncs the legacy GameObject Transform → framework
    /// Position.</para>
    ///
    /// <para><strong>Key difference vs DATN adapter</strong>: instead of attaching a
    /// <see cref="SkillControllerBehavior"/> that owns plain-class IZSkill instances,
    /// each starting skill becomes a <see cref="ZSkillRuntime"/> child GameObject
    /// instantiated under <see cref="_skillsContainer"/> (typically <c>Player/Skills/</c>).
    /// This matches the Survivor.io mental model where each weapon is a thing in the
    /// scene.</para>
    ///
    /// <para>Dormant by default. Enabled when
    /// <see cref="Migration.MigrationFlags.UseLuzartPlayerEntityRoot"/> is true. Until
    /// then, DATNPlayerEntityAdapter on the same GameObject creates the live
    /// PlayerCharacter.</para>
    /// </summary>
    public class LuzartPlayerEntityRoot : AbstractMonoBehaviorContent
    {
        [Header("Stats (optional — leave null for zero defaults)")]
        [SerializeField] private StatsConfig _statsConfig;

        [Header("Starting skills (drag ZSkillConfig assets here)")]
        [Tooltip("Picked from Assets/_Main/Data/Skills/Configs/. Each becomes a ZSkillRuntime " +
                 "child GameObject under Skills Container at Initialize.")]
        [SerializeField] private List<ZSkillConfig> _startingSkills = new List<ZSkillConfig>();

        [Header("Container for spawned ZSkillRuntime children")]
        [Tooltip("Parent Transform for runtime skill GOs. Defaults to a child named 'Skills' " +
                 "under this GameObject if left unwired.")]
        [SerializeField] private Transform _skillsContainer;

        private LuzartPlayerCharacter _character;
        private readonly List<ZSkillRuntime> _skillRuntimes = new List<ZSkillRuntime>();

        public LuzartPlayerCharacter Character => _character;
        public IReadOnlyList<ZSkillRuntime> SkillRuntimes => _skillRuntimes;

        private bool ActivePerFlag
        {
            get
            {
                var flags = _domain != null ? _domain.Get<Migration.MigrationFlags>() : null;
                return flags != null && flags.UseLuzartPlayerEntityRoot;
            }
        }

        public override void DoInject(IDomain domain)
        {
            base.DoInject(domain);
            if (!ActivePerFlag) return;

            _character = new LuzartPlayerCharacter(
                _statsConfig,
                string.IsNullOrEmpty(_id) ? "LuzartPlayer" : _id);

            // Domain.Add overwrites by id; when both Luzart + DATN adapter exist
            // on the same GO, whichever runs later wins. Execution order on
            // AbstractMonoBehaviorContent is -1000; both share it, so order is
            // the array order in DomainContentLoader / FindObjectsOfType.
            domain.Add<PlayerCharacter>(_character, _character.Id);
        }

        public override void DoInitialize()
        {
            base.DoInitialize();
            if (_character == null) return;

            _character.Inject(_domain);
            _character.Initialize();
            if (_character.Transform != null)
                _character.Transform.SetPosition(transform.position);

            SpawnStartingSkills();
        }

        public override void DoStart()
        {
            base.DoStart();
            _character?.Start();
        }

        protected override void DoUpdate(float dt)
        {
            base.DoUpdate(dt);
            if (_character == null) return;
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
            DespawnAllSkills();
            _character?.Terminate();
        }

        private Transform ResolveSkillsContainer()
        {
            if (_skillsContainer != null) return _skillsContainer;
            var child = transform.Find("Skills");
            if (child == null)
            {
                var go = new GameObject("Skills");
                go.transform.SetParent(transform, false);
                child = go.transform;
            }
            _skillsContainer = child;
            return child;
        }

        private void SpawnStartingSkills()
        {
            if (_startingSkills == null || _startingSkills.Count == 0) return;
            var parent = ResolveSkillsContainer();
            foreach (var cfg in _startingSkills)
            {
                if (cfg == null) continue;
                SpawnSkill(cfg, parent);
            }
        }

        private void SpawnSkill(ZSkillConfig cfg, Transform parent)
        {
            var go = new GameObject($"ZSkillRuntime_{cfg.name}");
            go.transform.SetParent(parent, false);
            var runtime = go.AddComponent<ZSkillRuntime>();
            runtime.Bind(_character, cfg);
            _skillRuntimes.Add(runtime);
        }

        private void DespawnAllSkills()
        {
            for (int i = 0; i < _skillRuntimes.Count; i++)
            {
                if (_skillRuntimes[i] != null && _skillRuntimes[i].gameObject != null)
                    Destroy(_skillRuntimes[i].gameObject);
            }
            _skillRuntimes.Clear();
        }
    }

    /// <summary>
    /// PlayerCharacter that skips the EntityBluePrint requirement (mirrors
    /// DATNPlayerCharacter's shim). Initializes StatsBehavior from a StatsConfig SO
    /// if provided. No SkillControllerBehavior — skills live as ZSkillRuntime
    /// children on the GameObject side instead.
    /// </summary>
    public class LuzartPlayerCharacter : PlayerCharacter
    {
        private readonly StatsConfig _maybeStats;

        public LuzartPlayerCharacter(StatsConfig stats, string id) : base(null)
        {
            _maybeStats = stats;
            Id = id;
        }

        public override void Initialize()
        {
            // Intentionally do NOT call base.Initialize() — needs entityBluePrint.
            if (_maybeStats != null && _maybeStats.AssetStats != null)
                InitStats(_maybeStats.AssetStats);
        }
    }
}
