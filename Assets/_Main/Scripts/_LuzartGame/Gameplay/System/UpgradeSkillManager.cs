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
        private SkillControllerBehavior _skillControllerBehavior;
        private GameController _gameController;
        private StatsBehavior _statsBehavior;
        private Queue<Action> _queueProcess = new Queue<Action>();
        public override void DoInitialize()
        {
            base.DoInitialize();
            _levelConfig = _domain.Get<LevelConfig>();
            _playerCharacter = _domain.Get<PlayerCharacter>();
            _gameController = _domain.Get<GameController>();
            if (_playerCharacter != null)
            {
                _skillControllerBehavior = _playerCharacter.GetBehavior<SkillControllerBehavior>();
                _statsBehavior = _playerCharacter.GetBehavior<StatsBehavior>();
            }
            if (_statsBehavior != null)
            {
                _maxSkillCanRolInSectionUpgrade = (int)_statsBehavior.Get(StatType.TotalSkill).Value;
            }
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

            EnsureSkillControllerResolved();
            if (_skillControllerBehavior == null || _levelConfig == null)
            {
                Debug.LogWarning("[UpgradeSkillManager] Missing deps — skipping UpgradeLevel.");
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
                    LevelIndex = _skillControllerBehavior.GetSkillLevel(availableUpgrades[i])
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
            // owns the pause; setting it twice from independent code paths (UpgradeLevel +
            // OnBeforeShow) caused a race when multiple level-ups queued — broadcast fired
            // timeScale=1 then dequeued next UpgradeLevel which set timeScale=0 before the
            // popup hide animation finished, leaving the game frozen with no visible popup.

            // NinjaUI: show level-up popup with the 3 rolled options.
            // When player picks, fire SkillUpgradeSuccessBroadcastData which OnSkillUpgradeSuccessBroadcast
            // (already subscribed) consumes to apply the upgrade.
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
                // Phase F: do the ShowAsync as an awaited task with a try/catch. If it
                // throws (e.g. pool race when previous popup hasn't finished hiding),
                // reset _isUpgrading + dequeue next so the queue doesn't deadlock.
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
        /// <summary>Awaited ShowAsync with fallback so a transient popup failure (pool
        /// race, registry mismatch) doesn't leave _isUpgrading stuck at true forever.
        /// Phase F bug fix: previously .Forget() swallowed exceptions silently, leaving
        /// the queue deadlocked.</summary>
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
                    // No options — just unblock the queue.
                    _isUpgrading = false;
                    if (_queueProcess.Count > 0) _queueProcess.Dequeue()?.Invoke();
                }
            }
        }

        private void OnSkillUpgradeSuccessBroadcast(SkillUpgradeSuccessBroadcastData data)
        {
            Time.timeScale = 1;
            EnsureSkillControllerResolved();
            _skillControllerBehavior?.UpgradeSkill(data.SkillConfig);
            _isUpgrading = false;
            if (_queueProcess.Count > 0)
            {
                _queueProcess.Dequeue()?.Invoke();
            }
        }
        private List<ZSkillConfig> FindAvailableUpgrades()
        {
            EnsureSkillControllerResolved();
            if (_levelConfig == null || _skillControllerBehavior == null)
                return new List<ZSkillConfig>();
            var totalSkillConfigsCanUpgrade = _levelConfig.GetAllSkillInLevel() ?? new List<ZSkillConfig>();
            var allConfigs = _skillControllerBehavior.GetAllZSkillConfigs();
            var currentConfigs = (allConfigs != null ? allConfigs.ToList() : new List<ZSkillConfig>());
            var currentSkill = _skillControllerBehavior.GetAllZSkill();
            if (currentSkill != null)
            {
                foreach (var skill in currentSkill)
                {
                    if (skill == null) continue;
                    if (skill.IsUpgradeSkill())
                    {
                        currentConfigs.Remove(skill.Config);
                    }
                }
            }
            totalSkillConfigsCanUpgrade.RemoveAll(currentConfigs.Contains);
            return totalSkillConfigsCanUpgrade;
        }

        private void EnsureSkillControllerResolved()
        {
            if (_skillControllerBehavior != null) return;
            if (_playerCharacter == null && _domain != null)
                _playerCharacter = _domain.Get<PlayerCharacter>();
            if (_playerCharacter != null)
                _skillControllerBehavior = _playerCharacter.GetBehavior<SkillControllerBehavior>();
        }
    }
    [System.Serializable]
    public class Data_UpgradeSkill
    {
        public ZSkillConfig SkillConfig;
        public int LevelIndex;
    }
}