using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Singleton quản lý toàn bộ inventory equipment của player.
/// Persistent giữa các trận.
/// Hỗ trợ: thêm/xóa item, tìm kiếm, merge (3 cùng loại + cùng quality → 1 quality cao hơn).
/// </summary>
public class EquipmentInventory : MonoBehaviour
{
    public static EquipmentInventory Instance { get; private set; }

    [Header("=== Equipment Database (để lookup EquipmentData từ equipId) ===")]
    [SerializeField] private EquipmentData[] allEquipmentDatas;

    // ---- Runtime State ----
    private List<EquipmentInstance> allItems = new List<EquipmentInstance>();

    // ---- Events ----
    public event Action OnInventoryChanged;

    // ---- Properties ----
    public IReadOnlyList<EquipmentInstance> AllItems => allItems;
    public int ItemCount => allItems.Count;

    // ============================================================
    // Lifecycle
    // ============================================================

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            LoadInventory();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // ============================================================
    // Inventory API
    // ============================================================

    /// <summary>Thêm item mới vào inventory.</summary>
    public void AddItem(EquipmentInstance item)
    {
        if (item == null) return;
        allItems.Add(item);
        SaveInventory();
        OnInventoryChanged?.Invoke();
    }

    /// <summary>Thêm item mới từ EquipmentData với quality chỉ định.</summary>
    public EquipmentInstance AddNewItem(EquipmentData data, EquipQuality quality = EquipQuality.Normal)
    {
        var instance = new EquipmentInstance(data, quality);
        AddItem(instance);
        return instance;
    }

    /// <summary>Xóa item khỏi inventory theo instanceId.</summary>
    public bool RemoveItem(string instanceId)
    {
        var item = FindItem(instanceId);
        if (item == null) return false;
        if (item.isEquipped) return false; // Không xóa item đang equipped

        allItems.Remove(item);
        SaveInventory();
        OnInventoryChanged?.Invoke();
        return true;
    }

    /// <summary>Tìm item theo instanceId.</summary>
    public EquipmentInstance FindItem(string instanceId)
    {
        foreach (var item in allItems)
        {
            if (item.instanceId == instanceId)
                return item;
        }
        return null;
    }

    /// <summary>Lấy tất cả items theo slot.</summary>
    public List<EquipmentInstance> GetItemsBySlot(EquipSlot slot)
    {
        var result = new List<EquipmentInstance>();
        foreach (var item in allItems)
        {
            if (item.data != null && item.data.slot == slot)
                result.Add(item);
        }
        return result;
    }

    /// <summary>Lấy tất cả items theo quality.</summary>
    public List<EquipmentInstance> GetItemsByQuality(EquipQuality quality)
    {
        var result = new List<EquipmentInstance>();
        foreach (var item in allItems)
        {
            if (item.currentQuality == quality)
                result.Add(item);
        }
        return result;
    }

    // ============================================================
    // Merge System
    // ============================================================

    /// <summary>
    /// Tìm các items có thể merge cùng với item chỉ định.
    /// Điều kiện: cùng equipId + cùng quality + chưa equipped.
    /// </summary>
    public List<EquipmentInstance> GetMergeCandidates(EquipmentInstance item)
    {
        var result = new List<EquipmentInstance>();
        if (item == null || item.data == null) return result;
        if (item.currentQuality >= EquipQuality.Relic) return result; // Đã max quality

        foreach (var other in allItems)
        {
            if (other == item) continue;
            if (other.isEquipped) continue;
            if (other.data == null) continue;
            if (other.data.equipId != item.data.equipId) continue;
            if (other.currentQuality != item.currentQuality) continue;
            result.Add(other);
        }
        return result;
    }

    /// <summary>
    /// Kiểm tra có thể merge 3 items không.
    /// Điều kiện: 3 items cùng equipId + cùng quality + quality < Relic.
    /// </summary>
    public bool CanMerge(EquipmentInstance item1, EquipmentInstance item2, EquipmentInstance item3)
    {
        if (item1 == null || item2 == null || item3 == null) return false;
        if (item1.data == null || item2.data == null || item3.data == null) return false;

        // Phải cùng equipId
        if (item1.data.equipId != item2.data.equipId ||
            item1.data.equipId != item3.data.equipId) return false;

        // Phải cùng quality
        if (item1.currentQuality != item2.currentQuality ||
            item1.currentQuality != item3.currentQuality) return false;

        // Quality chưa max
        if (item1.currentQuality >= EquipQuality.Relic) return false;

        // Không item nào đang equipped
        if (item1.isEquipped || item2.isEquipped || item3.isEquipped) return false;

        return true;
    }

    /// <summary>
    /// Merge 3 items cùng loại + cùng quality → 1 item quality cao hơn.
    /// Remove 3 items cũ, tạo 1 item mới.
    /// Enhance level reset về 0 cho item mới.
    /// Trả item mới hoặc null nếu không merge được.
    /// </summary>
    public EquipmentInstance TryMerge(EquipmentInstance item1, EquipmentInstance item2, EquipmentInstance item3)
    {
        if (!CanMerge(item1, item2, item3)) return null;

        EquipQuality newQuality = item1.currentQuality + 1;
        EquipmentData equipData = item1.data;

        // Remove 3 items cũ
        allItems.Remove(item1);
        allItems.Remove(item2);
        allItems.Remove(item3);

        // Tạo item mới với quality cao hơn
        var newItem = new EquipmentInstance(equipData, newQuality);
        allItems.Add(newItem);

        SaveInventory();
        OnInventoryChanged?.Invoke();

        return newItem;
    }

    // ============================================================
    // Save / Load
    // ============================================================

    public void SaveInventory()
    {
        var saveData = new EquipmentSaveData();
        foreach (var item in allItems)
        {
            saveData.items.Add(item.ToSaveEntry());
        }
        string json = JsonUtility.ToJson(saveData);

        if (DataManager.Instance != null)
        {
            DataManager.Instance.SetEquipmentInventory(json);
            DataManager.Instance.SaveData();
        }
        else
        {
            PlayerPrefs.SetString("equip_inventory", json);
            PlayerPrefs.Save();
        }
    }

    public void LoadInventory()
    {
        string json = "";
        if (DataManager.Instance != null)
            json = DataManager.Instance.GetEquipmentInventory();
        else
            json = PlayerPrefs.GetString("equip_inventory", "");

        if (string.IsNullOrEmpty(json)) return;

        try
        {
            var saveData = JsonUtility.FromJson<EquipmentSaveData>(json);
            if (saveData == null || saveData.items == null) return;

            allItems.Clear();
            foreach (var entry in saveData.items)
            {
                var equipData = FindEquipmentData(entry.equipId);
                if (equipData == null) continue;

                var instance = new EquipmentInstance(equipData, (EquipQuality)entry.quality);
                instance.instanceId = entry.instanceId;
                instance.enhanceLevel = entry.enhanceLevel;
                instance.isEquipped = entry.isEquipped;
                allItems.Add(instance);
            }
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"[EquipmentInventory] Failed to load inventory: {e.Message}");
        }
    }

    /// <summary>Tìm EquipmentData SO theo equipId.</summary>
    private EquipmentData FindEquipmentData(string equipId)
    {
        if (allEquipmentDatas == null) return null;
        foreach (var data in allEquipmentDatas)
        {
            if (data != null && data.equipId == equipId)
                return data;
        }
        return null;
    }
}
