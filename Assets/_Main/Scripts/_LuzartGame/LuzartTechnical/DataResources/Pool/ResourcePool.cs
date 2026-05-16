using System;
using System.Collections.Generic;
using UnityEngine;
namespace Luzart
{
    public interface IResourceDefinition
    {
        public ICostVisualResolver GetVisualResolver();
        public ETypeResourceDefinition EType { get; }
        public string DisplayName { get; }
        public Sprite GetMainImage();
    }
    public interface IResourcePool
    {
        public IResourceDefinition Definition { get; }
        public INumber Value { get; }
        void Set(double amount);
    }
    [CreateAssetMenu(menuName = "Luzart/Resources/_ResourcePool")]
    public class ResourcePool : AbstractScriptableContentSaveable, IResourcePool
    {
        private const string AMOUNT = "Amount";
        [SerializeField] ResourceDefinition _resourceDefinition;
        public ResourceDefinition ResourceDefinition => _resourceDefinition;
        IResourceDefinition IResourcePool.Definition => _resourceDefinition;
        private INumberWithSet _number;
        INumber IResourcePool.Value => TryGetINumber();
        void IResourcePool.Set(double amount)
        {
            Set(amount);
        }
        private INumber TryGetINumber()
        {
            if (_number == null)
            {
                _number = new Number();
            }
            return _number;
        }
        protected override void DoTerminate()
        {
            base.DoTerminate();
            _number.Changed -= NumberChanged;
        }
        private void Set(double newValue)
        {
            _number.Set(newValue);
        }
        public bool TryRemove(double amount)
        {
            double _value = _number.Value;
            if (_value >= amount)
            {
                Set(_value - amount);
                return true;
            }
            else
            {
                return false;
            }
        }
        public void Add(double amount)
        {
            double _value = _number.Value;
            Set(_value + amount);
        }
        #region Save & Load
        protected override IEnumerable<SaveItem> DoSave()
        {
            yield return new SaveItem(AMOUNT, _number.Value);
        }
        protected override void DoLoad(IEnumerable<SaveItem> saveItems)
        {
            foreach (var item in saveItems)
            {
                if (item.key == AMOUNT)
                {
                    if(_number == null)
                    {
                        _number = new Number(item.doubleValue);
                    }
                    else
                    {
                        _number.Set(item.doubleValue);
                    }
                }
            }
            _number.Changed -= NumberChanged;
            _number.Changed += NumberChanged;
        }
        private void NumberChanged(INumber obj)
        {
            DoSave();
        }
        #endregion
#if UNITY_EDITOR
        private void FindRelatedResourceDefinition()
        {
            string currentName = name;
            if (!currentName.StartsWith("ResourcePool_"))
            {
                Debug.LogWarning("Tên asset hiện tại không bắt đầu bằng ResourcePool_");
                return;
            }
            // Tạo tên mới bằng cách đổi prefix
            string targetName = currentName.Replace("ResourcePool_", "ResourceDefinition_");
            // Tìm asset
            string[] guids = UnityEditor.AssetDatabase.FindAssets(targetName);
            if (guids.Length == 0)
            {
                Debug.LogError($"❌ Không tìm thấy asset nào có tên: {targetName}");
                return;
            }
            // Load asset
            foreach (var guid in guids)
            {
                string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
                UnityEngine.Object foundAsset = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(path);
                if (foundAsset == null)
                {
                    Debug.LogError("❌ Load asset thất bại.");
                    continue;
                }
                if (!foundAsset.name.Equals(targetName))
                {
                    continue;
                }
                _resourceDefinition = foundAsset as ResourceDefinition;
                Debug.Log($"✅ Found: {path}");
            }
        }
#endif
    }
    public class SaveRequestResourcePool : IBroadcastData
    {
        public string resourceDefinitionId;
        public double amount;
        public double previousAmount;
    }
    [System.Serializable]
    public class ResourceReward
    {
        [SerializeField] private ResourcePool _resourcePool;
        [SerializeField] private double _amount;
        public ResourcePool ResourcePool
        {
            get { return _resourcePool; }
            set { _resourcePool = value; }
        }
        public double Amount
        {
            get { return _amount; }
            set { _amount = value; }
        }
    }
}