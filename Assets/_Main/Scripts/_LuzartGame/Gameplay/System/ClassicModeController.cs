namespace Luzart
{
    using UnityEngine;

    /// <summary>Run lifecycle states for Classic mode. Pause is a flag within
    /// <see cref="ClassicModeState.Playing"/>, NOT a separate state.</summary>
    public enum ClassicModeState { Idle, Playing, Ended }

    /// <summary>Why a run ended. Maps to win/lose: only <see cref="WavesCleared"/> is a win.</summary>
    public enum EndReason { PlayerDied, WavesCleared, QuitFromPause }

    /// <summary>
    /// Game-mode state machine for Classic mode — the single entry/exit owner of a run.
    ///
    /// <para>State: <c>Idle → Playing → Ended</c>. Every termination (player death, waves cleared,
    /// quit-from-pause) funnels through the one door <see cref="EndGame"/>, which is guarded so it
    /// fires exactly once. This kills the old per-second <c>OnWinGame</c> re-broadcast and
    /// guarantees the simulation is stopped (and, from S2, enemies despawned) BEFORE the Win/Lose
    /// screen is shown — fixing "win xong vẫn chơi tiếp / quái vẫn đuổi".</para>
    ///
    /// <para>S1 scope: owns state + start/end + stops the wave timer via
    /// <see cref="GameController.StopGameplay"/>. S2 will route Begin/EndRun through
    /// <c>GameCoordinator</c> so participants (spawner, player) start/stop too.</para>
    /// </summary>
    public class ClassicModeController : AbstractMonoBehaviorContent
    {
        private GameController _gameController;
        private GameCoordinator _coordinator;

        public ClassicModeState State { get; private set; } = ClassicModeState.Idle;
        public bool IsPlaying => State == ClassicModeState.Playing;
        public bool IsEnded => State == ClassicModeState.Ended;

        public override void DoInitialize()
        {
            base.DoInitialize();
            _gameController = _domain.Get<GameController>();
            _coordinator = _domain.Get<GameCoordinator>();
        }

        /// <summary>Begin a run (MainMenu Play / Retry). Idempotent while already Playing.
        ///
        /// <para>Post-nuke wiring (replaces deleted <c>DATN.Legacy.UIManager.PlayBtn</c>):
        /// after starting the wave timer + participants, this method also:
        /// (1) spawns the level prefab via <see cref="GameController.SpawnDefaultLevel"/>,
        /// (2) flips <see cref="GameController.MapReady"/> = true (the gate that
        /// <see cref="LuzartPlayerController.Update"/> blocks on), and
        /// (3) activates the legacy "Joystick Table" UI which was saved inactive in scene.
        /// Without (1)(2)(3) the player has no map, can't move, and has no input surface.</para>
        /// </summary>
        public void StartGame()
        {
            Debug.Log($"[DBG-CHAIN] B: StartGame entry, State={State}, gc={(_gameController==null?"null":"OK")}, coord={(_coordinator==null?"null":"OK")}");
            if (State == ClassicModeState.Playing) return;
            State = ClassicModeState.Playing;
            Time.timeScale = 1f;
            EnsureRefs();
            Debug.Log($"[DBG-CHAIN] B: after EnsureRefs, gc={(_gameController==null?"null":"OK")}");
            _gameController?.StartGameplay();
            _coordinator?.BeginRun();

            // (1) Spawn the level prefab once per run. Idempotent guard via static flag —
            // SpawnDefaultLevel itself does not check for an existing instance, and we
            // don't want a second map stacked on top after Retry.
            if (_gameController != null && !_levelSpawned)
            {
                var lvl = _gameController.SpawnDefaultLevel();
                if (lvl != null) _levelSpawned = true;
                Debug.Log($"[DBG-CHAIN] B(1): SpawnDefaultLevel returned {(lvl==null?"NULL":"OK name="+lvl.name)}");
            }
            else Debug.Log($"[DBG-CHAIN] B(1): SKIP SpawnDefaultLevel (gc null={_gameController==null}, alreadySpawned={_levelSpawned})");

            // (2) Flip MapReady — gates LuzartPlayerController.Update + camera spawn + pickups.
            if (_gameController != null) _gameController.MapReady = true;
            Debug.Log($"[DBG-CHAIN] B(2): MapReady now {_gameController?.MapReady}");

            // (3) Re-activate the legacy Joystick Table (saved as inactive in scene; legacy
            // PlayBtn used to SetActive it). Scanning Resources.FindObjectsOfTypeAll so we
            // find inactive GameObjects too. Skip prefab assets via hideFlags + scene check.
            ActivateLegacyJoystickTable();
        }

        private static bool _levelSpawned;

        private static void ActivateLegacyJoystickTable()
        {
            var all = Resources.FindObjectsOfTypeAll<Transform>();
            int found = 0, activated = 0;
            for (int i = 0; i < all.Length; i++)
            {
                var t = all[i];
                if (t == null || t.name != "Joystick Table") continue;
                if (t.hideFlags != HideFlags.None) continue; // skip prefab/editor assets
                if (!t.gameObject.scene.IsValid()) continue; // skip non-scene objects
                found++;
                if (!t.gameObject.activeSelf) { t.gameObject.SetActive(true); activated++; }
                break;
            }
            Debug.Log($"[DBG-CHAIN] B(3): ActivateLegacyJoystickTable found={found} activated={activated}");
        }

        /// <summary>The ONLY way a run ends. Guarded so it runs exactly once while Playing —
        /// stops the wave timer, then broadcasts <see cref="Data_ClassicEndGame"/> for
        /// <c>SV_EndGameBridge</c> to show the Win/Lose screen.</summary>
        public void EndGame(EndReason reason)
        {
            if (State != ClassicModeState.Playing) return; // not running, or already ended
            State = ClassicModeState.Ended;
            EnsureRefs();
            _gameController?.StopGameplay(); // stop wave timer + unsubscribe (no more re-fire)
            _coordinator?.EndRun();          // stop spawner + despawn all enemies
            bool isWin = reason == EndReason.WavesCleared;
            Broadcaster.Broadcast(new Data_ClassicEndGame { IsWin = isWin });
        }

        /// <summary>Return to Idle so the next <see cref="StartGame"/> begins fresh
        /// (called by the reset flow on Continue/Retry).</summary>
        public void ResetToIdle()
        {
            State = ClassicModeState.Idle;
            Time.timeScale = 1f;
        }

        private void EnsureRefs()
        {
            if (_domain == null) return;
            if (_gameController == null) _gameController = _domain.Get<GameController>();
            if (_coordinator == null) _coordinator = _domain.Get<GameCoordinator>();
        }
    }
}
