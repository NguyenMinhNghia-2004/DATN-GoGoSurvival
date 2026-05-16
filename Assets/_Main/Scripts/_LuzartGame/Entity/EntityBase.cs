using System;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
namespace Luzart
{
    public abstract class EntityBase : IEntity
    {
        public string Id { get; protected set; }
        public IDomain MyDomain { get; private set; }
        readonly List<IBehavior> _behaviors = new();
        public IReadOnlyList<IBehavior> Behaviors => _behaviors;
        protected TransformBehavior _transform;
        public ITransform Transform => _transform;
        protected bool _isDead = false;
        public bool IsDead => _isDead;
        public Action<IEntity> OnDead;
        #region IContent Implementation
        void IContent.Inject(IDomain domain)
        {
            Inject(domain);
        }
        void IContent.Start()
        {
            Start();
        }
        void IContent.Stop()
        {
            Stop();
        }
        void IContent.Initialize()
        {
            Initialize();
        }
        void IEntity.OnDeath()
        {
            OnDeath();
        }
        void IEntity.OnUpdate(float dt)
        {
            OnUpdate(dt);
        }
        void IContent.Terminate()
        {
            Terminate();
        }
        #endregion
        public virtual void Inject(IDomain domain)
        {
            MyDomain = domain;
            _transform = new TransformBehavior(this);
            AddBehavior(_transform);
        }
        public virtual void Initialize()
        {
        }
        public virtual void ConfigTransform(Vector3 position)
        {
            _transform?.SetPosition(position);
        }
        public virtual void Terminate()
        {
            for (int i = 0; i < _behaviors.Count; i++)
            {
                IBehavior b = _behaviors[i];
                b.OnDestroy();
            }
            OnDead = null;
        }
        public virtual void OnDeath()
        {
            _isDead = true;
            OnDead?.Invoke(this);
        }
        public virtual void OnUpdate(float dt)
        {
            for (int i = 0; i < _behaviors.Count; i++) _behaviors[i].Update(dt);
        }
        public T GetBehavior<T>() where T : IBehavior
        {
            int length = _behaviors.Count;
            for (int i = 0; i < length; i++)
            {
                if (_behaviors[i] is T behavior)
                {
                    return behavior;
                }
            }
            return default;
        }
        public void AddBehavior<T>(T behavior) where T : IBehavior
        {
            _behaviors.Add(behavior);
        }
        public virtual void Start()
        {
            for (int i = 0; i < _behaviors.Count; i++)
            {
                IBehavior behavior = _behaviors[i];
                behavior.Start();
            }
        }
        public virtual void Stop()
        {
            for (int i = 0; i < _behaviors.Count; i++)
            {
                _behaviors[i].OnDestroy();
            }
            _behaviors.Clear();
        }
    }
}