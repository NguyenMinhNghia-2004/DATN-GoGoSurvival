using System.IO;
using System.Text;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Luzart
{
    /// <summary>
    /// Default <see cref="ICloudSaveProvider"/>: stores the player blob as a JSON file in
    /// <c>Application.persistentDataPath</c>, keyed by player id. It stands in for a "server" so the
    /// whole sync pipeline works and is testable with NO backend/SDK installed. Swap to
    /// <see cref="RestCloudProvider"/> or a Firebase/PlayFab provider when ready — no other code changes.
    /// </summary>
    public class LocalFileCloudProvider : ICloudSaveProvider
    {
        private static string PathFor(string playerId)
            => Path.Combine(Application.persistentDataPath, $"cloudsave_{Sanitize(playerId)}.json");

        public async UniTask<bool> SaveAsync(string playerId, string json)
        {
            try
            {
                string path = PathFor(playerId);
                await UniTask.SwitchToThreadPool();
                File.WriteAllText(path, json, Encoding.UTF8);
                await UniTask.SwitchToMainThread();
                return true;
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[CloudSync/Local] save failed: {e.Message}");
                return false;
            }
        }

        public async UniTask<string> LoadAsync(string playerId)
        {
            try
            {
                string path = PathFor(playerId);
                if (!File.Exists(path)) return null;
                await UniTask.SwitchToThreadPool();
                string json = File.ReadAllText(path, Encoding.UTF8);
                await UniTask.SwitchToMainThread();
                return json;
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[CloudSync/Local] load failed: {e.Message}");
                return null;
            }
        }

        private static string Sanitize(string s)
        {
            if (string.IsNullOrEmpty(s)) return "default";
            foreach (var c in Path.GetInvalidFileNameChars()) s = s.Replace(c, '_');
            return s;
        }
    }
}
