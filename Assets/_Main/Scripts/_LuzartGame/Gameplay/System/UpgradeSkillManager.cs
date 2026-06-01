namespace Luzart
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Cysharp.Threading.Tasks;
    using UnityEngine;
    public class UpgradeSkillManager : AbstractMonoBehaviorContent
    {
        private int _maxSkillCanRolInSectionUpgrade = 3;
        private LevelConfig _levelConfig;
        private PlayerCharacter _playerCharacter;
        // Phase-F.G skill model: the player has NO SkillControllerBehavior. Skills live as
        // ZSkillRuntime children spawned by LuzartPlayerEntityRoot (the §3.6 GameObject-child
        // deviation). This manager resolves available upgrades from the player's ZSkillRuntime
        // children + the LevelConfig pool, and applies a pick by upgrading an existing runtime
        // or spawning a new one — instead of going through a SkillControllerBehavior.
        private LuzartPlayerEntityRoot _playerEntityRoot;
        private GameController _gameController;
        private StatsBehavior _statsBehavior;
        private Queue<Action> _queueProcess = new Queue<Action>();
        public override void DoInitialize()
        {
            base.DoInitialize();
            _levelConfig = _domain.Get<LevelConfig>();
            _playerCharacter = _domain.Get<PlayerCharacter>();
            _gameController = _domain.Get<GameController>();
            _playerEntityRoot = _domain.Get<LuzartPlayerEntityRoot>();
            if (_playerCharacter != null)
            {
                _statsBehavior = _playerCharacter.GetBehavior<StatsBehavior>();
            }
            if (_statsBehavior != null)
            {
                _maxSkillCanRolInSectionUpgrade = (int)_statsBehavior.Get(StatType.TotalSkill).Value;
            }
            if (_maxSkillCanRolInSectionUpgrade <= 0) _maxSkillCanRolInSectionUpgrade = 3;
            if (_gameController != null) _gameController.CurrentLevel.Changed += UpgradeLevel;
            _queueProcess = new Queue<Action>();
            Broadcaster.Register<SkillUpgradeSuccessBroadcastData>(OnSkillUpgradeSuccessBroadcast);
            _isUpgrading = false;
        }
        public override void DoTerminate()
        {
            base.DoTerminate();
            Broadcaster.Unregister<SkillUpgradeSuccessBroadcastData>(OnSkillUpgradeSuccessBroadcast);
            _queueProcess.Clear();
        }
        private void UpgradeLevel(IValue<int> obj)
        {
            int level = obj.Value;
            Debug.Log($"[UpgradeSkillManager] Level Changed → {level} (queueLen={_queueProcess.Count}, isUpgrading={_isUpgrading})");
            Action action = () => UpgradeLevel(level);
            _queueProcess.Enqueue(action);
            if (!_isUpgrading)
            {
                _queueProcess.Dequeue()?.Invoke();
            }
        }
        private bool _isUpgrading = false;
        public void UpgradeLevel(int levelCurrent)
        {
            // Level 0 = initial state from GameController.DoInitialize. Skip — no real level-up.
            if (levelCurrent <= 0) { _isUpgrading = false; return; }

            EnsurePlayerEntityRootResolved();
            if (_playerEntityRoot == null || _levelConfig == null)
            {
                Debug.LogWarning("[UpgradeSkillManager] Missing deps (player entity root / level config) — skipping UpgradeLevel.");
                _isUpgrading = false;
                return;
            }
            var availableUpgrades = FindAvailableUpgrades();
            List<Data_UpgradeSkill> data_UpgradeSkills = new List<Data_UpgradeSkill>();
            for (int i = 0; i < availableUpgrades.Count; i++)
            {
                data_UpgradeSkills.Add(new Data_UpgradeSkill()
                {
                    SkillConfig = availableUpgrades[i],
                    LevelIndex = GetSkillLevel(availableUpgrades[i])
                });
            }
            var shuffledUpgrades = data_UpgradeSkills.GetShuffle(_maxSkillCanRolInSectionUpgrade);
            if (shuffledUpgrades == null || shuffledUpgrades.Count == 0)
            {
                _isUpgrading = false;
                if (_queueProcess.Count > 0) _queueProcess.Dequeue()?.Invoke();
                return;
            }
            _isUpgrading = true;
            // Phase F fix: do NOT set Time.timeScale here. SV_LevelUpPopupUI.OnBeforeShowAsync
            // owns the pause; setting it twice from independent code paths caused a freeze race.

            // NinjaUI: show level-up popup with the rolled options. When the player picks, fire
            // SkillUpgradeSuccessBroadcastData which OnSkillUpgradeSuccessBroadcast consumes.
            if (UIManager.Instance != null)
            {
                var levelUpData = new SV_LevelUpData
                {
                    Options = shuffledUpgrades,
                    OnPicked = picked =>
                    {
                        Broadcaster.Broadcast(new SkillUpgradeSuccessBroadcastData(picked.SkillConfig, picked.LevelIndex));
                    }
                };
                ShowLevelUpPopupSafe(levelUpData, shuffledUpgrades).Forget();
            }
            else
            {
                Debug.LogWarning("[UpgradeSkillManager] UIManager.Instance is null — level-up popup skipped. Auto-pick first option as fallback.");
                if (shuffledUpgrades.Count > 0)
                {
                    Broadcaster.Broadcast(new SkillUpgradeSuccessBroadcastData(
                        shuffledUpgrades[0].SkillConfig, shuffledUpgrades[0].LevelIndex));
                }
            }
        }
        /// <summary>Awaited ShowAsync with fallback so a transient popup failure (pool race,
        /// registry mismatch) doesn't leave _isUpgrading stuck at true forever.</summary>
        private async UniTaskVoid ShowLevelUpPopupSafe(SV_LevelUpData data, List<Data_UpgradeSkill> fallbackOptions)
        {
            try
            {
                await UIManager.Instance.ShowAsync(UIId.SV_LevelUpPopup, new UIContext(data));
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[UpgradeSkillManager] ShowAsync threw: {e.GetType().Name}: {e.Message}. Auto-picking first option to keep queue unstuck.");
                if (fallbackOptions != null && fallbackOptions.Count > 0)
                {
                    Broadcaster.Broadcast(new SkillUpgradeSuccessBroadcastData(
                        fallbackOptions[0].SkillConfig, fallbackOptions[0].LevelIndex));
                }
                else
                {
                    _isUpgrading = false;
                    if (_queueProcess.Count > 0) _queueProcess.Dequeue()?.Invoke();
                }
            }
        }

        private void OnSkillUpgradeSuccessBroadcast(SkillUpgradeSuccessBroadcastData data)
        {
            Time.timeScale = 1;
            EnsurePlayerEntityRootResolved();
            ApplyUpgrade(data.SkillConfig);
            _isUpgrading = false;
            // Phase F fix: defer dequeue 2 frames so the popup's hide animation + UIManager
            // teardown complete before the next ShowAsync, otherwise NinjaUI drops the 2nd show.
            if (_queueProcess.Count > 0) DequeueNextAfterHideAsync().Forget();
        }

        /// <summary>Apply a picked skill in the ZSkillRuntime model: if the player already owns a
        /// runtime for this config, bump its level (re-rolls the behaviors' upgrade numbers);
        /// otherwise spawn a new ZSkillRuntime child for it.</summary>
        private void ApplyUpgrade(ZSkillConfig config)
        {
            if (config == null || _playerEntityRoot == null) return;
            var runtime = _playerEntityRoot.GetRuntime(config);
            if (runtime != null)
            {
                ((IZSkill)runtime).UpgradeSkill();
            }
            else
            {
                _playerEntityRoot.AddSkill(config);
            }
        }

        private async UniTaskVoid DequeueNextAfterHideAsync()
        {
            await UniTask.DelayFrame(2);
            if (_queueProcess.Count > 0)
            {
                _queueProcess.Dequeue()?.Invoke();
            }
        }
        /// <summary>Skills offered at this level: the LevelConfig pool minus skills the player
        /// already owns AND can no longer upgrade (maxed). Result = new skills + owned-still-
        /// upgradable skills.</summary>
        private List<ZSkillConfig> FindAvailableUpgrades()
        {
            EnsurePlayerEntityRootResolved();
            if (_levelConfig == null || _playerEntityRoot == null)
                return new List<ZSkillConfig>();
            var pool = _levelConfig.GetAllSkillInLevel() ?? new List<ZSkillConfig>();
            var ownedMaxed = new List<ZSkillConfig>();
            var runtimes = _playerEntityRoot.SkillRuntimes;
            if (runtimes != null)
            {
                foreach (var rt in runtimes)
                {
                    if (rt == null || rt.Config == null) continue;
                    bool canUpgrade = ((IZSkill)rt).IsUpgradeSkill();
                    if (!canUpgrade) ownedMaxed.Add(rt.Config);
                }
            }
            pool.RemoveAll(ownedMaxed.Contains);
            return pool;
        }

        /// <summary>Current level index of the skill hosting <paramref name="config"/>, or 0 if
        /// the player doesn't own it yet.</summary>
        private int GetSkillLevel(ZSkillConfig config)
        {
            var rt = _playerEntityRoot != null ? _playerEntityRoot.GetRuntime(config) : null;
            return rt != null ? (int)((IZSkill)rt).LevelIndex.Value : 0;
        }

        private void EnsurePlayerEntityRootResolved()
        {
            if (_playerEntityRoot == null && _domain != null)
                _playerEntityRoot = _domain.Get<LuzartPlayerEntityRoot>();
        }
    }
    [System.Serializable]
    public class Data_UpgradeSkill
    {
        public ZSkillConfig SkillConfig;
        public int LevelIndex;
    }
}
