using Luzart;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using System.IO;
using System.Text;
using Cysharp.Threading.Tasks;
using Unity.VisualScripting;
using Unity.Services.Core;
using Unity.Services.Authentication;
using Unity.Services.CloudSave;

namespace Luzart
{
    [CreateAssetMenu(menuName = "Luzart/System/SaveService")]
    public class SaveService : AbstractScriptableService
    {
        [Header("Save Settings")]
        [SerializeField] private bool _autoSaveEnabled = true;
        [SerializeField] private float _autoSaveInterval = 60f;
        [SerializeField] private string _saveFileName = "GameSave.json";
        [Header("Performance Settings")]
        [SerializeField] private bool _enableDeltaSave = true;
        [SerializeField] private bool _prettyPrint = false;
        [Header("Debug")]
        [SerializeField] private bool _isSaving = false;
        private float _autoSaveTimer;
        private bool _isInitialized = false;
        private bool _isStarted = false;
        private Dictionary<string, int> _lastSavedDataHashes = new Dictionary<string, int>();
        public bool IsInitialized => _isInitialized;
        public bool IsStarted => _isStarted;
        public bool IsSaving => _isSaving;

        protected override void DoInitialize()
        {
            base.DoInitialize();
            if (_isInitialized) return;
            _isSaving = false;
            _autoSaveTimer = _autoSaveInterval;
            _lastSavedDataHashes.Clear();
            _isInitialized = true;
            Debug.Log("[SaveService] Initialized");
        }

        protected override void DoStartContent()
        {
            base.DoStartContent();
            _isStarted = true;
            LoadAllData().Forget();
        }

        protected override void DoStopContent()
        {
            base.DoStopContent();
            SaveAllData().Forget();
            _isStarted = false;
        }

        protected override void DoTerminate()
        {
            base.DoTerminate();
            _lastSavedDataHashes.Clear();
            _isInitialized = false;
        }

        public void UpdateAutoSave(float deltaTime)
        {
            if (!_isStarted || !_autoSaveEnabled || _isSaving) return;
            _autoSaveTimer -= deltaTime;
            if (_autoSaveTimer <= 0f)
            {
                _autoSaveTimer = _autoSaveInterval;
                SaveAllData().Forget();
            }
        }

        private async UniTask InitializeUGSAsync()
        {
            try
            {
                if (UnityServices.State == ServicesInitializationState.Uninitialized)
                {
                    await UnityServices.InitializeAsync();
                }
                if (!AuthenticationService.Instance.IsSignedIn)
                {
                    await AuthenticationService.Instance.SignInAnonymouslyAsync();
                }
            }
            catch (System.Exception e)
            {
                Debug.LogWarning("[SaveService] UGS Init/Login failed: " + e.Message);
            }
        }

