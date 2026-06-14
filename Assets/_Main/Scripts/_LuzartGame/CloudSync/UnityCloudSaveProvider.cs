using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Unity.Services.Authentication;
using Unity.Services.CloudSave;
using Unity.Services.Core;
using UnityEngine;

namespace Luzart
{
    /// <summary>
    /// <see cref="ICloudSaveProvider"/> backed by Unity Gaming Services (UGS) Cloud Save.
    /// Stores the whole player blob under one Cloud Save key, scoped to the anonymously
    /// authenticated UGS player (the UGS identity IS the per-player key, so the playerId argument
    /// from <see cref="CloudSyncService"/> is not needed here).
    ///
    /// <para><b>One unavoidable setup step (not code):</b> Cloud Save needs the project LINKED to a
    /// UGS Project ID. Open <c>Edit ▸ Project Settings ▸ Services</c>, sign in, and link/create a
    /// project (free tier). Until that is done, <c>UnityServices.InitializeAsync()</c> throws
    /// "project ID is missing" — there is no code-only bypass, because the Project ID is the server
    /// anchor Unity uses to identify the app. This provider <b>degrades gracefully</b>: if UGS can't
    /// initialize (unlinked / offline), it transparently uses <see cref="LocalFileCloudProvider"/>
    /// so the game still saves locally and upgrades to cloud the moment the project is linked.</para>
    /// </summary>
    public class UnityCloudSaveProvider : ICloudSaveProvider
    {
        private const string CloudKey = "player_save";

        private readonly LocalFileCloudProvider _fallback = new LocalFileCloudProvider();
        private bool _initAttempted;
        private bool _initOk;

        /// <summary>Initialize UGS + anonymous sign-in once. Returns false (and logs once) if the
        /// project isn't linked / services are unavailable — caller then uses the local fallback.</summary>
        private async UniTask<bool> EnsureInitialized()
        {
            if (_initAttempted) return _initOk;
            _initAttempted = true;
            try
            {
                // Explicit .AsUniTask() — awaiting the raw Task inside a UniTask method can drop the
                // continuation when the Task faults/cancels (UGS init throws when no project linked).
                if (UnityServices.State != ServicesInitializationState.Initialized)
                    await UnityServices.InitializeAsync().AsUniTask();

                if (UnityServices.State == ServicesInitializationState.Initialized)
                {
                    if (!AuthenticationService.Instance.IsSignedIn)
                        await AuthenticationService.Instance.SignInAnonymouslyAsync().AsUniTask();
                    _initOk = AuthenticationService.Instance.IsSignedIn;
                }
            }
            catch (System.Exception e)
            {
                _initOk = false;
                Debug.LogWarning($"[CloudSync/UGS] init exception: {e.GetType().Name}: {e.Message}");
            }

            if (_initOk)
                Debug.Log($"[CloudSync/UGS] cloud ACTIVE (PlayerId={AuthenticationService.Instance.PlayerId}).");
            else
                Debug.LogWarning("[CloudSync/UGS] cloud NOT active → using LOCAL save fallback. " +
                    "To enable Unity Cloud Save: Edit ▸ Project Settings ▸ Services → link a UGS project (free). " +
                    "No code change needed once linked.");
            return _initOk;
        }

        public async UniTask<bool> SaveAsync(string playerId, string json)
        {
            if (await EnsureInitialized())
            {
                try
                {
                    var data = new Dictionary<string, object> { { CloudKey, json } };
                    await CloudSaveService.Instance.Data.Player.SaveAsync(data);
                    return true;
                }
                catch (System.Exception e)
                {
                    Debug.LogWarning($"[CloudSync/UGS] save failed: {e.Message} — using local fallback.");
                }
            }
            return await _fallback.SaveAsync(playerId, json);
        }

        public async UniTask<string> LoadAsync(string playerId)
        {
            if (await EnsureInitialized())
            {
                try
                {
                    var result = await CloudSaveService.Instance.Data.Player.LoadAsync(
                        new HashSet<string> { CloudKey });
                    if (result != null && result.TryGetValue(CloudKey, out var item) && item != null)
                    {
                        string json = item.Value.GetAsString();
                        if (!string.IsNullOrEmpty(json)) return json;
                    }
                    // Signed in but cloud is empty (first cloud run): migrate any existing local
                    // save up — it will be pushed to the cloud on the next save.
                    return await _fallback.LoadAsync(playerId);
                }
                catch (System.Exception e)
                {
                    Debug.LogWarning($"[CloudSync/UGS] load failed: {e.Message} — using local fallback.");
                }
            }
            return await _fallback.LoadAsync(playerId);
        }
    }
}
