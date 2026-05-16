namespace Luzart
{
    using System;
    using System.Collections.Generic;
    public interface IBroadcastData { }
    public static class Broadcaster 
    {
        private static readonly Dictionary<Type, object> _subBroadcasters = new Dictionary<Type, object>();
        // Đăng ký handler cho kiểu dữ liệu cụ thể
        public static void Register<T>(Action<T> handler) where T : IBroadcastData
        {
            if (handler == null) return;
            var subBroadcaster = FindSubBroadcaster<T>();
            subBroadcaster.Register(handler);
        }
        // Hủy đăng ký handler
        public static void Unregister<T>(Action<T> handler) where T : IBroadcastData
        {
            if (handler == null) return;
            var type = typeof(T);
            if (_subBroadcasters.TryGetValue(type, out var obj))
            {
                ((SubBroadcaster<T>)obj).Unregister(handler);
            }
        }
        // Gửi broadcast cho tất cả các listener kiểu T
        public static void Broadcast<T>(T data) where T : IBroadcastData
        {
            var subBroadcaster = FindSubBroadcaster<T>();
            subBroadcaster.Broadcast(data);
        }
        private static SubBroadcaster<T> FindSubBroadcaster<T>() where T : IBroadcastData
        {
            var type = typeof(T);
            if (!_subBroadcasters.TryGetValue(type, out var sub))
            {
                var newSub = new SubBroadcaster<T>();
                _subBroadcasters[type] = newSub;
                return newSub;
            }
            return (SubBroadcaster<T>)sub;
        }
        // ------------------------
        // SubBroadcaster nội bộ
        // ------------------------
        private class SubBroadcaster<T> where T : IBroadcastData
        {
            private readonly List<Action<T>> _handlers = new List<Action<T>>();
            public void Register(Action<T> handler)
            {
                if (handler == null) return;
                if (!_handlers.Contains(handler))
                    _handlers.Add(handler);
            }
            public void Unregister(Action<T> handler)
            {
                if (handler == null) return;
                _handlers.Remove(handler);
            }
            public void Broadcast(T data)
            {
                // Sao chép danh sách để tránh lỗi nếu handler thay đổi trong khi đang loop
                var temp = _handlers.ToArray();
                for (int i = 0; i < temp.Length; i++)
                {
                    try
                    {
                        temp[i]?.Invoke(data);
                    }
                    catch (Exception e)
                    {
                        UnityEngine.Debug.LogError($"[Broadcaster] Error in handler: {e}");
                    }
                }
            }
        }
    }
    public class Observer : Singleton<Observer>
    {
        public delegate void Callback(object data);
        private readonly Dictionary<string, HashSet<Callback>> dict = new();
        public void AddObserver(string topic, Callback cb)
        {
            if (!dict.TryGetValue(topic, out var list))
                dict[topic] = list = new();
            list.Add(cb);
        }
        public void RemoveObserver(string topic, Callback cb)
        {
            if (dict.TryGetValue(topic, out var list))
                list.Remove(cb);
        }
        public void Notify(string topic, object data = null)
        {
            if (dict.TryGetValue(topic, out var list))
            {
                var copy = new HashSet<Callback>(list);
                foreach (var cb in copy) cb?.Invoke(data);
            }
        }
        // ====== Type-safe layer ======
        private readonly Dictionary<Type, HashSet<Delegate>> typed = new();
        public void Subscribe<T>(Action<T> handler)
        {
            if (!typed.TryGetValue(typeof(T), out var list))
                typed[typeof(T)] = list = new();
            list.Add(handler);
        }
        public void Unsubscribe<T>(Action<T> handler)
        {
            if (typed.TryGetValue(typeof(T), out var list))
                list.Remove(handler);
        }
        public void Publish<T>(T evt)
        {
            if (typed.TryGetValue(typeof(T), out var list))
            {
                var copy = new HashSet<Delegate>(list);
                foreach (var d in copy)
                    ((Action<T>)d)?.Invoke(evt);
            }
        }
        public void ClearAll()
        {
            dict.Clear();
            typed.Clear();
        }
    }
    public static class ObserverKey
    {
        public const string TimeActionPerSecond = "TimeActionPerSecond";
        public const string CoinObserverNormal = "CoinObserverNormal";
        public const string CoinObserverTextRun = "CoinObserverTextRun";
        public const string CoinObserverDontAuto = "CoinObserverDontAuto";
        public const string OnNewDay = "OnNewDay";
        public const string QuestKey = "QuestKey";
    }
}
