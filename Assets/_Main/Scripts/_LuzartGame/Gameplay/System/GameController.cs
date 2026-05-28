using Cysharp.Threading.Tasks;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
namespace Luzart
{
    public class GameController : AbstractMonoBehaviorContent
    {
        private IVariable<int> _indexWave = new Variable<int>(0);
        private IVariable<int> _countTime = new Variable<int>(0);
        private IVariable<int> _currentLevel = new Variable<int>(0);
        private INumberWithSet _countEnemyDead = new Number(0);
        public IVariable<int> CurrentLevel => _currentLevel;
        public IVariable<int> IndexWave => _indexWave;
        public IVariable<int> CountTime => _countTime;
        public INumberWithSet CountEnemyDead => _countEnemyDead;
        /// <summary>True after the playfield is spawned and gameplay has begun.
        /// Migrated from legacy DATN.Legacy.UIManager.MapReady (Phase D.6).
        /// Readers: ControllerSpawening (camera spawn gating), Diamond/DiamondVip
        /// (pickup gating), LocalisationPresent (level localisation gating).</summary>
        public bool MapReady { get; set; }
    
        // Inject
        private LevelConfig _levelConfig;
        private EnemySpawnerManager _enemyManager;
        private PlayerCharacter _playerCharacter;
        private UpgradeSkillManager _upgradeSkillManager;
        // Cache
        private DataWave _dataWaveCurrent;
        private EnemyWaveConfig _enemyWaveConfig;
        // Timer cancellation token
        private CancellationTokenSource _timerCancellationToken;
        private INumberWithSet XP => _playerCharacter.Stats.GetRuntime(StatType.Runtime_XP);
        private INumberWithSet HP => _playerCharacter.Stats.GetRuntime(StatType.Runtime_HP);
        public override void DoInitialize()
        {
            base.DoInitialize();
            _levelConfig = _domain.Get<LevelConfig>();
            _enemyManager = _domain.Get<EnemySpawnerManager>();
            _playerCharacter = _domain.Get<PlayerCharacter>();
            _upgradeSkillManager = _domain.Get<UpgradeSkillManager>();
            _indexWave.Set(0);
            _countTime.Set(0);
            _currentLevel.Set(0);
            _timerCancellationToken = new CancellationTokenSource();
        }
        public override void DoStart()
        {
            base.DoStart();
            // Defer real gameplay until SV_MainMenuUI.OnPlay calls StartGameplay().
            // Otherwise framework would tick waves while user is still on MainMenu.
        }

        /// <summary>Public entry point — call from UI Play button (or DATN.Legacy.UIManager.PlayBtn).</summary>
        public void StartGameplay()
        {
            if (_gameplayActive) return;
            _gameplayActive = true;
            SpawnNewWave(IndexWave);
            IndexWave.Changed += SpawnNewWave;
            XP.Changed += OnXPChange;
            HP.Changed += OnHPChange;
            _timerCoroutine = StartCoroutine(IECountPerSecond());
        }

        public void StopGameplay()
        {
            if (!_gameplayActive) return;
            _gameplayActive = false;
            IndexWave.Changed -= SpawnNewWave;
            XP.Changed -= OnXPChange;
            HP.Changed -= OnHPChange;
            if (_timerCoroutine != null) StopCoroutine(_timerCoroutine);
        }

        /// <summary>Reset wave/timer/level/kill counters so the next StartGameplay starts fresh.
        /// Used by Win/Lose → Continue/Retry/MainMenu flows.</summary>
        public void ResetState()
        {
            StopGameplay();
            _indexWave.Set(0);
            _countTime.Set(0);
            _currentLevel.Set(0);
            _countEnemyDead.Set(0);
        }

        private bool _gameplayActive;
        private Coroutine _timerCoroutine;
        private void SpawnNewWave(IValue<int> indexWave)
        {
            Debug.Log("[GameController] Call In Here 1");
            _dataWaveCurrent = _levelConfig.EnemyWaves[indexWave.Value];
            List<EnemyWaveConfig> listEnemyWaveConfig = _dataWaveCurrent.GetListWaveEnemy(indexWave.Value);
            StartCoroutine(_enemyManager.StartSpawning(listEnemyWaveConfig));
        }
        public override void DoStop()
        {
            base.DoStop();
            StopGameplay();
        }
        public override void DoTerminate()
        {
            base.DoTerminate();
        }
        private IEnumerator IECountPerSecond()
        {
            var wait = new WaitForSeconds(1);
            while (true)
            {
                yield return wait;
                OnTimerTick();
            }
        }
        private void OnTimerTick()
        {
            _countTime.Set(_countTime.Value + 1);
            int waveNowOnChange = ListExtensions.FindLowerBoundIndex(_levelConfig.TimeWaves, _countTime.Value);
            if(_indexWave.Value < waveNowOnChange)
            {
                if (waveNowOnChange >= _levelConfig.TimeWaves.Count)
                {
                    OnWinGame();
                    return;
                }
                _indexWave.Set(waveNowOnChange);
            }
        }
        private void OnWinGame()
        {
            Broadcaster.Broadcast<Data_ClassicEndGame>(new Data_ClassicEndGame()
            {
                IsWin = true
            });
        }
        private void OnLoseGame()
        {
            Broadcaster.Broadcast<Data_ClassicEndGame>(new Data_ClassicEndGame()
            {
                IsWin = false
            });
        }
        public void OnXPChange(INumber XP)
        {
            double valueXP = XP.Value;
            int levelNowOnChange = ListExtensions.FindLowerBoundIndex(_levelConfig.ListXPRequirePerLevel, valueXP);
            int currentValue = _currentLevel.Value;
            if (currentValue < levelNowOnChange)
            {
                for (int i = currentValue + 1; i <= levelNowOnChange; i++)
                {
                    int value = i;
                    _currentLevel.Set(value);
                }
            }
        }
        public void OnHPChange(INumber HP)
        {
            if (HP.Value <= 0)
            {
                OnLoseGame();
            }
        }
        public void AddEnemyDead(int add)
        {
            _countEnemyDead.Set(_countEnemyDead.Value + add);
        }

        /// <summary>Instantiate the default level prefab from LevelCatalog (Phase D).</summary>
        public GameObject SpawnDefaultLevel(Transform parent = null)
        {
            var catalog = _domain != null ? _domain.Get<LevelCatalog>() : null;
            if (catalog == null || catalog.DefaultLevelPrefab == null)
            {
                Debug.LogWarning("[GameController] SpawnDefaultLevel: LevelCatalog or DefaultLevelPrefab missing.");
                return null;
            }
            var prefab = catalog.DefaultLevelPrefab;
            var inst = Object.Instantiate(prefab, prefab.transform.position, prefab.transform.rotation);
            if (parent != null) inst.transform.SetParent(parent);
            return inst;
        }

    }
}