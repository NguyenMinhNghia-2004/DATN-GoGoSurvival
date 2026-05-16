using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
namespace Luzart
{
    /// <summary>
    /// Spatial Layer definitions for different entity types
    /// </summary>
    public enum SpatialLayer
    {
        Enemies = 0,
        Projectiles_Player = 1,
        Projectiles_Enemy = 2,
        Player = 3,
        Drop = 4
        //Obstacles = 4,
    }
    /// <summary>
    /// Public struct for cell information
    /// </summary>
    public struct CellInfo
    {
        public int startIndex;
        public int count;
    }
    /// <summary>
    /// Interface for spatial hash systems
    /// </summary>
    public interface ISpatialHashSystem
    {
        float CellSize { get; }
        void Initialize();
        // 2-phase update cycle - Now works with ICollider
        void BeginFrame();
        void Feed(SpatialLayer layer, ICollider collider);
        void EndFrame();
        void GetNearby(SpatialLayer layer, Vector2 position, float radius, List<ICollider> results);
        ICollider GetFirstHit(SpatialLayer layer, Vector2 position, float radius);
    }
    /// <summary>
    /// Main spatial hash coordinator - manages both implementations
    /// Now works with ICollider instead of IEntity
    /// </summary>
    public class SpatialHashSystem
    {
        [Header("Spatial Hash Configuration")]
        private float cellSize = 8f;
        // IContent implementation
        private string _id;
        private ISpatialHashSystem _activeImplementation;
        // IContent lifecycle management
        private bool _isInitialized = false;
        private bool _isStarted = false;
        public string Id => _id ??= GetType().Name;
        public IDomain MyDomain { get; private set; }
        public SpatialHashSystem()
        {
            _activeImplementation = new ManagedSpatialHashSystem(cellSize);
        }
        public void Initialize()
        {
            if (_isInitialized) return;
            _activeImplementation.Initialize();
            _isInitialized = true;
        }
        public void BeginFrame() => _activeImplementation?.BeginFrame();
        public void Feed(SpatialLayer layer, ICollider collider) => _activeImplementation?.Feed(layer, collider);
        public void EndFrame() => _activeImplementation?.EndFrame();
        public void GetNearby(SpatialLayer layer, Vector2 position, float radius, List<ICollider> results)
            => _activeImplementation?.GetNearby(layer, position, radius, results);
        public ICollider GetFirstHit(SpatialLayer layer, Vector2 position, float radius)
            => _activeImplementation?.GetFirstHit(layer, position, radius);
        public float GetCellSize() => cellSize;
    }
    public class ManagedSpatialHashSystem : ISpatialHashSystem
    {
        // Optimized: Use arrays instead of dictionaries for layers (fixed enum size)
        private readonly Dictionary<long, List<ICollider>>[] layerCells;
        private readonly List<ICollider>[] frameColliders;
        // Enhanced object pooling for GetNearby spam protection
        private readonly Queue<List<ICollider>> resultPoolQueue;
        private readonly List<ICollider> tempResults;
        private readonly List<long> tempCellKeys;
        private readonly HashSet<long> processedCells;
        // Cache for frequently accessed cells to avoid dictionary lookups
        private readonly Dictionary<long, List<ICollider>>[] cellCache;
        private readonly bool[] cellCacheValid;
        private float _cellSize;
        private string _id;
        public string Id => _id;
        public IDomain MyDomain { get; private set; }
        float ISpatialHashSystem.CellSize => _cellSize;
        // Query optimization caches
        private struct QueryCache
        {
            public Vector2 lastPosition;
            public float lastRadius;
            public SpatialLayer lastLayer;
            public List<long> cachedCellKeys;
            public bool isValid;
        }
        private QueryCache queryCache;
        // Cache frequently used values
        private readonly float _cellSizeInverse;
        private readonly int layerCount;
        // Spatial optimization constants
        private const int MAX_CACHED_CELLS_PER_LAYER = 64;
        private const float CACHE_TOLERANCE = 0.1f; // Position tolerance for cache reuse
        private const int RESULT_POOL_SIZE = 8;
        private bool isInitialized = false;
        private bool isStarted = false;
        public ManagedSpatialHashSystem(float cellSize, string id = "ManagedSpatialHash")
        {
            _id = id;
            _cellSize = cellSize;
            _cellSizeInverse = 1f / cellSize;
            layerCount = System.Enum.GetValues(typeof(SpatialLayer)).Length;
            layerCells = new Dictionary<long, List<ICollider>>[layerCount];
            frameColliders = new List<ICollider>[layerCount];
            cellCache = new Dictionary<long, List<ICollider>>[layerCount];
            cellCacheValid = new bool[layerCount];
            // Initialize arrays
            for (int i = 0; i < layerCount; i++)
            {
                layerCells[i] = new Dictionary<long, List<ICollider>>();
                frameColliders[i] = new List<ICollider>();
                cellCache[i] = new Dictionary<long, List<ICollider>>(MAX_CACHED_CELLS_PER_LAYER);
            }
            // Enhanced object pooling for GetNearby spam
            resultPoolQueue = new Queue<List<ICollider>>(RESULT_POOL_SIZE);
            for (int i = 0; i < RESULT_POOL_SIZE; i++)
            {
                resultPoolQueue.Enqueue(new List<ICollider>(256));
            }
            tempResults = new List<ICollider>(256);
            tempCellKeys = new List<long>(16);
            processedCells = new HashSet<long>();
            // Initialize query cache
            queryCache = new QueryCache
            {
                cachedCellKeys = new List<long>(16),
                isValid = false
            };
        }
        void ISpatialHashSystem.Initialize()
        {
            if (isInitialized) return;
            isInitialized = true;
            isStarted = true;
        }
        void ISpatialHashSystem.BeginFrame()
        {
            if (!isStarted) return;
            // Clear frame colliders efficiently
            for (int i = 0; i < layerCount; i++)
            {
                frameColliders[i].Clear();
                cellCacheValid[i] = false; // Invalidate cache
            }
            // Invalidate query cache
            queryCache.isValid = false;
        }
        void ISpatialHashSystem.Feed(SpatialLayer layer, ICollider collider)
        {
            if (!isStarted || collider == null) return;
            frameColliders[(int)layer].Add(collider);
        }
        void ISpatialHashSystem.EndFrame()
        {
            if (!isStarted) return;
            // Clear all cells efficiently
            for (int layerIndex = 0; layerIndex < layerCount; layerIndex++)
            {
                var cellDict = layerCells[layerIndex];
                foreach (var cellList in cellDict.Values)
                {
                    cellList.Clear();
                }
                // Clear cell cache
                cellCache[layerIndex].Clear();
                cellCacheValid[layerIndex] = false;
            }
            // Rebuild spatial structure from frame data
            for (int layerIndex = 0; layerIndex < layerCount; layerIndex++)
            {
                var colliders = frameColliders[layerIndex];
                var cellDict = layerCells[layerIndex];
                var cache = cellCache[layerIndex];
                // Process all colliders in this layer
                for (int i = 0; i < colliders.Count; i++)
                {
                    var collider = colliders[i];
                    long cellKey = GetCellKeyFast(collider.Position2D);
                    if (!cellDict.TryGetValue(cellKey, out var cellList))
                    {
                        cellList = new List<ICollider>(8);
                        cellDict[cellKey] = cellList;
                    }
                    cellList.Add(collider);
                    // Add to cache if not full
                    if (cache.Count < MAX_CACHED_CELLS_PER_LAYER && !cache.ContainsKey(cellKey))
                    {
                        cache[cellKey] = cellList;
                    }
                }
                cellCacheValid[layerIndex] = true;
            }
        }
        // HEAVILY OPTIMIZED GetNearby for spam scenarios
        void ISpatialHashSystem.GetNearby(SpatialLayer layer, Vector2 position, float radius, List<ICollider> results)
        {
            if (!isStarted)
            {
                return;
            }
            results.Clear();
            int layerIndex = (int)layer;
            // Check if we can reuse query cache
            bool canUseCache = queryCache.isValid &&
                             queryCache.lastLayer == layer &&
                             math.abs(queryCache.lastRadius - radius) < CACHE_TOLERANCE &&
                             math.distance(queryCache.lastPosition, position) < CACHE_TOLERANCE;
            List<long> cellKeys;
            if (canUseCache)
            {
                cellKeys = queryCache.cachedCellKeys;
            }
            else
            {
                GetRelevantCellsUltraFast(position, radius);
                cellKeys = tempCellKeys;
                // Update query cache
                queryCache.lastPosition = position;
                queryCache.lastRadius = radius;
                queryCache.lastLayer = layer;
                queryCache.cachedCellKeys.Clear();
                queryCache.cachedCellKeys.AddRange(tempCellKeys);
                queryCache.isValid = true;
            }
            // Use cache when possible, fallback to main dictionary
            var cellDict = cellCacheValid[layerIndex] ? cellCache[layerIndex] : layerCells[layerIndex];
            var mainDict = layerCells[layerIndex];
            float radiusSqr = radius * radius;
            float posX = position.x;
            float posY = position.y;
            // Ultra-fast cell processing with manual loop unrolling for small counts
            int cellCount = cellKeys.Count;
            if (cellCount <= 4) // Manual unroll for small queries
            {
                for (int i = 0; i < cellCount; i++)
                {
                    long cellKey = cellKeys[i];
                    List<ICollider> cellList = null;
                    // Try cache first, then main dict
                    if (!cellDict.TryGetValue(cellKey, out cellList))
                    {
                        mainDict.TryGetValue(cellKey, out cellList);
                    }
                    if (cellList != null)
                    {
                        ProcessCellListInline(cellList, results, posX, posY, radiusSqr);
                    }
                }
            }
            else // Standard loop for larger queries
            {
                for (int i = 0; i < cellCount; i++)
                {
                    long cellKey = cellKeys[i];
                    List<ICollider> cellList = null;
                    if (!cellDict.TryGetValue(cellKey, out cellList))
                    {
                        mainDict.TryGetValue(cellKey, out cellList);
                    }
                    if (cellList != null)
                    {
                        ProcessCellListInline(cellList, results, posX, posY, radiusSqr);
                    }
                }
            }
        }
        // Inline processing method to avoid method call overhead
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        private void ProcessCellListInline(List<ICollider> cellList, List<ICollider> results, float posX, float posY, float radiusSqr)
        {
            int count = cellList.Count;
            for (int j = 0; j < count; j++)
            {
                var collider = cellList[j];
                var colliderPos = collider.Position2D;
                float deltaX = colliderPos.x - posX;
                float deltaY = colliderPos.y - posY;
                float distSqr = deltaX * deltaX + deltaY * deltaY;
                if (distSqr <= radiusSqr)
                {
                    results.Add(collider);
                }
            }
        }
        ICollider ISpatialHashSystem.GetFirstHit(SpatialLayer layer, Vector2 position, float radius)
        {
            if (!isStarted) return null;
            GetRelevantCellsUltraFast(position, radius);
            var cellDict = layerCells[(int)layer];
            float radiusSqr = radius * radius;
            float posX = position.x;
            float posY = position.y;
            // Optimized first hit with early exit
            for (int i = 0; i < tempCellKeys.Count; i++)
            {
                long cellKey = tempCellKeys[i];
                if (cellDict.TryGetValue(cellKey, out var cellList))
                {
                    for (int j = 0; j < cellList.Count; j++)
                    {
                        var collider = cellList[j];
                        var colliderPos = collider.Position2D;
                        float deltaX = colliderPos.x - posX;
                        float deltaY = colliderPos.y - posY;
                        float distSqr = deltaX * deltaX + deltaY * deltaY;
                        if (distSqr <= radiusSqr)
                        {
                            return collider;
                        }
                    }
                }
            }
            return null;
        }
        // Ultra-fast cell calculation for spam scenarios
        private void GetRelevantCellsUltraFast(Vector2 position, float radius)
        {
            tempCellKeys.Clear();
            // Hyper-optimized cell calculation
            float radiusCells = radius * _cellSizeInverse;
            int cellRadius = (int)math.ceil(radiusCells);
            int centerX = (int)math.floor(position.x * _cellSizeInverse);
            int centerY = (int)math.floor(position.y * _cellSizeInverse);
            // For small radius, use manual unrolling
            if (cellRadius <= 2)
            {
                // Manual unroll for common small radius cases
                for (int x = -cellRadius; x <= cellRadius; x++)
                {
                    for (int y = -cellRadius; y <= cellRadius; y++)
                    {
                        tempCellKeys.Add(GetCellKeyFast(centerX + x, centerY + y));
                    }
                }
            }
            else
            {
                // Standard processing for larger radius
                processedCells.Clear();
                int cellRadiusSqr = cellRadius * cellRadius + 1;
                for (int x = -cellRadius; x <= cellRadius; x++)
                {
                    for (int y = -cellRadius; y <= cellRadius; y++)
                    {
                        int distSqr = x * x + y * y;
                        if (distSqr <= cellRadiusSqr)
                        {
                            long cellKey = GetCellKeyFast(centerX + x, centerY + y);
                            if (processedCells.Add(cellKey))
                            {
                                tempCellKeys.Add(cellKey);
                            }
                        }
                    }
                }
            }
        }
        // Ultra-optimized cell key calculation
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        private long GetCellKeyFast(Vector2 position)
        {
            int x = (int)math.floor(position.x * _cellSizeInverse);
            int y = (int)math.floor(position.y * _cellSizeInverse);
            return ((long)x << 32) | (uint)y;
        }
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        private long GetCellKeyFast(int x, int y)
        {
            return ((long)x << 32) | (uint)y;
        }
    }
}