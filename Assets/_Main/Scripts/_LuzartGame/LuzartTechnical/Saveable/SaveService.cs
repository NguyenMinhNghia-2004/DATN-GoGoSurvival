using Luzart;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using System.IO;
using System.Text;
using Cysharp.Threading.Tasks;
using Unity.VisualScripting;
namespace Luzart
{
    [CreateAssetMenu(menuName = "Luzart/System/SaveService")]
    public class SaveService : AbstractScriptableService
    {
        [Header("Save Settings")]
        [SerializeField] private bool _autoSaveEnabled = true;
        [SerializeField] private float _autoSaveInterval = 60f; // seconds
        [SerializeField] private string _saveFileName = "GameSave.json";
        [Header("Performance Settings")]
        [SerializeField] private bool _enableDeltaSave = true; // Only save changed data
        [SerializeField] private bool _prettyPrint = false; // Disable for smaller files
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
                // Delta save: Only include if data has changed
                if (_enableDeltaSave)
                {
                    int dataHash = ComputeDataHash(contentSaveData);
                    if (_lastSavedDataHashes.TryGetValue(saveableContent.Id, out var lastHash) && lastHash == dataHash)
                    {
                        continue; // Skip unchanged data
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
            string filePath = Path.Combine(Application.persistentDataPath, _saveFileName);
            if (await TryLoadSaveFileAsync(filePath))
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
                            // Update hash for delta saves
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
        // Manual save/load methods
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
    // Optimized serializable wrapper classes
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
    // Highly optimized save item for Unity JsonUtility
    [System.Serializable]
    public class OptimizedSaveItem
    {
        public string key;          // key
        public ValueSaveType type;   // type
        // Union-like approach - only serialize relevant field
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
        // For manual access without boxing
        public bool GetBoolValue() => bool.TryParse(value, out var result) ? result : false;
        public int GetIntValue() => int.TryParse(value, out var result) ? result : 0;
        public float GetFloatValue() => float.TryParse(value, out var result) ? result : 0f;
        public double GetDoubleValue() => double.TryParse(value, out var result) ? result : 0.0;
        public string GetStringValue() => value ?? "";
    }
}