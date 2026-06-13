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
        [Header("Player runtime spawn (optional)")]
        [Tooltip("Player prefab spawned on run begin and destroyed on run end. Leave NULL to keep " +
                 "using the in-scene Player (no runtime spawn — backward compatible).")]
        [SerializeField] private GameObject _playerPrefab;
        [Tooltip("Where the player spawns each run. If null, the prefab's authored position is used.")]
        [SerializeField] private Transform _playerSpawnPoint;

        // The player instance spawned for the CURRENT run (null between runs / in scene-player mode).
        private GameObject _spawnedPlayer;

        /// <summary>Spawn + register a fresh Player for this run (prefab model). MUST be called
        /// BEFORE <see cref="BeginRun"/> and before GameController.StartGameplay so the new
        /// PlayerCharacter is in the Domain when those subscribe to its HP/XP and fan OnRunBegin.
        ///
        /// <para>No-op when <see cref="_playerPrefab"/> is unassigned — in that case the in-scene
        /// Player (auto-discovered at boot) keeps being used, so this change is backward compatible
        /// until the prefab + scene flip is wired.</para>
        ///
        /// Idempotent: returns the existing instance if a player is already spawned this run.</summary>
        public GameObject SpawnPlayer()
        {
            if (_playerPrefab == null) return null;   // scene-player mode
            if (_spawnedPlayer != null) return _spawnedPlayer;

            Vector3 pos = _playerSpawnPoint != null ? _playerSpawnPoint.position : _playerPrefab.transform.position;
            Quaternion rot = _playerSpawnPoint != null ? _playerSpawnPoint.rotation : _playerPrefab.transform.rotation;
            var go = Object.Instantiate(_playerPrefab, pos, rot);
            go.name = _playerPrefab.name; // keep "Player" (tag + name lookups rely on it)

            // Drive the Content lifecycle (Inject → Initialize → Start). LuzartPlayerEntityRoot
            // .DoInitialize creates the LuzartPlayerCharacter + registers it in the Domain.
            var root = go.GetComponent<LuzartPlayerEntityRoot>();
            var srm = SceneRootManager.Instance;
            if (root != null && srm != null) srm.RegisterContent(root);

            _spawnedPlayer = go;
            return go;
        }

        /// <summary>Tear down + destroy the current run's Player (prefab model). Called on EndRun
        /// AFTER participants' OnRunEnd. No-op in scene-player mode.</summary>
        public void DespawnPlayer()
        {
            if (_spawnedPlayer == null) return;
            var root = _spawnedPlayer.GetComponent<LuzartPlayerEntityRoot>();
            var srm = SceneRootManager.Instance;
            if (root != null && srm != null) srm.UnregisterContent(root);
            Object.Destroy(_spawnedPlayer);
            _spawnedPlayer = null;
        }

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
                // RestoreHP fills to the actual HPMax (config base + equipment), not a
                // hardcoded 100 — the prior magic number ignored HPMax/equipment entirely.
                // (The authoritative per-run player reset lives in
                // LuzartPlayerEntityRoot.OnRunBegin, which also respawns skills; this is the
                // pre-StartGame reset for the Retry/MainMenu flow.)
                player.Stats.RestoreHP();
                player.Stats.GetRuntime(StatType.Runtime_XP).Set(0);
            }
        }
    }
}
