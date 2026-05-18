using DATN.Legacy;
using UnityEngine;

namespace Luzart
{
    /// <summary>
    /// Two-way sync between DATN's legacy gameplay state and the framework's IContent / IEntity
    /// systems. Without this bridge, the two run side-by-side without communicating:
    ///
    ///   - DATN's <c>Manager.Health</c> drops on hit, but <c>PlayerCharacter.Stats.Runtime_HP</c>
    ///     stays at zero, so framework XP/level-up never trigger.
    ///   - Framework's <c>EnemySpawnerManager</c> spawns enemies, but their deaths are not
    ///     credited to <c>GameController.CountEnemyDead</c> or to the player's XP.
    ///
    /// This MonoBehaviour:
    ///   1. Each frame: mirror <c>GameManager.Health</c> → <c>Stats.Runtime_HP</c>
    ///      so framework code reads the live HP.
    ///   2. Each frame: mirror <c>GameManager.CurrentKilled</c> → grant XP per kill increment.
    ///      The XP→level→upgrade pipeline is handled downstream by
    ///      <c>GameController.OnXPChange → CurrentLevel.Changed → UpgradeSkillManager.UpgradeLevel</c>.
    ///
    /// Attach to `_GameBoot` (or any persistent GameObject) — auto-discovers all refs.
    /// </summary>
    public class DATNGameplayBridge : MonoBehaviour
    {
        [Header("XP grant per enemy killed")]
        [SerializeField] private int xpPerKill = 10;

        // Discovered refs
        private GameManager _datnGameManager;
        private PlayerCharacter _player;
        private GameController _gameController;

        // State trackers
        private int _lastSyncedKillCount;
        private bool _hooked;

        private void Start()
        {
            _datnGameManager = FindObjectOfType<GameManager>();

            // Resolve framework side via Domain
            if (SceneRootManager.Instance != null && SceneRootManager.Instance.Domain != null)
            {
                var d = SceneRootManager.Instance.Domain;
                _player = d.Get<PlayerCharacter>();
                _gameController = d.Get<GameController>();
            }

            if (_player != null && _player.Stats != null)
            {
                // Initialize HP runtime to 100 (matching DATN) — Stats.Runtime_HP is from the dict baked in StatsBehavior.
                _player.Stats.GetRuntime(StatType.Runtime_HP).Set(100);
                _player.Stats.GetRuntime(StatType.Runtime_XP).Set(0);
                _hooked = true;
            }
            // Note: XP→level pipeline is handled by GameController.OnXPChange (subscribed in StartGameplay)
            // → drives CurrentLevel.Set → UpgradeSkillManager.UpgradeLevel.
            // No duplicate subscription needed here.

            if (_datnGameManager != null) _lastSyncedKillCount = _datnGameManager.CurrentKilled;
        }

        private void Update()
        {
            if (!_hooked) return;

            // -- HP sync: DATN → framework --
            if (_datnGameManager != null && _player?.Stats != null)
            {
                var hpRT = _player.Stats.GetRuntime(StatType.Runtime_HP);
                if ((float)hpRT.Value != _datnGameManager.Health)
                    hpRT.Set(_datnGameManager.Health);
            }

            // -- Kills delta → kill counter only --
            // XP is NOT granted per kill (Survivor.io spec: kill → drop → walk over → XP).
            // The Diamond pickup in Diamond.OnTriggerEnter2D handles framework Stats.AddXP.
            // We still need to mirror the kill count so GameController.CountEnemyDead and the
            // HUD kill counter stay in sync with legacy CurrentKilled.
            if (_datnGameManager != null)
            {
                int killed = _datnGameManager.CurrentKilled;
                int delta = killed - _lastSyncedKillCount;
                if (delta > 0)
                {
                    _lastSyncedKillCount = killed;
                    _gameController?.AddEnemyDead(delta);
                }
                else if (delta < 0)
                {
                    // Legacy reset CurrentKilled to 0 (Retry / BackFinishSafe path).
                    _lastSyncedKillCount = killed;
                }
            }
        }

        private void OnDestroy()
        {
            // XP.Changed is owned by GameController (subscribed in StartGameplay / unsubscribed in StopGameplay).
        }
    }
}
