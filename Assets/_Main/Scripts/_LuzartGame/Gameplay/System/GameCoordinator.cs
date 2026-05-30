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
    }
}
