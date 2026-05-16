// Luzart/DI/SimpleEventBus.cs
using System;
using System.Collections.Generic;
namespace Luzart
{
    public class BroadcastService
    {
        private readonly Dictionary<Type, List<Delegate>> _handlers = new();
        public void Publish<T>(T evt)
        {
            if (_handlers.TryGetValue(typeof(T), out var list))
                foreach (var d in list.ToArray())
                    (d as Action<T>)?.Invoke(evt);
        }
        public void Subscribe<T>(Action<T> cb)
        {
            if (!_handlers.TryGetValue(typeof(T), out var list))
                _handlers[typeof(T)] = list = new List<Delegate>();
            if (!list.Contains(cb)) list.Add(cb);
        }
        public void Unsubscribe<T>(Action<T> cb)
        {
            if (_handlers.TryGetValue(typeof(T), out var list))
                list.Remove(cb);
        }
    }
}
