using System;
using System.Collections.Generic;
namespace Luzart
{
    public interface IDomain
    {
        void AddService<T>(T instance, string id = null);
        void RemoveService<T>(T instance, string id = null);
        T GetService<T>(string id = null);
        void Add<T>(T instance, string id = null);
        bool Remove<T>(T instance, string id = null);
        T GetFirst<T>();
        T Get<T>(string id = null);
        bool TryGet<T>(out T result, string id = null);
        IReadOnlyList<T> GetAll<T>();
        void Inject(object target);
        // Lifecycle
        void InitializeAll();
        void StartAll();
        void StopAll();
        void TerminateAll();
    }
}
