using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
namespace Luzart
{
    public interface ICollider
    {
        SpatialLayer Layer { get; }
        Vector2 Position2D { get; }
        float Radius { get; }
        event Action<ICollider, ICollider> OnCollideWith;
        void CollideWith(ICollider collider);
        void UpdatePosition(Vector2 position);
    }
    public class Collider : ICollider
    {
        private SpatialLayer _layer;
        public SpatialLayer Layer => _layer;
        private Vector2 _position;
        public Vector2 Position2D => _position;
        private float _radius;
        public float Radius => _radius;
        public event Action<ICollider, ICollider> OnCollideWith;
        public Collider(SpatialLayer layer,  Vector2 position, float radius)
        {
            _layer = layer;
            _position = position;
            _radius = radius;
        }
        public void CollideWith(ICollider collider)
        {
            OnCollideWith?.Invoke(this, collider);
        }
        public void UpdatePosition(Vector2 position)
        {
            this._position = position;
        }
    }
    public class ColliderBehavior : BehaviorBase
    {
        private CollisionManager _collisionManager;
        private Mapping _mapping;
        private ICollider _collider;
        public ICollider Collider => _collider;
        public ColliderBehavior(IEntity owner, SpatialLayer layer, float radius) : base(owner)
        {
            _collider = new Collider(layer, new Vector2(UnityEngine.Random.Range(-1000,1000), UnityEngine.Random.Range(-1000,1000)), radius);
            owner.Transform.Position.Changed += OnChangePosition;
        }
        public ColliderBehavior(IEntity owner, SpatialLayer layer, double radius) : base(owner)
        {
            _collider = new Collider(layer, new Vector2(UnityEngine.Random.Range(-1000, 1000), UnityEngine.Random.Range(-1000, 1000)), (float)radius);
            owner.Transform.Position.Changed += OnChangePosition;
        }
        protected override void DoStart()
        {
            base.DoStart();
            // Resolve deps. The scene may not have a CollisionManager (post-W-nuke);
            // auto-spawn one + register with SceneRootManager so its IContent
            // Initialize/Start fires. Mapping is added to Domain directly if absent.
            _collisionManager = this.MyDomain?.Get<CollisionManager>();
            if (_collisionManager == null && SceneRootManager.Instance != null)
            {
                _collisionManager = UnityEngine.Object.FindFirstObjectByType<CollisionManager>();
                if (_collisionManager == null)
                {
                    var go = new GameObject("CollisionManager (auto-spawned)");
                    _collisionManager = go.AddComponent<CollisionManager>();
                    SceneRootManager.Instance.RegisterContent(_collisionManager);
                    UnityEngine.Debug.LogWarning("[ColliderBehavior] No CollisionManager in scene — auto-spawned one.");
                }
            }
            _mapping = this.MyDomain?.Get<Mapping>();
            if (_mapping == null && this.MyDomain != null)
            {
                _mapping = new Mapping();
                this.MyDomain.Add<Mapping>(_mapping);
                UnityEngine.Debug.LogWarning("[ColliderBehavior] No Mapping in Domain — auto-added.");
            }

            // Defensive null checks — if any dep still missing, skip registration
            // (entity won't be searchable via TargetProvider but won't crash).
            if (_collisionManager == null || _mapping == null)
            {
                UnityEngine.Debug.LogError($"[ColliderBehavior] Cannot register {this.Owner?.Id} — CollisionManager={_collisionManager} Mapping={_mapping}");
                return;
            }

            _collisionManager.Add(_collider);
            _mapping.AddCollider(this.Owner, _collider);
            _collider.UpdatePosition(this.Owner.Transform.Position.Value);
        }
        //protected override void DoUpdate(float dt)
        //{
        //    base.DoUpdate(dt);
        //    _collider.UpdatePosition(this.Owner.Transform.Position.Value);
        //}
        protected override void DoDestroy()
        {
            _collisionManager?.Remove(_collider);
            if (Owner?.Transform != null)
            {
                Owner.Transform.Position.Changed -= OnChangePosition;
            }
            base.DoDestroy();
        }
        public void OnChangePosition(IValue<Vector2> value)
        {
            _collider.UpdatePosition(value.Value);
        }
    }
}