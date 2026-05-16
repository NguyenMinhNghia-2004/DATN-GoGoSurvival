using System.Collections.Generic;
using UnityEngine;
using Luzart;
using System;
using System.Linq;
public class RenderingManager : AbstractMonoBehaviorContent
{
    private static Mesh _sharedQuadMesh;
    public static Mesh SharedQuadMesh 
    { 
        get 
        {
            if (_sharedQuadMesh == null)
            {
                CreateSharedQuadMesh();
            }
            return _sharedQuadMesh;
        }
    }
    private struct RenderData
    {
        public Vector3 position;
        public Quaternion rotation;
        public Vector3 scale;
        public MaterialPropertyBlock propertyBlock; 
        public SortingDataRender sortingData; 
    }
    private class BatchData
    {
        public Material material;
        public SortingDataRender sortingData; // Sorting data cho batch này
        public List<RenderData> renderDataList;
        public Matrix4x4[] matrices;
        public MaterialPropertyBlock[] propertyBlocks; // Thêm array cho property blocks
        // Removed mesh field - always use SharedQuadMesh
        public BatchData()
        {
            sortingData = SortingDataRender.Default; // Default sorting
            renderDataList = new List<RenderData>();
            matrices = new Matrix4x4[MAX_INSTANCES_PER_BATCH];
            propertyBlocks = new MaterialPropertyBlock[MAX_INSTANCES_PER_BATCH];
        }
        public BatchData(SortingDataRender sorting)
        {
            sortingData = sorting;
            renderDataList = new List<RenderData>();
            matrices = new Matrix4x4[MAX_INSTANCES_PER_BATCH];
            propertyBlocks = new MaterialPropertyBlock[MAX_INSTANCES_PER_BATCH];
        }
    }
    private const int MAX_INSTANCES_PER_BATCH = 1023;
    // Dictionary group theo material + sorting layer (key = combined hash)  
    private readonly Dictionary<int, BatchData> renderBatches = new Dictionary<int, BatchData>();
    private readonly List<int> activeBatchKeys = new List<int>();
    private Camera mainCamera;
    private int GetBatchKey(Material material, SortingDataRender sortingData)
    {
        // Combine material ID và sorting hash
        int materialId = material?.GetInstanceID() ?? 0;
        int sortingHash = sortingData.GetHashCode();
        return materialId + sortingHash;
    }
    public override void DoInitialize()
    {
        base.DoInitialize();
        mainCamera = _domain.Get<Camera>("MainCamera");
        if (_sharedQuadMesh == null)
        {
            CreateSharedQuadMesh();
        }
    }
    public override void DoTerminate()
    {
        base.DoTerminate();
        ClearAll();
        // Cleanup shared mesh khi terminate
        if (_sharedQuadMesh != null)
        {
            DestroyImmediate(_sharedQuadMesh);
            _sharedQuadMesh = null;
        }
        Debug.Log("[RenderingManager] Terminated");
    }
    private void LateUpdate()
    {
        if (!_isStarted) return;
        RenderAll();
    }
    public void RegisterBatch(Material material, SortingDataRender sortingData)
    {
        if (material == null)
        {
            return;
        }
        int key = GetBatchKey(material, sortingData);
        if (!renderBatches.ContainsKey(key))
        {
            var batchData = new BatchData(sortingData);
            batchData.material = material;
            renderBatches[key] = batchData;
        }
    }
    public void AddToRenderQueue(Material material, Vector3 position, Quaternion rotation, Vector3 scale, SortingDataRender sortingData, MaterialPropertyBlock propertyBlock = null)
    {
        if (!_isStarted) return;
        int key = GetBatchKey(material, sortingData);
        BatchData batch;
        if (renderBatches.TryGetValue(key, out batch))
        {
            var renderData = new RenderData();
            renderData.position = position;
            renderData.rotation = rotation;
            renderData.scale = scale;
            renderData.sortingData = sortingData;
            renderData.propertyBlock = propertyBlock;
            batch.renderDataList.Add(renderData);
            // Thêm key vào active list n?u ch?a có
            if (!activeBatchKeys.Contains(key))
            {
                activeBatchKeys.Add(key);
            }
        }
    }
    void RenderAll()
    {
        if (!mainCamera || !_isStarted) return;
        for (int i = 0; i < activeBatchKeys.Count; i++)
        {
            int key = activeBatchKeys[i];
            BatchData batch;
            if (renderBatches.TryGetValue(key, out batch))
            {
                RenderSingleBatch(batch);
                batch.renderDataList.Clear(); // Clear sau khi render
            }
        }
        activeBatchKeys.Clear();
    }
    private int GetSortingOrderForKey(int key)
    {
        if (renderBatches.TryGetValue(key, out var batch))
        {
            return batch.sortingData.GetSortingOrder();
        }
        return 0;
    }
    private void RenderSingleBatch(BatchData batch)
    {
        var renderDataList = batch.renderDataList;
        int totalCount = renderDataList.Count;
        if (totalCount == 0) return;
        // Always use SharedQuadMesh for sprite rendering
        var meshToRender = SharedQuadMesh;
        for (int i = 0; i < totalCount; i += MAX_INSTANCES_PER_BATCH)
        {
            int batchSize = Mathf.Min(MAX_INSTANCES_PER_BATCH, totalCount - i);
            for (int j = 0; j < batchSize; j++)
            {
                var data = renderDataList[i + j];
                batch.matrices[j] = Matrix4x4.TRS(data.position, data.rotation, data.scale);
                batch.propertyBlocks[j] = data.propertyBlock;
            }
            bool hasCustomProperties = false;
            for (int j = 0; j < batchSize; j++)
            {
                if (batch.propertyBlocks[j] != null)
                {
                    hasCustomProperties = true;
                    break;
                }
            }
            if (hasCustomProperties)
            {
                for (int j = 0; j < batchSize; j++)
                {
                    Graphics.DrawMesh(
                        meshToRender,
                        batch.matrices[j],
                        batch.material,
                        0,
                        null,
                        0,
                        batch.propertyBlocks[j]
                    );
                }
            }
            else
            {
                Graphics.DrawMeshInstanced(
                    meshToRender, 
                    0, 
                    batch.material, 
                    batch.matrices, 
                    batchSize
                );
            }
        }
    }
    void ClearAll()
    {
        foreach (var batch in renderBatches.Values)
        {
            batch.renderDataList.Clear();
        }
        activeBatchKeys.Clear();
    }
    private static void CreateSharedQuadMesh()
    {
        _sharedQuadMesh = new Mesh();
        _sharedQuadMesh.name = "SharedQuadMesh";
        _sharedQuadMesh.vertices = new[]
        {
            new Vector3(-0.5f, -0.5f, 0),
            new Vector3(0.5f, -0.5f, 0),
            new Vector3(0.5f, 0.5f, 0),
            new Vector3(-0.5f, 0.5f, 0)
        };
        _sharedQuadMesh.triangles = new[] { 0, 1, 2, 0, 2, 3 };
        _sharedQuadMesh.uv = new[]
        {
            new Vector2(0, 0),
            new Vector2(1, 0),
            new Vector2(1, 1),
            new Vector2(0, 1)
        };
        _sharedQuadMesh.RecalculateNormals();
        Debug.Log("[RenderingManager] Created shared quad mesh");
    }
}