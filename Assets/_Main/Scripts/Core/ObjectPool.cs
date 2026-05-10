using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Generic Object Pool để tái sử dụng GameObjects.
/// Thay thế pattern Instantiate/Destroy gây GC pressure.
/// 
/// Sử dụng:
///   var pool = ObjectPool.Instance;
///   var bullet = pool.Get(bulletPrefab, position, rotation);
///   pool.Return(bullet, bulletPrefab); // thay vì Destroy
/// </summary>
public class ObjectPool : MonoBehaviour
{
    public static ObjectPool Instance { get; private set; }

    [Header("Settings")]
    [SerializeField] private int defaultPrewarmCount = 5;

    // Pool storage: prefab instanceID → Queue of inactive objects
    private Dictionary<int, Queue<GameObject>> pools = new Dictionary<int, Queue<GameObject>>();
    private Dictionary<int, Transform> poolParents = new Dictionary<int, Transform>();

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// Lấy 1 object từ pool. Nếu pool rỗng, tạo mới.
    /// </summary>
    public GameObject Get(GameObject prefab, Vector3 position, Quaternion rotation,
                          Transform parent = null)
    {
        int id = prefab.GetInstanceID();
        EnsurePool(id, prefab);

        GameObject obj;
        if (pools[id].Count > 0)
        {
            obj = pools[id].Dequeue();

            // Có thể bị destroy bởi scene unload
            if (obj == null)
            {
                obj = CreateNew(prefab, id);
            }

            obj.transform.position = position;
            obj.transform.rotation = rotation;
            if (parent != null)
                obj.transform.SetParent(parent);
            obj.SetActive(true);
        }
        else
        {
            obj = CreateNew(prefab, id);
            obj.transform.position = position;
            obj.transform.rotation = rotation;
            if (parent != null)
                obj.transform.SetParent(parent);
        }

        return obj;
    }

    /// <summary>
    /// Trả object về pool thay vì Destroy.
    /// </summary>
    public void Return(GameObject obj, GameObject prefab)
    {
        if (obj == null) return;

        int id = prefab.GetInstanceID();
        EnsurePool(id, prefab);

        obj.SetActive(false);
        obj.transform.SetParent(poolParents[id]);
        pools[id].Enqueue(obj);
    }

    /// <summary>
    /// Trả object về pool sau delay giây.
    /// </summary>
    public void ReturnDelayed(GameObject obj, GameObject prefab, float delay)
    {
        if (obj == null) return;
        StartCoroutine(ReturnAfterDelay(obj, prefab, delay));
    }

    /// <summary>
    /// Pre-warm pool với N objects.
    /// </summary>
    public void Prewarm(GameObject prefab, int count)
    {
        int id = prefab.GetInstanceID();
        EnsurePool(id, prefab);

        for (int i = 0; i < count; i++)
        {
            var obj = CreateNew(prefab, id);
            obj.SetActive(false);
            obj.transform.SetParent(poolParents[id]);
            pools[id].Enqueue(obj);
        }
    }

    /// <summary>
    /// Xóa toàn bộ pools (gọi khi load scene mới).
    /// </summary>
    public void ClearAll()
    {
        foreach (var kvp in pools)
        {
            while (kvp.Value.Count > 0)
            {
                var obj = kvp.Value.Dequeue();
                if (obj != null)
                    Destroy(obj);
            }
        }
        pools.Clear();

        foreach (var kvp in poolParents)
        {
            if (kvp.Value != null)
                Destroy(kvp.Value.gameObject);
        }
        poolParents.Clear();
    }

    // ---- Private ----

    private void EnsurePool(int id, GameObject prefab)
    {
        if (!pools.ContainsKey(id))
        {
            pools[id] = new Queue<GameObject>();
            var parentGO = new GameObject($"Pool_{prefab.name}");
            parentGO.transform.SetParent(transform);
            poolParents[id] = parentGO.transform;
        }
    }

    private GameObject CreateNew(GameObject prefab, int id)
    {
        var obj = Instantiate(prefab);
        // Tag với pool id để Return có thể tìm đúng pool
        var tag = obj.AddComponent<PoolTag>();
        tag.prefabId = id;
        return obj;
    }

    private System.Collections.IEnumerator ReturnAfterDelay(GameObject obj,
                                                              GameObject prefab, float delay)
    {
        yield return new WaitForSeconds(delay);
        Return(obj, prefab);
    }
}

/// <summary>
/// Helper component gắn trên pooled objects để track prefab gốc.
/// </summary>
public class PoolTag : MonoBehaviour
{
    [HideInInspector] public int prefabId;
}
