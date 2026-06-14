using System.Text;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

namespace Luzart
{
    /// <summary>
    /// Example <see cref="ICloudSaveProvider"/> for your OWN backend over plain HTTP (UnityWebRequest).
    /// Wire-format contract (implement these two on your server):
    /// <list type="bullet">
    /// <item><c>GET  {baseUrl}/save/{playerId}</c> → 200 with the JSON blob body (or 404/empty if none)</item>
    /// <item><c>POST {baseUrl}/save/{playerId}</c> with the JSON blob as the request body → 200 on success</item>
    /// </list>
    ///
    /// <para>To use Firebase / PlayFab / Unity Cloud Save instead, write a sibling class implementing
    /// <see cref="ICloudSaveProvider"/> with that SDK and select it in <see cref="CloudSyncService"/>.
    /// The blob format and the rest of the game are unchanged.</para>
    /// </summary>
    public class RestCloudProvider : ICloudSaveProvider
    {
        private readonly string _baseUrl;

        public RestCloudProvider(string baseUrl)
        {
            _baseUrl = string.IsNullOrEmpty(baseUrl) ? string.Empty : baseUrl.TrimEnd('/');
        }

        private string Url(string playerId) => $"{_baseUrl}/save/{UnityWebRequest.EscapeURL(playerId)}";

        public async UniTask<bool> SaveAsync(string playerId, string json)
        {
            if (string.IsNullOrEmpty(_baseUrl)) { Debug.LogWarning("[CloudSync/REST] base URL not set."); return false; }
            using var req = new UnityWebRequest(Url(playerId), UnityWebRequest.kHttpVerbPOST)
            {
                uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(json)),
                downloadHandler = new DownloadHandlerBuffer()
            };
            req.SetRequestHeader("Content-Type", "application/json");
            try { await req.SendWebRequest(); }
            catch (System.Exception e) { Debug.LogWarning($"[CloudSync/REST] save error: {e.Message}"); }
            bool ok = req.result == UnityWebRequest.Result.Success;
            if (!ok) Debug.LogWarning($"[CloudSync/REST] save failed: {req.error}");
            return ok;
        }

        public async UniTask<string> LoadAsync(string playerId)
        {
            if (string.IsNullOrEmpty(_baseUrl)) { Debug.LogWarning("[CloudSync/REST] base URL not set."); return null; }
            using var req = UnityWebRequest.Get(Url(playerId));
            try { await req.SendWebRequest(); }
            catch (System.Exception e) { Debug.LogWarning($"[CloudSync/REST] load error: {e.Message}"); return null; }
            if (req.result != UnityWebRequest.Result.Success)
            {
                // 404 (no save yet) is a normal "first run" case, not an error to surface loudly.
                if (req.responseCode != 404) Debug.LogWarning($"[CloudSync/REST] load failed: {req.error}");
                return null;
            }
            var text = req.downloadHandler != null ? req.downloadHandler.text : null;
            return string.IsNullOrEmpty(text) ? null : text;
        }
    }
}
