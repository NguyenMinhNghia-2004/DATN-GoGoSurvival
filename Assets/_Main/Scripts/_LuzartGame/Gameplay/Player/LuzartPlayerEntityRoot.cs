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
    /// each starting skill becomes a <see cref="ZSkill"/> child GameObject
    /// instantiated under <see cref="_skillsContainer"/> (typically <c>Player/Skills/</c>).
    /// This matches the Survivor.io mental model where each weapon is a thing in the
    /// scene.</para>
    ///
    /// <para>Dormant by default. Enabled when
    /// <see cref="Migration.MigrationFlags.UseLuzartPlayerEntityRoot"/> is true. Until
    /// then, DATNPlayerEntityAdapter on the same GameObject creates the live
    /// PlayerCharacter.</para>
    /// </summary>
    public class LuzartPlayerEntityRoot : AbstractMonoBehaviorContent, IRunParticipant
    {
        [Header("Stats (optional — leave null for zero defaults)")]
        [SerializeField] private StatsConfig _statsConfig;

        [Header("Starting skills (drag ZSkillConfig assets here)")]
        [Tooltip("Picked from Assets/_Main/Data/Skills/Configs/. Each becomes a ZSkill " +
                 "child GameObject under Skills Container at Initialize.")]
        [SerializeField] private List<ZSkillConfig> _startingSkills = new List<ZSkillConfig>();

        [Header("Container for spawned ZSkill children")]
        [Tooltip("Parent Transform for runtime skill GOs. Defaults to a child named 'Skills' " +
                 "under this GameObject if left unwired.")]
        [SerializeField] private Transform _skillsContainer;

        private LuzartPlayerCharacter _character;
        private readonly List<ZSkill> _skillRuntimes = new List<ZSkill>();

        // Designed spawn point (the player's scene-authored position at boot). Captured once
        // and restored at the start of every run so a new run begins at spawn instead of
        // wherever the player died/ended the previous run.
        private Vector3 _spawnPosition;
        private bool _spawnCaptured;

        public LuzartPlayerCharacter Character => _character;
        public IReadOnlyList<ZSkill> SkillRuntimes => _skillRuntimes;

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
            // F.G.6: DATNPlayerEntityAdapter has been deleted. Nothing to disable here.
            // PlayerCharacter override happens in DoInitialize (after MigrationFlags are
            // guaranteed to be in Domain).
        }

        public override void DoInitialize()
        {
            base.DoInitialize();
            // F.G.3: All DoInject calls have completed (including DomainContentLoader
            // adding MigrationFlags). Now safe to read ActivePerFlag.
            if (!ActivePerFlag) return;

            // Forcefully remove ALL existing PlayerCharacters from Domain.
            // DATN may have added one in its DoInject. We can't rely on insertion
            // order — wipe all, then add Luzart's as the sole canonical PC.
            var existing = _domain.GetAll<PlayerCharacter>();
            if (existing != null)
            {
                var copy = new List<PlayerCharacter>(existing);
                foreach (var pc in copy) _domain.Remove<PlayerCharacter>(pc);
            }

            _character = new LuzartPlayerCharacter(
                _statsConfig,
                string.IsNullOrEmpty(_id) ? "LuzartPlayer" : _id);
            _domain.Add<PlayerCharacter>(_character, _character.Id);

            _character.Inject(_domain);
            _character.Initialize();
            // Capture the scene-authored spawn point for per-run respawn.
            _spawnPosition = transform.position;
            _spawnCaptured = true;
            if (_character.Transform != null)
                _character.Transform.SetPosition(transform.position);

            // Bridge: lets TargetProvider's Physics2D query resolve hit Collider2D
            // back to this PlayerCharacter without going through CollisionManager.
            var refComp = GetComponent<EntityRef>();
            if (refComp == null) refComp = gameObject.AddComponent<EntityRef>();
            refComp.Entity = _character;

            // World-space HP bar floating above the player's head (it resolves HP from the Domain).
            if (GetComponent<PlayerHealthBar>() == null) gameObject.AddComponent<PlayerHealthBar>();

            // Starting skills are NOT spawned here anymore — they are spawned per-run in
            // OnRunBegin and destroyed in OnRunEnd (symmetric run lifecycle). Spawning at
            // init would (a) leave skills alive at the MainMenu and (b) double-apply their
            // passive stat bonuses when OnRunBegin respawns them on the first run.

            // F.G.2: Hook framework death state — when Runtime_HP drops to 0,
            // freeze the player's Rigidbody2D so legacy logic doesn't keep applying
            // velocity. GameController.OnHPChange already broadcasts Data_ClassicEndGame
            // which the SV_EndGameBridge translates into SV_LoseScreen.
            SubscribeDeath();
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
            UnsubscribeDeath();
            DespawnAllSkills();
            _character?.Terminate();
            // Remove our PlayerCharacter from the Domain so a despawned player doesn't leave a
            // dead entity that Domain.Get<PlayerCharacter>() (GetFirst) would keep returning.
            // Critical for the runtime spawn/despawn model: without this, the next spawn's
            // consumers (camera, AI, UpgradeSkillManager) could resolve the destroyed instance.
            if (_domain != null && _character != null)
                _domain.Remove<PlayerCharacter>(_character, _character.Id);
        }

        // ─────────────────────────────────────────────────────────────
        // IRunParticipant — driven by GameCoordinator.BeginRun/EndRun
        // Stops skill ticks + zeros player velocity when a run ends so the
        // player can't keep firing / coasting under the Win/Lose screen.
        // ─────────────────────────────────────────────────────────────
        void IRunParticipant.OnRunBegin()
        {
            // Full run-scoped RESPAWN, not a SetActive re-arm. Destroy every skill from a
            // prior run (level-up acquisitions included), then spawn the starting skills
            // fresh. This is what makes a new run start clean:
            //   • acquired-during-run skills are gone (only starting skills remain)
            //   • each skill re-Binds at _levelIndex = 0 (skill levels reset)
            //   • passive stat bonuses are undone — DespawnAllSkills destroys the skill
            //     GameObjects, whose child behaviors' DoDispose calls RemoveStatBonus,
            //     so StatsBehavior._statDefaultDict returns to its config base (equipment
            //     modifiers flow through a separate AssetNumber pipeline and are untouched).
            DespawnAllSkills();

            // Re-initialise stats from the live config BEFORE spawning skills. StatsBehavior
            // .Add rebuilds _statDefaultDict from the AssetStat values, which re-links the live
            // AssetNumber pipeline (so equipment changed at the menu between runs is reflected)
            // and clears any frozen drift left by prior-run ApplyStatBonus calls. Safe to call:
            // _skillRuntimes is empty here (skills were destroyed at the previous OnRunEnd), so
            // there are no pending RemoveStatBonus disposes to corrupt the freshly-built dict.
            if (_character != null && _statsConfig != null && _statsConfig.AssetStats != null)
                _character.InitStats(_statsConfig.AssetStats);

            SpawnStartingSkills();

            // Teleport the player back to its spawn point so a new run starts at the map
            // centre, not wherever the previous run ended. Sync the framework Transform too.
            if (_spawnCaptured)
            {
                transform.position = _spawnPosition;
                if (_character.Transform != null) _character.Transform.SetPosition(_spawnPosition);
            }

            // Death from a prior run freezes the Rigidbody2D via OnHpChangedForDeath;
            // re-enable physics + clear the latch so the next run starts clean.
            var rb = GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                rb.simulated = true;
                rb.linearVelocity = Vector2.zero;
            }
            _deathHandled = false;

            // Reset run-scoped player progression: full HP (to current HPMax incl. equipment
            // + freshly-applied starting-skill bonuses) and zero XP/level progress.
            if (_character != null && _character.Stats != null)
            {
                _character.Stats.RestoreHP();
                _character.Stats.GetRuntime(StatType.Runtime_XP).Set(0);
            }
        }

        void IRunParticipant.OnRunEnd()
        {
            // Destroy all skills (and their behaviors) when the run ends — symmetric with
            // OnRunBegin's spawn. Disposing the behaviors here also undoes their passive
            // stat bonuses, so the player isn't left inflated on the Win/Lose screen.
            DespawnAllSkills();
            // Zero velocity so the player doesn't keep coasting after EndGame. The
            // controller's per-frame gate (ClassicMode.IsPlaying) also writes zero,
            // but doing it here as well covers the one-frame gap before the next Update.
            var rb = GetComponent<Rigidbody2D>();
            if (rb != null) rb.linearVelocity = Vector2.zero;
        }

        // ─────────────────────────────────────────────────────────────
        // F.G.2 — Death handling
        // ─────────────────────────────────────────────────────────────
        private bool _deathHandled;
        private INumberWithSet _hpForDeath;

        private void SubscribeDeath()
        {
            if (_character == null || _character.Stats == null) return;
            _hpForDeath = _character.Stats.GetRuntime(StatType.Runtime_HP);
            if (_hpForDeath != null) _hpForDeath.Changed += OnHpChangedForDeath;
        }

        private void UnsubscribeDeath()
        {
            if (_hpForDeath != null) _hpForDeath.Changed -= OnHpChangedForDeath;
            _hpForDeath = null;
        }

        private void OnHpChangedForDeath(INumber hp)
        {
            if (_deathHandled) return;
            if (hp.Value > 0) return;
            _deathHandled = true;

            // Freeze player physics so velocity writes are ignored. GameController's
            // own OnHPChange listener will broadcast Data_ClassicEndGame which
            // SV_EndGameBridge consumes to show SV_LoseScreen.
            var rb = GetComponent<Rigidbody2D>();
            if (rb != null) rb.simulated = false;
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

        private ZSkill SpawnSkill(ZSkillConfig cfg, Transform parent)
        {
            ZSkill runtime;
            if (cfg.SkillPrefab != null)
            {
                // Preferred: Instantiate the SO-authored prefab (carries ZSkill + any per-skill
                // visuals/particles as children). No AddComponent ceremony.
                var go = Object.Instantiate(cfg.SkillPrefab, parent, false);
                go.name = $"ZSkill_{cfg.name}";
                runtime = go.GetComponent<ZSkill>();
                if (runtime == null) runtime = go.AddComponent<ZSkill>();
            }
            else
            {
                // Fallback for configs without an authored prefab.
                var go = new GameObject($"ZSkill_{cfg.name}");
                go.transform.SetParent(parent, false);
                runtime = go.AddComponent<ZSkill>();
            }
            runtime.Bind(_character, cfg);
            _skillRuntimes.Add(runtime);
            return runtime;
        }

        /// <summary>Runtime API for the level-up flow: ensure a ZSkill exists for
        /// <paramref name="cfg"/>. Returns the existing runtime if the player already owns the
        /// skill, otherwise spawns a new ZSkill child under the Skills container.</summary>
        public ZSkill AddSkill(ZSkillConfig cfg)
        {
            if (cfg == null || _character == null) return null;
            for (int i = 0; i < _skillRuntimes.Count; i++)
            {
                if (_skillRuntimes[i] != null && _skillRuntimes[i].Config == cfg)
                    return _skillRuntimes[i];
            }
            return SpawnSkill(cfg, ResolveSkillsContainer());
        }

        /// <summary>Find the ZSkill hosting <paramref name="cfg"/>, or null.</summary>
        public ZSkill GetRuntime(ZSkillConfig cfg)
        {
            if (cfg == null) return null;
            for (int i = 0; i < _skillRuntimes.Count; i++)
            {
                if (_skillRuntimes[i] != null && _skillRuntimes[i].Config == cfg)
                    return _skillRuntimes[i];
            }
            return null;
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
    /// if provided. No SkillControllerBehavior — skills live as ZSkill
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
