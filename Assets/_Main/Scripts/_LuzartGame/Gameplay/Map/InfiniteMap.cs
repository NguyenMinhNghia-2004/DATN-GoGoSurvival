using UnityEngine;
namespace Luzart
{
    public class InfiniteMap : MonoBehaviour
    {
        [Header("References")]
        [Tooltip("Player transform mà camera theo dõi")]
        public Transform player;
        [Tooltip("Renderer của map background")]
        public Renderer mapRenderer;
        [Header("Material Settings")]
        [Tooltip("Tên property của texture offset trong material ")]
        public string textureProperty = "_MainTex";
        [Tooltip("Tốc độ scroll tự config nếu khác tỉ lệ blabla")]
        public float scrollSpeed = 1f;
        // Private variables
        private Material _mapMaterial;
        private Vector2 _currentOffset;
        private Vector3 _lastPlayerPosition;
        void Start()
        {
            InitializeMap();
        }
        void LateUpdate()
        {
            UpdateMapOffset();
        }
        void InitializeMap()
        {
            _mapMaterial = mapRenderer.material;
            _mapMaterial.SetTextureScale(textureProperty, Vector2.one);
            _lastPlayerPosition = player.position;
            _currentOffset = Vector2.zero;
        }
        void UpdateMapOffset()
        {
            Vector2 totalOffset = Vector2.zero;
            Vector3 playerMovement = player.position - _lastPlayerPosition;
            Vector2 movementOffset = new Vector2(playerMovement.x, playerMovement.y) * scrollSpeed;
            _currentOffset += movementOffset;
            totalOffset += _currentOffset;
            _lastPlayerPosition = player.position;
            _mapMaterial.SetTextureOffset(textureProperty, totalOffset);
        }
        // ===== CLEANUP =====
        void OnDestroy()
        {
            // Cleanup material nếu cần
            if (_mapMaterial != null)
            {
                _mapMaterial.SetTextureOffset(textureProperty, Vector2.zero);
            }
        }
    }
}
