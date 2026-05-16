using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Pool;
using UnityEngine.Profiling;
namespace Luzart
{
    [System.Serializable]
    public class EnemyAIBehavior : BehaviorBase
    {
        private Vector2 _velocity;
        private float separationRadius = 2f;
        private float alignmentRadius = 4f;
        private float cohesionRadius = 6f;
        private float alignmentWeight = 0.5f;
        private double moveSpeed = 3.5f;
        private float maxForce = 5f;
        private float neighborUpdateInterval = 0.1f;
        private int maxNeighborsToConsider = 10;
        protected PlayerCharacter target;
        protected TargetProvider targetProvider;
        private List<EnemyCharacter> _cachedNeighbors = new List<EnemyCharacter>();
        private float _lastNeighborUpdate;
        private float _maxRadius;
        // Object pooling for temp collections
        private readonly List<EnemyCharacter> _tempNeighbors = new List<EnemyCharacter>(32);
        private readonly List<NeighborDistance> _distanceCache = new List<NeighborDistance>(32);
        private readonly Dictionary<EnemyCharacter, EnemyAIBehavior> _neighborAICache = new Dictionary<EnemyCharacter, EnemyAIBehavior>(128);
        private StatsBehavior _statBehavior;
        // Cached values to avoid repeated calculations
        private float _separationRadiusSqr;
        private float _alignmentRadiusSqr;
        private float _cohesionRadiusSqr;
        // Adaptive behavior cache
        private AdaptiveWeights _adaptiveWeights;
        private float _lastDistToTarget = -1f;
        // Struct for distance caching to avoid sorting overhead
        private struct NeighborDistance
        {
            public EnemyCharacter enemy;
            public float sqrDistance;
            public NeighborDistance(EnemyCharacter enemy, float sqrDistance)
            {
                this.enemy = enemy;
                this.sqrDistance = sqrDistance;
            }
        }
        // Struct to cache adaptive weights calculations
        private struct AdaptiveWeights
        {
            public float separation;
            public float cohesion;
            public float seek;
            public float distanceUsed;
        }
        public EnemyAIBehavior(IEntity owner) : base(owner) { }
        protected override void DoStart()
        {
            // Cache own StatsBehavior once
            _statBehavior = Owner.GetBehavior<StatsBehavior>();
            moveSpeed = _statBehavior.Get(StatType.Speed).Value;
            target = MyDomain.Get<PlayerCharacter>();
            _velocity = UnityEngine.Random.insideUnitCircle.normalized * (float)moveSpeed;
            targetProvider = MyDomain.Get<TargetProvider>();
            _maxRadius = Mathf.Max(separationRadius, alignmentRadius, cohesionRadius);
            // Pre-calculate squared values for distance checks
            _separationRadiusSqr = separationRadius * separationRadius;
            _alignmentRadiusSqr = alignmentRadius * alignmentRadius;
            _cohesionRadiusSqr = cohesionRadius * cohesionRadius;
        }
        protected override void DoUpdate(float dt)
        {
            if (Owner == null || target == null)
                return;
            Profiler.BeginSample("Enemy_UpdateNeighborCacheOptimized");
            UpdateNeighborCacheOptimized(dt);
            Profiler.EndSample();
            // Cache current distance and adaptive weights
            float distToTarget = Vector3.Distance(Owner.Transform.Position.Value, target.Transform.Position.Value);
            UpdateAdaptiveWeights(distToTarget);
            Vector2 separation = ComputeSeparationOptimized() * _adaptiveWeights.separation;
            Vector2 alignment = ComputeAlignmentOptimized() * alignmentWeight;
            Vector2 cohesion = ComputeCohesionOptimized() * _adaptiveWeights.cohesion;
            Vector2 seekTarget = ComputeSeekTargetOptimized() * _adaptiveWeights.seek;
            Vector2 acceleration = separation + alignment + cohesion + seekTarget;
            acceleration = Vector2.ClampMagnitude(acceleration, maxForce);
            _velocity += acceleration * dt;
            _velocity = Vector2.ClampMagnitude(_velocity, (float)moveSpeed);
            Vector2 newPosition = Owner.Transform.Position.Value + new Vector2(_velocity.x, _velocity.y) * dt;
            Owner.Transform.Position.Set(newPosition);
            UpdateRotationOptimized();
        }
        // HEAVILY OPTIMIZED neighbor cache - eliminates expensive sorting
        private void UpdateNeighborCacheOptimized(float dt)
        {
            _lastNeighborUpdate += dt;
            if (_lastNeighborUpdate >= neighborUpdateInterval)
            {
                _cachedNeighbors.Clear();
                _neighborAICache.Clear();
                _distanceCache.Clear();
                var nearby = ListPool<IEntity>.Get();
                Profiler.BeginSample("UpdateNeighborCacheOptimized_GetTargetsInRangeByLayer");
                targetProvider.GetTargetsInRangeByLayer(Owner, _maxRadius, SpatialLayer.Enemies, nearby);
                Profiler.EndSample();
                Vector2 myPosition = Owner.Transform.Position.Value;
                Profiler.BeginSample("UpdateNeighborCacheOptimized_Pass1");
                // First pass: collect neighbors and calculate distances
                foreach (var entity in nearby)
                {
                    if (entity is EnemyCharacter enemy && enemy != Owner)
                    {
                        float sqrDist = Vector2.SqrMagnitude(enemy.Transform.Position.Value - myPosition);
                        _distanceCache.Add(new NeighborDistance(enemy, sqrDist));
                        // Cache AI behavior immediately
                        var aiCache = enemy.GetBehavior<EnemyAIBehavior>();
                        if (aiCache != null)
                        {
                            _neighborAICache[enemy] = aiCache;
                        }
                    }
                }
                Profiler.EndSample();
                ListPool<IEntity>.Release(nearby);
                // OPTIMIZATION: Use partial sort instead of full sort
                if (_distanceCache.Count > maxNeighborsToConsider)
                {
                    // Find the maxNeighborsToConsider closest enemies using partial sort
                    // This is O(n + k*log(k)) instead of O(n*log(n))
                    Profiler.BeginSample("UpdateNeighborCacheOptimized_PartialSortByDistance");
                   // PartialSortByDistance(_distanceCache, maxNeighborsToConsider);
                    for (int i = 0; i < _distanceCache.Count; i++)
                    {
                        _cachedNeighbors.Add(_distanceCache[i].enemy);
                    }
                    Profiler.EndSample();
                }
                else
                {
                    Profiler.BeginSample("UpdateNeighborCacheOptimized_NoPartialSortByDistance");
                    // If we have fewer neighbors than max, just add them all
                    for (int i = 0; i < _distanceCache.Count; i++)
                    {
                        _cachedNeighbors.Add(_distanceCache[i].enemy);
                    }
                    Profiler.EndSample();
                }
                _lastNeighborUpdate = 0f;
            }
        }
        // Custom partial sort - much faster than full sort for small k
        private void PartialSortByDistance(List<NeighborDistance> list, int k)
        {
            int lenght = list.Count;
            // Use selection algorithm to find k smallest elements
            // More efficient than full sort when k << n
            for (int i = 0; i < lenght; i++)
            {
                int minIndex = i;
                float minDist = list[i].sqrDistance;
                // Find minimum in remaining elements
                for (int j = i + 1; j < list.Count; j++)
                {
                    if (list[j].sqrDistance < minDist)
                    {
                        minIndex = j;
                        minDist = list[j].sqrDistance;
                    }
                }
                // Swap to correct position
                if (minIndex != i)
                {
                    var temp = list[i];
                    list[i] = list[minIndex];
                    list[minIndex] = temp;
                }
            }
        }
        // Cache adaptive weights to avoid recalculating every frame
        private void UpdateAdaptiveWeights(float distToTarget)
        {
            // Only recalculate if distance changed significantly
            if (math.abs(distToTarget - _lastDistToTarget) > 0.5f)
            {
                float invLerpFar = 1f - Mathf.InverseLerp(3f, 8f, distToTarget);
                float invLerpSeek = 1f - Mathf.InverseLerp(4f, 10f, distToTarget);
                _adaptiveWeights.separation = Mathf.Lerp(3.0f, 8.0f, invLerpFar);
                _adaptiveWeights.cohesion = Mathf.Lerp(1.2f, 0.3f, invLerpFar);
                _adaptiveWeights.seek = Mathf.Lerp(2.5f, 1.5f, invLerpSeek);
                _lastDistToTarget = distToTarget;
            }
        }
        // Optimized separation with cached squared distances
        private Vector2 ComputeSeparationOptimized()
        {
            Vector2 steer = Vector2.zero;
            int count = 0;
            Vector2 myPosition = Owner.Transform.Position.Value;
            for (int i = 0; i < _cachedNeighbors.Count; i++)
            {
                var neighbor = _cachedNeighbors[i];
                Vector3 diff = myPosition - neighbor.Transform.Position.Value;
                float sqrDist = diff.sqrMagnitude;
                if (sqrDist > 0 && sqrDist < _separationRadiusSqr)
                {
                    // Avoid sqrt when possible - use inverse distance approximation
                    float invDist = math.rsqrt(sqrDist); // Fast inverse sqrt
                    steer += new Vector2(diff.x, diff.y) * invDist * invDist; // Normalized / dist
                    count++;
                }
            }
            if (count > 0)
                steer /= count;
            return steer.normalized;
        }
        // Optimized alignment with cached AI behaviors
        private Vector2 ComputeAlignmentOptimized()
        {
            Vector2 avgVelocity = Vector2.zero;
            int count = 0;
            Vector2 myPosition = Owner.Transform.Position.Value;
            for (int i = 0; i < _cachedNeighbors.Count; i++)
            {
                var neighbor = _cachedNeighbors[i];
                float sqrDist = Vector2.SqrMagnitude(neighbor.Transform.Position.Value - myPosition);
                if (sqrDist < _alignmentRadiusSqr)
                {
                    if (_neighborAICache.TryGetValue(neighbor, out var neighborAI))
                    {
                        avgVelocity += neighborAI._velocity;
                        count++;
                    }
                }
            }
            if (count == 0) return Vector2.zero;
            avgVelocity *= (1.0f / count); // Avoid division
            Vector2 desired = avgVelocity.normalized * (float)moveSpeed;
            return desired - _velocity;
        }
        // Optimized cohesion with reduced Vector2 operations
        private Vector2 ComputeCohesionOptimized()
        {
            Vector2 center = Vector2.zero;
            int count = 0;
            Vector2 myPosition = Owner.Transform.Position.Value;
            float myPosX = myPosition.x;
            float myPosY = myPosition.y;
            for (int i = 0; i < _cachedNeighbors.Count; i++)
            {
                var neighbor = _cachedNeighbors[i];
                float sqrDist = Vector2.SqrMagnitude(neighbor.Transform.Position.Value - myPosition);
                if (sqrDist < _cohesionRadiusSqr)
                {
                    Vector3 neighborPos = neighbor.Transform.Position.Value;
                    center.x += neighborPos.x;
                    center.y += neighborPos.y;
                    count++;
                }
            }
            if (count == 0) return Vector2.zero;
            center.x = (center.x / count) - myPosX;
            center.y = (center.y / count) - myPosY;
            return center.normalized;
        }
        // Cached seek target calculation
        private Vector2 ComputeSeekTargetOptimized()
        {
            Vector3 targetDirection = target.Transform.Position.Value - Owner.Transform.Position.Value;
            return new Vector2(targetDirection.x, targetDirection.y).normalized;
        }
        // Optimized rotation with reduced calculations
        private void UpdateRotationOptimized()
        {
            if (target == null) return;
            float dirX = target.Transform.Position.Value.x - Owner.Transform.Position.Value.x;
            // Avoid creating Quaternion objects when possible
            if (dirX > 0)
            {
                Owner.Transform.SetRotation(Quaternion.identity); // Reuse identity
            }
            else
            {
                Owner.Transform.SetRotation(Quaternion.Euler(0, 180, 0));
            }
        }
    }
}
