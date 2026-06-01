using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;
namespace Luzart
{
    public class TargetProvider : AbstractMonoBehaviorContent
    {
        [SerializeField] private float cacheExpireTime = 0.1f; // Cache results for 100ms
        // Dependencies
        private PlayerCharacter _player;
        private EnemySpawnerManager _enemyManager;
        private CollisionManager _collisionManager;
        private SpatialHashSystem _spatialHash;
        private Mapping _mapping;
        // Caching system for performance
        private readonly Dictionary<IEntity, CachedTargetResult> _cachedTargets = new();
        private float _lastCacheCleanTime;
        // Temp collections to avoid allocations
        private readonly List<IEntity> _tempTargetList = new();
        #region IContent Implementation
        public override void DoInitialize()
        {
            base.DoInitialize();
            _player = _domain.Get<PlayerCharacter>();
            _enemyManager = _domain.Get<EnemySpawnerManager>();
            // Note: _collisionManager / _mapping / _spatialHash are no longer needed —
            // CollectTargetsInRange uses Physics2D.OverlapCircleNonAlloc + EntityRef.
            // Kept as fields for any legacy callers that still reference them.
        }
        public override void DoStop()
        {
            base.DoStop();
            _cachedTargets.Clear();
        }
        public override void DoTerminate()
        {
            base.DoTerminate();
            _cachedTargets.Clear();
        }
        #endregion
        #region Public API
        public List<IEntity> GetTargetsInRange(Vector2 position, float range, SpatialLayer targetLayer, List<IEntity> results)
        {
            // Generic position-based range query via Unity Physics2D. Filters hit
            // colliders by EntityRef + targetLayer hint (Player vs Enemies).
            results.Clear();
            _tempEntitySet.Clear();
            int hitCount = Physics2D.OverlapCircleNonAlloc(position, range, _physBuffer);
            for (int i = 0; i < hitCount; i++)
            {
                var col = _physBuffer[i];
                if (col == null) continue;
                var er = col.GetComponent<EntityRef>();
                if (er == null || er.Entity == null) continue;
                if (_tempEntitySet.Contains(er.Entity)) continue;
                bool matchLayer = targetLayer switch
                {
                    SpatialLayer.Player => er.Entity is PlayerCharacter,
                    SpatialLayer.Enemies => er.Entity is EnemyCharacter,
                    _ => true,
                };
                if (!matchLayer) continue;
                _tempEntitySet.Add(er.Entity);
                results.Add(er.Entity);
            }
            return results;
        }
        public List<IEntity> GetTargetsInRange(IEntity owner, float range)
        {
            _tempTargetList.Clear();
            if (!IsStarted(owner)) return _tempTargetList;
            CollectTargetsInRange(owner, range, _tempTargetList);
            return _tempTargetList;
        }
        public void GetTargetsInRangeByLayer(IEntity owner, float range, SpatialLayer targetLayer, List<IEntity> entities)
        {
            entities.Clear();
            if (!IsStarted(owner)) return;
            CollectTargetsInRangeByLayer(owner, range, targetLayer, entities);
        }
        public Vector3 GetTargetDirection(IEntity owner, IEntity target)
        {
            if (owner?.Transform == null || target?.Transform == null)
                return Vector3.right; // Default direction
            return (target.Transform.Position.Value - owner.Transform.Position.Value).normalized;
        }
        #endregion
        #region Core Targeting Logic
        public IEntity GetTarget(IEntity owner, float range, SearchTargetType strategy)
        {
            if (!IsStarted(owner)) return null;
            // Check cache first
            if (TryGetCachedTarget(owner, strategy, range, out var cachedTarget))
            {
                return cachedTarget;
            }
            // Get all targets in range
            var targets = GetTargetsInRange(owner, range);
            if (targets.Count == 0) return null;
            // Apply strategy
            var target = ApplyTargeting(targets, strategy);
            // Cache result
            if (target != null)
            {
                CacheTarget(owner, strategy, range, target);
            }
            return target;
        }
        private IEntity ApplyTargeting(List<IEntity> targets, SearchTargetType strategy)
        {
            if (targets.Count == 0) return null;
            if (targets.Count == 1) return targets[0];
            return strategy switch
            {
                SearchTargetType.Nearest => FindNearestTarget(targets),
                SearchTargetType.Strongest => FindTargetByHealth(targets, strongest: true),
                SearchTargetType.Weakest => FindTargetByHealth(targets, strongest: false),
                SearchTargetType.Random => targets[Random.Range(0, targets.Count)],
                _ => targets[0] // Fallback
            };
        }
        private IEntity FindNearestTarget(List<IEntity> targets)
        {
            return targets[0];
        }
        private IEntity FindTargetByHealth(List<IEntity> targets, bool strongest)
        {
            IEntity bestTarget = null;
            double bestHealth = strongest ? 0 : double.MaxValue;
            foreach (var target in targets)
            {
                if (target is not ICharacter character) continue;
                double health = character.Stats.Get(StatType.HPMax).Value;
                bool isBetter = strongest ? (health > bestHealth) : (health < bestHealth);
                if (isBetter)
                {
                    bestHealth = health;
                    bestTarget = target;
                }
            }
            return bestTarget ?? targets[0];
        }
        // Reusable buffer for Physics2D.OverlapCircleNonAlloc — avoids GC alloc per query.
        private static readonly Collider2D[] _physBuffer = new Collider2D[64];

