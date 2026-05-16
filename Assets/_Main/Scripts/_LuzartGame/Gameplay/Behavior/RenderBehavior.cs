using UnityEngine;
namespace Luzart
{
    /// <summary>
    /// RenderBehavior - Chỉ chịu trách nhiệm rendering
    /// Lấy transform data từ ITransform của Owner
    /// Always uses RenderingManager.SharedQuadMesh for sprites
    /// No mesh storage needed - always references SharedQuadMesh
    /// </summary>
    public class RenderBehavior : BehaviorBase
    {
        // Rendering data (không có SerializeField)
        private Material material;
        private MaterialPropertyBlock propertyBlock;
        private SortingDataRender sortingData = SortingDataRender.Default; // Thêm sorting data
        // Dependencies
        private RenderingManager _renderingManager;
        private Camera mainCamera;
        public RenderBehavior(IEntity owner) : base(owner)
        {
        }
        protected override void DoStart()
        {
            _renderingManager = MyDomain.Get<RenderingManager>();
            if (_renderingManager == null)
            {
                Debug.LogError($"[RenderBehavior] RenderingManager not found in domain for {Owner.Id}!");
                return;
            }
            mainCamera = MyDomain.Get<Camera>("MainCamera");
            if (mainCamera == null)
            {
                Debug.LogWarning("[RenderBehavior] Main camera not found!");
            }
            if (material != null)
            {
                _renderingManager.RegisterBatch(material, sortingData);
            }
        }
        protected override void DoUpdate(float dt)
        {
            AddToRenderQueue();
        }
        private void AddToRenderQueue()
        {
            if (Owner.Transform == null) return;
            // Lấy transform từ ITransform
            Vector3 position = Owner.Transform.Position.Value;
            Quaternion rotation = Owner.Transform.Rotation.Value;
            Vector3 scale = Owner.Transform.Scale.Value;
            _renderingManager.AddToRenderQueue(material, position, rotation, scale, sortingData, propertyBlock);
        }
        #region Configuration Methods (được gọi từ bên ngoài)
        /// <summary>
        /// Cấu hình material (mesh luôn là SharedQuadMesh) - Legacy method
        /// </summary>
        public void Configure(Material newMaterial)
        {
            Configure(newMaterial, SortingLayerRender.Ground, 0); // Default sorting
        }
        /// <summary>
        /// Cấu hình material với sorting layer
        /// </summary>
        public void Configure(Material newMaterial, SortingLayerRender layer, int orderInLayer = 0)
        {
            material = newMaterial;
            sortingData = new SortingDataRender(layer, orderInLayer);
        }
        /// <summary>
        /// Cập nhật material runtime - Legacy method
        /// </summary>
        public void SetMaterial(Material newMaterial)
        {
            SetMaterial(newMaterial, sortingData.layer, sortingData.orderInLayer);
        }
        /// <summary>
        /// Cập nhật material với sorting layer
        /// </summary>
        public void SetMaterial(Material newMaterial, SortingLayerRender layer, int orderInLayer = 0)
        {
            if (material != newMaterial || !sortingData.Equals(new SortingDataRender(layer, orderInLayer)))
            {
                material = newMaterial;
                sortingData = new SortingDataRender(layer, orderInLayer);
                // Re-register batch with new material and sorting
                if (_renderingManager != null)
                {
                    _renderingManager.RegisterBatch(material, sortingData);
                }
            }
        }
        /// <summary>
        /// Cập nhật chỉ sorting layer
        /// </summary>
        public void SetSortingLayer(SortingLayerRender layer, int orderInLayer = 0)
        {
            var newSortingData = new SortingDataRender(layer, orderInLayer);
            if (!sortingData.Equals(newSortingData))
            {
                sortingData = newSortingData;
                // Re-register batch với sorting mới
                if (_renderingManager != null && material != null)
                {
                    _renderingManager.RegisterBatch(material, sortingData);
                }
            }
        }
        #endregion
        #region Properties (Read-only)
        public Material Material => material;
        public MaterialPropertyBlock PropertyBlock => propertyBlock;
        public SortingDataRender SortingData => sortingData; // Thêm property mới
        // Removed Mesh property - always use RenderingManager.SharedQuadMesh directly
        #endregion
        /// <summary>
        /// Cập nhật MaterialPropertyBlock cho animation
        /// </summary>
        public void UpdatePropertyBlock(MaterialPropertyBlock newPropertyBlock)
        {
            if (newPropertyBlock != null)
            {
                propertyBlock = new MaterialPropertyBlock();
                // Copy properties từ newPropertyBlock vào propertyBlock hiện tại
                propertyBlock.Clear();
                // Unity không có API copy trực tiếp, nên ta sẽ dùng cách khác
                // Hoặc ta có thể replace reference
                propertyBlock = newPropertyBlock;
            }
        }
        /// <summary>
        /// Set property cho MaterialPropertyBlock
        /// </summary>
        public void SetProperty(string name, Vector4 value)
        {
            if(propertyBlock == null)
            {
                propertyBlock = new MaterialPropertyBlock();
            }
            propertyBlock.SetVector(name, value);
        }
        public void SetProperty(string name, float value)
        {
            if (propertyBlock == null)
            {
                propertyBlock = new MaterialPropertyBlock();
            }
            propertyBlock.SetFloat(name, value);
        }
        public void SetProperty(string name, Color value)
        {
            if (propertyBlock == null)
            {
                propertyBlock = new MaterialPropertyBlock();
            }
            propertyBlock.SetColor(name, value);
        }
        protected override void DoDestroy()
        {
            // Cleanup nếu cần
        }
    }
}