using Cysharp.Threading.Tasks;

namespace Luzart
{
    /// <summary>
    /// Backend abstraction for player-data sync. This is the ONLY seam that changes when you switch
    /// backends: write one implementation per backend (own REST server, Firebase, PlayFab, Unity
    /// Cloud Save, …) — the rest of the game (the central <see cref="PlayerDataSerializer"/> blob and
    /// <see cref="CloudSyncService"/> orchestration) stays identical. "Đổi SDK thôi."
    ///
    /// <para>The payload is an opaque JSON string produced/consumed by
    /// <see cref="PlayerDataSerializer"/>. The provider only stores and retrieves it, keyed by a
    /// stable per-player id.</para>
    /// </summary>
    public interface ICloudSaveProvider
    {
        /// <summary>Persist <paramref name="json"/> for <paramref name="playerId"/>. Returns true on success.</summary>
        UniTask<bool> SaveAsync(string playerId, string json);

        /// <summary>Fetch the stored JSON for <paramref name="playerId"/>, or null/empty if none exists.</summary>
        UniTask<string> LoadAsync(string playerId);
    }
}