        private void CollectTargetsInRange(IEntity owner, float range, List<IEntity> results)
        {
            // Unity Physics2D query — Unity's broadphase handles the spatial lookup,
            // we filter the hit Collider2Ds by their EntityRef's entity type.
            Vector2 origin = owner.Transform.Position.Value;
            int hitCount = Physics2D.OverlapCircleNonAlloc(origin, range, _physBuffer);
            bool wantEnemies = owner is PlayerCharacter; // player → enemies; else → player
            for (int i = 0; i < hitCount; i++)
            {
                var col = _physBuffer[i];
                if (col == null) continue;
                var er = col.GetComponent<EntityRef>();
                if (er == null || er.Entity == null) continue;
                if (er.Entity == owner) continue; // skip self
                bool isEnemy = er.Entity is EnemyCharacter;
                bool isPlayer = er.Entity is PlayerCharacter;
                if (wantEnemies && isEnemy) results.Add(er.Entity);
                else if (!wantEnemies && isPlayer) results.Add(er.Entity);
            }
        }
        private void CollectTargetsInRangeByLayer(IEntity owner, float range, SpatialLayer targetLayer, List<IEntity> results)
        {
            // Delegate to the position-based Physics2D query — same filtering by EntityRef.
            GetTargetsInRange(owner.Transform.Position.Value, range, targetLayer, results);
        }
        private readonly HashSet<IEntity> _tempEntitySet = new();
        #endregion
        #region Helper Methods
        private bool IsStarted(IEntity owner)
        {
            return owner?.Transform != null;
        }
        private SpatialLayer GetTargetLayer(IEntity owner)
        {
            return owner switch
            {
                PlayerCharacter => SpatialLayer.Enemies,
                EnemyCharacter => SpatialLayer.Player,
                _ => SpatialLayer.Enemies // Default
            };
        }
        private IEntity GetPlayerIfInRange(IEntity owner, float range)
        {
            if (_player?.Transform == null) return null;
            float distance = Vector3.Distance(
                owner.Transform.Position.Value,
                _player.Transform.Position.Value);
            return distance <= range ? _player : null;
        }
        #endregion
        #region Caching System
        private struct CachedTargetResult
        {
            public IEntity target;
            public float cachedTime;
            public SearchTargetType type;
            public float range;
        }
        private bool TryGetCachedTarget(IEntity owner, SearchTargetType type, float range, out IEntity target)
        {
            target = null;
            if (!_cachedTargets.TryGetValue(owner, out var cached))
                return false;
            // Check cache validity
            bool isExpired = Time.time - cached.cachedTime > cacheExpireTime;
            bool isDifferentQuery = cached.type != type || Mathf.Abs(cached.range - range) > 0.1f;
            if (isExpired || isDifferentQuery)
                return false;
            target = cached.target;
            return target != null;
        }
        private void CacheTarget(IEntity owner, SearchTargetType type, float range, IEntity target)
        {
            _cachedTargets[owner] = new CachedTargetResult
            {
                target = target,
                cachedTime = Time.time,
                type = type,
                range = range
            };
        }
        private void CleanExpiredCache()
        {
            var keysToRemove = new List<IEntity>();
            foreach (var kvp in _cachedTargets)
            {
                if (Time.time - kvp.Value.cachedTime > cacheExpireTime)
                {
                    keysToRemove.Add(kvp.Key);
                }
            }
            foreach (var key in keysToRemove)
            {
                _cachedTargets.Remove(key);
            }
        }
        #endregion
        #region MonoBehaviour
        protected override void DoUpdate(float dt)
        {
            base.DoUpdate(dt);
            if (Time.time - _lastCacheCleanTime > cacheExpireTime * 2)
            {
                CleanExpiredCache();
                _lastCacheCleanTime = Time.time;
            }
        }
        #endregion
    }
    public enum SearchTargetType
    {
        Nearest,
        Strongest,
        Weakest,
        Random
    }
}