using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
namespace Luzart
{
    /// <summary>
    /// Interface for collision managers
    /// </summary>
    public interface ICollisionManager : IContent
    {
        IReadOnlyList<ICollider> colliders { get; }
        SpatialHashSystem spatialHashSystem { get; }
        void Add(ICollider collider);
        // Core collision processing
        void ProcessCollisions();
        void Remove(ICollider collider);
    }
    public class CollisionManager : AbstractMonoBehaviorContent
    {
        private ICollisionManager _activeImplementation;
        public ICollisionManager ActiveImplementation => _activeImplementation;
        // IContent lifecycle management
        private bool isInitialized = false;
        private bool isStarted = false;
        public override void DoInitialize()
        {
            base.DoInitialize();
            _activeImplementation = new ManagedCollisionManager();
            _activeImplementation.Inject(_domain);
            _activeImplementation.Initialize();
        }
        public override void DoStart()
        {
            base.DoStart();
            _activeImplementation.Start();
        }
        public override void DoStop()
        {
            base.DoStop();
            _activeImplementation?.Stop();
        }
        public override void DoTerminate()
        {
            base.DoTerminate();
            _activeImplementation?.Terminate();
        }
        protected override void DoUpdate(float dt)
        {
            base.DoUpdate(dt);
            ProcessCollisions();
        }
        // Public API delegates to active implementation
        public void ProcessCollisions() => _activeImplementation?.ProcessCollisions();
        public void Add(ICollider collider)
        {
            _activeImplementation?.Add(collider);
        }
        public void Remove(ICollider collider)
        {
            _activeImplementation?.Remove(collider);
        }
    }
    public class ManagedCollisionManager : ICollisionManager
    {
        public string Id { get; private set; }
        public IDomain MyDomain { get; private set; }
        private List<ICollider> _colliderList = new List<ICollider>();
        public IReadOnlyList<ICollider> colliders => _colliderList;
        private SpatialHashSystem _spatialHash;
        public SpatialHashSystem spatialHashSystem => _spatialHash;
        private Mapping _mapping;
        // Optimization: Maintain layer collections in real-time
        private readonly Dictionary<SpatialLayer, List<ICollider>> _layerColliders = new Dictionary<SpatialLayer, List<ICollider>>();
        private readonly List<ICollider> collidersNearMe = new List<ICollider>();
        // Optimization: Pre-collect collision pairs to avoid concurrent modification
        private readonly List<(ICollider, ICollider)> _collisionPairs = new List<(ICollider, ICollider)>(4096);
        private static readonly (SpatialLayer, SpatialLayer)[] CollisionPairs = new[]
        {
            (SpatialLayer.Projectiles_Player, SpatialLayer.Enemies),
            (SpatialLayer.Projectiles_Enemy, SpatialLayer.Player),
            (SpatialLayer.Player, SpatialLayer.Drop),
            (SpatialLayer.Player, SpatialLayer.Enemies)
        };
        public ManagedCollisionManager(string id = "CollisionManager")
        {
            Id = id;
            InitializeLayerColliders();
        }
        private void InitializeLayerColliders()
        {
            foreach (SpatialLayer layer in System.Enum.GetValues(typeof(SpatialLayer)))
            {
                _layerColliders[layer] = new List<ICollider>();
            }
        }
        public void Inject(IDomain domain)
        {
            MyDomain = domain;
        }
        public void Initialize()
        {
            _spatialHash = new SpatialHashSystem();
            _spatialHash.Initialize();
            _mapping = MyDomain.Get<Mapping>();
        }
        public void Start()
        {
        }
        public void Stop()
        {
        }
        public void Terminate()
        {
            _colliderList.Clear();
            foreach (var kvp in _layerColliders)
            {
                kvp.Value.Clear();
            }
            collidersNearMe.Clear();
            _collisionPairs.Clear();
        }
        public void ProcessCollisions()
        {
            _spatialHash.BeginFrame();
            int length = _colliderList.Count;
            for (int i = 0; i < length; i++)
            {
                var collider = _colliderList[i];
                _spatialHash.Feed(collider.Layer, collider);
            }
            _spatialHash.EndFrame();
            // Step 1: Collect all collision pairs first (safe phase)
            CollectCollisionPairs();
            // Step 2: Process all collected pairs (execution phase)
            ProcessCollectedPairs();
        }
        private void CollectCollisionPairs()
        {
            _collisionPairs.Clear();
            for (int i = 0; i < CollisionPairs.Length; i++)
            {
                var (layerA, layerB) = CollisionPairs[i];
                CollectCollisionPairsBetweenLayers(layerA, layerB);
            }
        }                       
        private void CollectCollisionPairsBetweenLayers(SpatialLayer layerA, SpatialLayer layerB)
        {
            var collidersA = _layerColliders[layerA];
            int countA = collidersA.Count;
            if(countA == 0) return;
            // Safe iteration - just collecting data, no logic execution
            for (int i = 0; i < countA; i++)
            {
                var colliderA = collidersA[i];
                if (colliderA == null) continue; // Safety check
                collidersNearMe.Clear();
                _spatialHash.GetNearby(layerB, colliderA.Position2D, colliderA.Radius * 2, collidersNearMe);
                int nearbyCount = collidersNearMe.Count;
                for (int j = 0; j < nearbyCount; j++)
                {
                    var colliderB = collidersNearMe[j];
                    if (colliderB == null) continue; // Safety check
                    if (IsColliding(colliderA, colliderB))
                    {
                        // Just collect the pair, don't execute logic yet
                        _collisionPairs.Add((colliderA, colliderB));
                    }
                }
            }
        }
        private void ProcessCollectedPairs()
        {
            // Process all collected collision pairs
            // Even if collections are modified during this phase, 
            // we're working with pre-collected data
            int pairCount = _collisionPairs.Count;
            for (int i = 0; i < pairCount; i++)
            {
                var (colliderA, colliderB) = _collisionPairs[i];
                // Additional safety: check if colliders are still valid
                if (colliderA == null || colliderB == null) continue;
                // Re-verify collision in case objects moved during collection
                if (IsColliding(colliderA, colliderB))
                {
                    TriggerCollisionEvents(colliderA, colliderB);
                }
            }
        }
        private bool IsColliding(ICollider colliderA, ICollider colliderB)
        {
            var posA = colliderA.Position2D;
            var posB = colliderB.Position2D;
            float deltaX = posA.x - posB.x;
            float deltaY = posA.y - posB.y;
            float distanceSquared = deltaX * deltaX + deltaY * deltaY;
            float combinedRadius = colliderA.Radius + colliderB.Radius;
            float combinedRadiusSquared = combinedRadius * combinedRadius;
            return distanceSquared <= combinedRadiusSquared;
        }
        private void TriggerCollisionEvents(ICollider colliderA, ICollider colliderB)
        {
            colliderA.CollideWith(colliderB);
            colliderB.CollideWith(colliderA);
        }
        private bool ShouldColliderHandleCollision(ICollider colliderA, ICollider colliderB)
        {
            // Define priority: Player > Projectiles > Enemies > Drops
            return GetLayerPriority(colliderA.Layer) >= GetLayerPriority(colliderB.Layer);
        }
        private int GetLayerPriority(SpatialLayer layer)
        {
            return layer switch
            {
                SpatialLayer.Player => 4,
                SpatialLayer.Projectiles_Player => 3,
                SpatialLayer.Projectiles_Enemy => 3,
                SpatialLayer.Enemies => 2,
                SpatialLayer.Drop => 1,
                //SpatialLayer.Obstacles => 0,
                _ => 0
            };
        }
        public void Add(ICollider collider)
        {
            _colliderList.Add(collider);
            _layerColliders[collider.Layer].Add(collider);
        }
        public void Remove(ICollider collider)
        {
            _colliderList.Remove(collider);
            _layerColliders[collider.Layer].Remove(collider);
        }
    }
}