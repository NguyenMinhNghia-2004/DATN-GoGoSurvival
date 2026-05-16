namespace Luzart
{
    using System.Collections;
    using System.Collections.Generic;
    using UnityEngine;
    public class SkillControllerMonoBehavior : AbstractMonoBehaviorContent, IEntityBehaviorProvider
    {
        [SerializeField] private List<ZSkillConfig> skillConfigsDefault = new();
        [SerializeField] private MappingItemForSkill mappingItemForSkill;
        [SerializeField] private List<AssetEquipmentSlot> assetEquipmentSlots = new();
        private EntityBluePrint entityBluePrint;
        private SkillControllerBehavior _skillControllerBehavior;
        void IEntityBehaviorProvider.CreateBehavior(IEntity entity)
        {
            _skillControllerBehavior = new SkillControllerBehavior(entity);
            entity.AddBehavior(_skillControllerBehavior);
        }
        public void InitEntityBluePrint(EntityBluePrint entity)
        {
            entityBluePrint = entity;
            GetAllAssetEquipmentSlot();
        }
        private void GetAllAssetEquipmentSlot()
        {
            List<ZSkillConfig> list = new();
            int length = assetEquipmentSlots.Count;
            for (int i = 0; i < length; i++)
            {
                var assetEquipment = assetEquipmentSlots[i];
                IEquipmentSlot iEquipmentSlot = assetEquipment;
                var itemConfig = iEquipmentSlot.EquippedItem;
                var skill = mappingItemForSkill.GetSkillWithItem(itemConfig);
                list.Add(skill);
            }
            _skillControllerBehavior.InitZSkillConfigs(list);
        }
    }
}