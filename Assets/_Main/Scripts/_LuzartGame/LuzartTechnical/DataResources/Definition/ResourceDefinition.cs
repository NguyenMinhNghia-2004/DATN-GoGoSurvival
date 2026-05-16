using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;
namespace Luzart
{
    [CreateAssetMenu(menuName = "Luzart/Resources/_ResourceDefinition")]
    public class ResourceDefinition : AbstractScriptableContent, IResourceDefinition
    {
        [SerializeField] ETypeResourceDefinition _eType;
        [SerializeField] string _displayName;
        [SerializeField] string _description;
        [SerializeField] Sprite _mainImage;
        [SerializeField] AssetCostVisualResolver_ResourcePool _assetCostVisualResolver;
        string IResourceDefinition.DisplayName => _displayName;
        public string Description => _description;
        ETypeResourceDefinition IResourceDefinition.EType => _eType;
        ICostVisualResolver IResourceDefinition.GetVisualResolver()
        {
            return _assetCostVisualResolver;
        }
        Sprite IResourceDefinition.GetMainImage()
        {
            return _mainImage;
        }
        public void AutoDisplayName()
        {
            _displayName = name.Replace('_', ' ');
            _description = $"This is the resource: {_displayName}";
        }
        public void AutoSpriteResourceDefinition(string namePrefix)
        {
#if UNITY_EDITOR
            if (string.IsNullOrEmpty(namePrefix))
            {
                Debug.LogError("❌ namePrefix trống.");
                return;
            }
            // Tìm tất cả sprite bắt đầu bằng prefix
            string[] guids = AssetDatabase.FindAssets($"t:Sprite {namePrefix}");
            if (guids.Length == 0)
            {
                Debug.LogWarning($"⚠️ Không tìm thấy sprite nào với prefix: {namePrefix}");
                return;
            }
            // Regex để match hậu tố _00x (3 chữ số)
            Regex regex = new Regex($@"^{namePrefix}_\d{{3}}$", RegexOptions.Compiled);
            Sprite chosen = null;
            foreach (var guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                Sprite spr = AssetDatabase.LoadAssetAtPath<Sprite>(path);
                if (spr == null)
                    continue;
                // Lấy ký tự cuối, nếu không phải số thì mặc định là '0'
                char lastChar = name[^1];
                if (!char.IsDigit(lastChar))
                {
                    lastChar = '0';
                    Debug.Log($"ℹ️ Ký tự cuối không phải số → tự động dùng '0'");
                }
                // Kiểm tra format tên sprite
                char lastCharSpr = spr.name[^1];
                if(lastCharSpr != lastChar)
                {
                    Debug.LogWarning($"⚠️ Sprite '{spr.name}' không hợp lệ với pattern: {namePrefix}_00x (yêu cầu ký tự cuối là '{lastChar}') → Bỏ qua.");
                    continue;
                }
                // Nếu trùng sprite hiện tại → Skip + log
                if (_mainImage == spr)
                {
                    Debug.LogWarning($"⚠️ Sprite '{spr.name}' đã được gán nên bỏ qua.");
                    continue;
                }
                // Chọn sprite đầu tiên đúng format và không trùng
                chosen = spr;
                break;
            }
            if (chosen == null)
            {
                Debug.LogWarning($"⚠️ Không có sprite nào hợp lệ với pattern: {namePrefix}_00x");
                return;
            }
            // Assign
            _mainImage = chosen;
            // Save
            EditorUtility.SetDirty(this);
            Debug.Log($"✅ Sprite đã gán: {chosen.name}");
#endif
        }
    }
    public enum ETypeResourceDefinition
    {
        None = 0,
        Shard = 1,
        Chest = 2,
    }
}