        public async UniTask SaveAllData()
        {
            if (!_isStarted || _isSaving) return;
            _isSaving = true;
            try
            {
                var saveDataWrapper = await PrepareSerializedDataAsync();
                if (saveDataWrapper != null)
                {
                    string filePath = Path.Combine(Application.persistentDataPath, _saveFileName);
                    await WriteSaveDataToDiskAsync(saveDataWrapper, filePath);
                    Debug.Log($"[SaveService] Save completed - {saveDataWrapper.contentSaveDataList.Count} objects");

                    await InitializeUGSAsync();
                    if (AuthenticationService.Instance.IsSignedIn)
                    {
                        try
                        {
                            string json = JsonUtility.ToJson(saveDataWrapper, _prettyPrint);
                            var cloudData = new Dictionary<string, object> { { "GameSave", json } };
                            await CloudSaveService.Instance.Data.Player.SaveAsync(cloudData);
                        }
                        catch (System.Exception e)
                        {
                            Debug.LogWarning("[SaveService] Cloud save failed: " + e.Message);
                        }
                    }
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[SaveService] Save failed: {e.Message}");
            }
            finally
            {
                _isSaving = false;
            }
        }

        private async UniTask<SaveDataWrapper> PrepareSerializedDataAsync()
        {
            await UniTask.SwitchToThreadPool();
            var saveDataWrapper = new SaveDataWrapper();
            var saveableObjects = _domain.GetAll<ISaveable>();
            foreach (var saveable in saveableObjects)
            {
                if (saveable is not IContent saveableContent)
                {
                    Debug.LogWarning($"[SaveService] {saveable} is not IContent, skipping");
                    continue;
                }
                var saveItems = saveable.Save();
                if (saveItems == null || !saveItems.Any()) continue;
                var contentSaveData = new ContentSaveData
                {
                    contentId = saveableContent.Id,
                    saveItems = saveItems.Select(item => CreateOptimizedSaveItem(item)).ToArray()
                };
                if (_enableDeltaSave)
                {
                    int dataHash = ComputeDataHash(contentSaveData);
                    if (_lastSavedDataHashes.TryGetValue(saveableContent.Id, out var lastHash) && lastHash == dataHash)
                    {
                        continue;
                    }
                    _lastSavedDataHashes[saveableContent.Id] = dataHash;
                }
                saveDataWrapper.contentSaveDataList.Add(contentSaveData);
            }
            await UniTask.SwitchToMainThread();
            return saveDataWrapper.contentSaveDataList.Count > 0 ? saveDataWrapper : null;
        }

        private OptimizedSaveItem CreateOptimizedSaveItem(SaveItem item)
        {
            return new OptimizedSaveItem
            {
                key = item.key,
                type = item.valueType,
                v = GetValueForSerialization(item)
            };
        }

        private object GetValueForSerialization(SaveItem item)
        {
            return item.valueType switch
            {
                ValueSaveType.Bool => item.boolValue,
                ValueSaveType.Int => item.intValue,
                ValueSaveType.Float => item.floatValue,
                ValueSaveType.Double => item.doubleValue,
                ValueSaveType.String => item.stringValue ?? "",
                _ => ""
            };
        }

        private async UniTask WriteSaveDataToDiskAsync(SaveDataWrapper saveDataWrapper, string filePath)
        {
            await UniTask.SwitchToThreadPool();
            string json = JsonUtility.ToJson(saveDataWrapper, _prettyPrint);
            File.WriteAllText(filePath, json, Encoding.UTF8);
            await UniTask.SwitchToMainThread();
        }

        private int ComputeDataHash(ContentSaveData data)
        {
            int hash = data.contentId?.GetHashCode() ?? 0;
            if (data.saveItems != null)
            {
                foreach (var item in data.saveItems)
                {
                    hash = unchecked(hash * 397 ^ (item.key?.GetHashCode() ?? 0));
                    hash = unchecked(hash * 397 ^ (int)item.type);
                    hash = unchecked(hash * 397 ^ (item.v?.GetHashCode() ?? 0));
                }
            }
            return hash;
        }

        public async UniTask LoadAllData()
        {
            if (!_isStarted) return;
            await InitializeUGSAsync();
            if (AuthenticationService.Instance.IsSignedIn)
            {
                try
                {
                    var data = await CloudSaveService.Instance.Data.Player.LoadAsync(new HashSet<string> { "GameSave" });
                    if (data.TryGetValue("GameSave", out var value))
                    {
                        string json = value.Value.GetAsString();
                        if (!string.IsNullOrEmpty(json))
                        {
                            string filePath = Path.Combine(Application.persistentDataPath, _saveFileName);
                            File.WriteAllText(filePath, json, Encoding.UTF8);
                        }
                    }
                }
                catch (System.Exception e)
                {
                    Debug.LogWarning("[SaveService] Cloud load failed: " + e.Message);
                }
            }
            string filePathLocal = Path.Combine(Application.persistentDataPath, _saveFileName);
            if (await TryLoadSaveFileAsync(filePathLocal))
            {
                return;
            }
            Debug.LogWarning("[SaveService] No valid save file found");
        }

        private async UniTask<bool> TryLoadSaveFileAsync(string filePath)
        {
            if (!File.Exists(filePath)) return false;
            try
            {
                await UniTask.SwitchToThreadPool();
                string json = File.ReadAllText(filePath, Encoding.UTF8);
                await UniTask.SwitchToMainThread();
                if (string.IsNullOrEmpty(json)) return false;
                ProcessLoadedData(json);
                return true;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[SaveService] Failed to load {filePath}: {e.Message}");
                return false;
            }
        }

        private void ProcessLoadedData(string json)
        {
            try
            {
                var saveDataWrapper = JsonUtility.FromJson<SaveDataWrapper>(json);
                if (saveDataWrapper?.contentSaveDataList != null)
                {
                    var saveableObjects = _domain.GetAll<ISaveable>();
                    int loadedCount = 0;
                    foreach (var contentSaveData in saveDataWrapper.contentSaveDataList)
                    {
                        var matchingSaveable = saveableObjects.FirstOrDefault(saveable =>
                            saveable is IContent content && content.Id == contentSaveData.contentId);
                        if (matchingSaveable != null)
                        {
                            var saveItems = contentSaveData.saveItems?.Select(item => CreateSaveItemFromOptimized(item))
                                           .ToArray() ?? new SaveItem[0];
                            matchingSaveable.Load(saveItems);
                            loadedCount++;
                            if (_enableDeltaSave)
                            {
                                _lastSavedDataHashes[contentSaveData.contentId] = ComputeDataHash(contentSaveData);
                            }
                        }
                        else
                        {
                            Debug.LogWarning($"[SaveService] Could not find saveable object with ID: {contentSaveData.contentId}");
                        }
                    }
                    Debug.Log($"[SaveService] Loaded {loadedCount}/{saveDataWrapper.contentSaveDataList.Count} objects");
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[SaveService] Failed to process loaded data: {e.Message}");
            }
        }

        private SaveItem CreateSaveItemFromOptimized(OptimizedSaveItem item)
        {
            return item.type switch
            {
                ValueSaveType.Bool => new SaveItem(item.key, item.GetBoolValue()),
                ValueSaveType.Int => new SaveItem(item.key, item.GetIntValue()),
                ValueSaveType.Float => new SaveItem(item.key, item.GetFloatValue()),
                ValueSaveType.Double => new SaveItem(item.key, item.GetDoubleValue()),
                ValueSaveType.String => new SaveItem(item.key, item.GetStringValue()),
                _ => new SaveItem(item.key, "")
            };
        }

        public void ForceSave()
        {
            SaveAllData().Forget();
        }

        public void ForceLoad()
        {
            LoadAllData().Forget();
        }

        public void ClearCache()
        {
            _lastSavedDataHashes.Clear();
            Debug.Log("[SaveService] Cache cleared");
        }

        public void ShowSaveInfo()
        {
            string filePath = Path.Combine(Application.persistentDataPath, _saveFileName);
            if (File.Exists(filePath))
            {
                var fileInfo = new FileInfo(filePath);
                Debug.Log($"[SaveService] Save file: {filePath}\nSize: {fileInfo.Length} bytes\nLast modified: {fileInfo.LastWriteTime}");
            }
            else
            {
                Debug.Log("[SaveService] No save file found");
            }
        }
    }

    [System.Serializable]
    public class SaveDataWrapper
    {
        public List<ContentSaveData> contentSaveDataList = new List<ContentSaveData>();
    }

    [System.Serializable]
    public class ContentSaveData
    {
        public string contentId;
        public OptimizedSaveItem[] saveItems;
    }

    [System.Serializable]
    public class OptimizedSaveItem
    {
        public string key;
        public ValueSaveType type;
        [SerializeField] private string value;
        public object v
        {
            get
            {
                if (string.IsNullOrEmpty(value)) return GetDefaultValue();
                return type switch
                {
                    ValueSaveType.Bool => bool.Parse(value),
                    ValueSaveType.Int => int.Parse(value),
                    ValueSaveType.Float => float.Parse(value),
                    ValueSaveType.Double => double.Parse(value),
                    ValueSaveType.String => value,
                    _ => value
                };
            }
            set
            {
                this.value = value?.ToString() ?? "";
            }
        }

        private object GetDefaultValue()
        {
            return type switch
            {
                ValueSaveType.Bool => false,
                ValueSaveType.Int => 0,
                ValueSaveType.Float => 0f,
                ValueSaveType.Double => 0.0,
                ValueSaveType.String => "",
                _ => ""
            };
        }

        public bool GetBoolValue() => bool.TryParse(value, out var result) ? result : false;
        public int GetIntValue() => int.TryParse(value, out var result) ? result : 0;
        public float GetFloatValue() => float.TryParse(value, out var result) ? result : 0f;
        public double GetDoubleValue() => double.TryParse(value, out var result) ? result : 0.0;
        public string GetStringValue() => value ?? "";
    }
}