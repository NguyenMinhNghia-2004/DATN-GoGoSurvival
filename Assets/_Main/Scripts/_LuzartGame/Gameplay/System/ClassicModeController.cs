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

        /// <summary>Begin a run (MainMenu Play / Retry). Idempotent while already Playing.</summary>
        public void StartGame()
        {
            if (State == ClassicModeState.Playing) return;
            State = ClassicModeState.Playing;
            Time.timeScale = 1f;
            EnsureRefs();
            _gameController?.StartGameplay();
            _coordinator?.BeginRun();
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
