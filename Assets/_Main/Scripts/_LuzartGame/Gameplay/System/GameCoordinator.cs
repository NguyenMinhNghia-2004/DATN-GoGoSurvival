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
            if (participants != null)
            {
                for (int i = 0; i < participants.Count; i++)
                {
                    try { participants[i].OnRunBegin(); }
                    catch (System.Exception e) { Debug.LogError($"[GameCoordinator] OnRunBegin {participants[i]} : {e}"); }
                }
            }
        }

        public void EndRun()
        {
            var participants = _domain?.GetAll<IRunParticipant>();
            int participantCount = participants?.Count ?? -1;
            // Diagnostic: prior testing showed slice 2 effects (legacy spawner + GunManager
            // stop) didn't reach the runtime. Confirm-or-deny by logging the IRunParticipant
            // count actually visited each EndRun. Remove after the end-game flow is stable.
            Debug.Log($"[GameCoordinator] EndRun fired. IRunParticipant count = {participantCount}");
            if (participants != null)
            {
                for (int i = 0; i < participants.Count; i++)
                {
                    try { participants[i].OnRunEnd(); }
                    catch (System.Exception e) { Debug.LogError($"[GameCoordinator] OnRunEnd {participants[i]} : {e}"); }
                }
            }
            // Belt against IRunParticipant chain failure: if EnemySpawnerManager.OnRunEnd
            // didn't fire (or fires too late), directly destroy every Enemy-tagged GO so the
            // Win/Lose world isn't filled with chasing zombies. Idempotent with the spawner's
            // own loop.
            DestroyAllEnemiesByTag();
            // Drop MapReady so legacy ControllerSpawening flips its Spawner GO inactive →
            // SpawenManager's spawn coroutines stop via OnDisable. LuzartPlayerController
            // also gates on MapReady; this is defense-in-depth alongside slice 1's
            // ClassicMode.IsPlaying gate. legacy.BackFinishSafe normally drops MapReady on
            // Continue press; doing it here moves it forward to EndGame so the world
            // stops spawning new enemies + frozen-state takes effect the instant Win/Lose fires.
            var gc = _domain?.Get<GameController>();
            if (gc != null) gc.MapReady = false;
        }

        /// <summary>Belt-and-suspenders enemy wipe — fallback in case
        /// <c>EnemySpawnerManager.OnRunEnd</c> didn't fire (Domain not seeded, init-order
        /// race, etc.). Destroys every active GameObject tagged "Enemy" so the Win/Lose
        /// screen sits over a still world. Idempotent. Will retire once the IRunParticipant
        /// chain is proven to fire reliably.</summary>
        private static void DestroyAllEnemiesByTag()
        {
            var enemies = GameObject.FindGameObjectsWithTag("Enemy");
            for (int i = 0; i < enemies.Length; i++)
                if (enemies[i] != null) Object.Destroy(enemies[i]);
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
