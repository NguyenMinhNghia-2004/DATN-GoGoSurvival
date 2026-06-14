using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Luzart
{
    /// <summary>
    /// Orchestrates player-data sync. On game enter it PULLs the player's blob from the configured
    /// <see cref="ICloudSaveProvider"/> and applies it onto the Domain saveables; on run end / app
    /// background / quit it PUSHes the current Domain state back.
    ///
    /// <para>Auto-discovered by SceneRootManager (it is an <see cref="AbstractMonoBehaviorContent"/>).
    /// <see cref="DoStart"/> runs after all saveable content is registered + after ItemConfigsOwned's
    /// boot-time random seeding, so the pulled state authoritatively overrides defaults.</para>
    ///
    /// <para>Swapping backend = pick a different <see cref="ProviderKind"/> (or add a case in
    /// <see cref="CreateProvider"/> for a Firebase/PlayFab provider). Nothing else in the game changes.</para>
    /// </summary>
    public class CloudSyncService : AbstractMonoBehaviorContent
    {
        public enum ProviderKind { LocalFile, Rest, UnityCloudSave }

        [Header("Backend (swap by changing this — only the provider class differs)")]
        [SerializeField] private ProviderKind _provider = ProviderKind.UnityCloudSave;
        [Tooltip("Base URL for the Rest provider, e.g. https://your-server.com/api")]
        [SerializeField] private string _restBaseUrl = "https://your-server.example.com/api";

        private const string PlayerIdKey = "cloud_player_id";

        private ICloudSaveProvider _cloud;
        private string _playerId;
        private bool _loaded;

        public bool IsLoaded => _loaded;
        public string PlayerId => _playerId;

        public override void DoStart()
        {
            base.DoStart();
            _cloud = CreateProvider();
            _playerId = ResolvePlayerId();
            Debug.Log($"[CloudSync] provider={_provider} → {(_cloud != null ? _cloud.GetType().Name : "null")}");
            PullAndApply().Forget();
        }

        public override void DoStop()
        {
            base.DoStop();
            // Best-effort push on teardown (editor stop / scene unload).
            if (_loaded) PushNow().Forget();
        }

        private ICloudSaveProvider CreateProvider()
        {
            switch (_provider)
            {
                case ProviderKind.UnityCloudSave: return new UnityCloudSaveProvider();
                case ProviderKind.Rest: return new RestCloudProvider(_restBaseUrl);
                // case ProviderKind.Firebase: return new FirebaseCloudProvider();  // add your SDK provider here
                default: return new LocalFileCloudProvider();
            }
        }

        private static string ResolvePlayerId()
        {
            var id = PlayerPrefs.GetString(PlayerIdKey, string.Empty);
            if (string.IsNullOrEmpty(id))
            {
                id = System.Guid.NewGuid().ToString("N");
                PlayerPrefs.SetString(PlayerIdKey, id);
                PlayerPrefs.Save();
            }
            return id;
        }

        /// <summary>Pull the cloud blob and apply it to the Domain (game-enter restore).</summary>
        private async UniTaskVoid PullAndApply()
        {
            string json = null;
            try { json = await _cloud.LoadAsync(_playerId); }
            catch (System.Exception e) { Debug.LogWarning($"[CloudSync] pull failed: {e.Message}"); }

            if (!string.IsNullOrEmpty(json))
            {
                int n = PlayerDataSerializer.ApplyToDomain(_domain, json);
                Debug.Log($"[CloudSync] pulled + applied {n} content(s) for player {_playerId}.");
            }
            else
            {
                Debug.Log($"[CloudSync] no cloud data for player {_playerId} (first run / empty).");
            }
            _loaded = true;
        }

        /// <summary>Serialize the whole Domain and push it to the backend. Safe to call any time.</summary>
        public async UniTask<bool> PushNow()
        {
            if (_cloud == null || _domain == null) return false;
            string json = PlayerDataSerializer.SerializeDomain(_domain);
            bool ok = false;
            try { ok = await _cloud.SaveAsync(_playerId, json); }
            catch (System.Exception e) { Debug.LogWarning($"[CloudSync] push failed: {e.Message}"); }
            if (ok) Debug.Log($"[CloudSync] pushed data for player {_playerId}.");
            return ok;
        }

        private void OnApplicationPause(bool paused)
        {
            if (paused && _loaded) PushNow().Forget(); // mobile background — most reliable push point
        }

        private void OnApplicationQuit()
        {
            if (_loaded) PushNow().Forget(); // best-effort (async may not finish on hard quit)
        }
    }
}
