namespace Luzart
{
    using System.Collections.Generic;
    using UnityEngine;

    /// <summary>
    /// Owns the lifecycle of the gameplay components for a Classic-mode run. Driven by
    /// <see cref="ClassicModeController"/>: <see cref="BeginRun"/> on StartGame,
    /// <see cref="EndRun"/> on EndGame. It simply fans the call out to every
    /// <see cref="IRunParticipant"/> registered in the Domain (enemy spawner, player, …) — so
    /// stopping a run reliably stops every component, instead of leaving the spawner spawning and
    /// enemies chasing under the Win/Lose screen.
    /// </summary>
    public class GameCoordinator : AbstractMonoBehaviorContent
    {
        public void BeginRun()
        {
            var participants = _domain?.GetAll<IRunParticipant>();
            if (participants == null) return;
            for (int i = 0; i < participants.Count; i++)
            {
                try { participants[i].OnRunBegin(); }
                catch (System.Exception e) { Debug.LogError($"[GameCoordinator] OnRunBegin {participants[i]} : {e}"); }
            }
        }

        public void EndRun()
        {
            var participants = _domain?.GetAll<IRunParticipant>();
            if (participants == null) return;
            for (int i = 0; i < participants.Count; i++)
            {
                try { participants[i].OnRunEnd(); }
                catch (System.Exception e) { Debug.LogError($"[GameCoordinator] OnRunEnd {participants[i]} : {e}"); }
            }
        }

        /// <summary>Reset the framework state (counters, mode→Idle) + player stats so the next
        /// run starts fresh. The legacy-layer teardown (level/UI) stays in
        /// <c>GameplayResetCoordinator</c> which calls this.</summary>
        public void ResetRun()
        {
            if (_domain == null) return;
            _domain.Get<GameController>()?.ResetState();
            _domain.Get<ClassicModeController>()?.ResetToIdle();

            var player = _domain.Get<PlayerCharacter>();
            if (player?.Stats != null)
            {
                player.Stats.GetRuntime(StatType.Runtime_HP).Set(100);
                player.Stats.GetRuntime(StatType.Runtime_XP).Set(0);
            }
        }
    }
}
