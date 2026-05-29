using Luzart;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
[System.Serializable]
public class Domain : IDomain
{
    // Content
    private readonly Dictionary<string, object> _instances = new();
    private readonly List<object> _contents = new();
    // Service
    private readonly List<object> _services = new();
    // ---------------- Instance store ----------------
    public void Add<T>(T instance, string id = null)
    {
        if (!string.IsNullOrEmpty(id))
        {
            _instances[id] = instance;
        }
        _contents.Add(instance);
        Inject(instance);
    }
    public bool Remove<T>(string id = null)
    {
        if (!string.IsNullOrEmpty(id))
        {
            var instance = GetFirst<T>();
            if (instance != null)
            {
                return _contents.Remove(instance);
            }
        }
        return false;
    }
    public void AddService<T>(T service, string id = null)
    {
        _services.Add(service);
        if (service is IService c)
            c.Inject(this); // direct call, no reflection
    }
    public void RemoveService<T>(T service, string id = null)
    {
        _services.Remove(service);
    }
    public T GetService<T>(string id = null)
    {
        for (int i = 0; i < _services.Count; i++)
        {
            object c = _services[i];
            if (c is T t)
            {
                return t;
            }
        }
        return default;
    }
    public bool Remove<T>(T instance, string id = null)
    {
        if (!string.IsNullOrEmpty(id))
        {
            // Remove from dictionary if it exists
            var keyToRemove = _instances.FirstOrDefault(kvp => kvp.Value.Equals(instance)).Key;
            if (keyToRemove != null)
            {
                _instances.Remove(keyToRemove);
            }
        }
        return _contents.Remove(instance);
    }
    public T Get<T>(string id = null)
    {
        if (string.IsNullOrEmpty(id))
        {
            return GetFirst<T>();
        }
        else if (_instances.ContainsKey(id))
        {
            return (T)_instances[id];
        }
        else
        {
            UnityEngine.Debug.LogError("[Domain] No instance found with id: " + id);
            return default;
        }
    }
    public bool TryGet<T>(out T result, string id = null)
    {
        result = Get<T>(id);
        return result != null;
    }
    public IReadOnlyList<T> GetAll<T>()
    {
        return _contents.OfType<T>().ToList();
    }
    // ---------------- Injection ----------------
    public void Inject(object target)
    {
        if (target is IContent c)
            c.Inject(this); // direct call, no reflection
    }
    // ---------------- Lifecycle ----------------
    // Snapshot _contents before iterating so DoInitialize/DoStart/DoStop/DoTerminate
    // implementations can safely Add/Remove from the Domain without tripping
    // InvalidOperationException ("Collection was modified"). Items added during a
    // phase are NOT initialized this pass — they get their own lifecycle on next
    // Add (which triggers DoInject via Inject(instance)) or are picked up at next
    // phase if they're MonoBehavior-based (they receive Unity Awake/Start themselves).
    public void InitializeAll()
    {
        var allContent = _contents.OfType<IContent>().ToList();
        foreach (var c in allContent)
            c.Initialize();
    }
    public void StartAll()
    {
        var allContent = _contents.OfType<IContent>().ToList();
        foreach (var c in allContent)
            c.Start();
    }
    public void StopAll()
    {
        var allContent = _contents.OfType<IContent>().ToList();
        foreach (var c in allContent)
            c.Stop();
    }
    public void TerminateAll()
    {
        var allContent = _contents.OfType<IContent>().ToList();
        foreach (var c in allContent)
            c.Terminate();
    }
    public T GetFirst<T>()
    {
        for (int i = 0; i < _contents.Count; i++)
        {
            object c = _contents[i];
            if (c is T t)
            {
                return t;
            }
        }
        return default;
    }
}