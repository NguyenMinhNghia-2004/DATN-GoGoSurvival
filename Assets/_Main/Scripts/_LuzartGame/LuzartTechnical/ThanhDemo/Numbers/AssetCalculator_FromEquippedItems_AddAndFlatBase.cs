using System.Collections.Generic;
using UnityEngine;
namespace Luzart
{
    [CreateAssetMenu(fileName = "New FromEquippedItems Calculator", menuName = "ThanhDemo/Calculators/From Equipped Items")]
    // assume sum all numbers of equipments
    public sealed class AssetCalculator_FromEquippedItems_AddAndFlatBase : AssetCalculator
    {
        [SerializeField] AssetStatDefinition statDefinition;
        protected override double DoCalculate(double x)
        {
            //// buoc 1: prepare all 
            //var allActiveSkillsHPBoost = new RuntimeNumber_DynamicAggregation(Mode.Sum, 0);
            //var ingameFinalMaxHP = new RuntimeNumber_Aggregate(pregameMaxHP, allActiveSkillsHPBoost, ...);
            //// buoc 2: initialize all in-game skills
            //foreach(var skill in intializedSkills)
            //{
            //    var hpBoost = skill.Boosters.FirstOrDefault(b => b.StatDefinition == MaxHPStatDef);
            //    if(hpBoost!=null)
            //    {
            //        allActiveSkillsHPBoost.Include(hpBoost.Value);
            //    }
            //}
            double baseValue = x;
            double result = 0;
            //List<ItemConfig> list = equipment.ItemConfigsCurrent;
            //for (int i = 0; i < list.Count; i++)
            //{
            //    ItemConfig item = list[i];
            //    int level = equipment.GetEquippedItemLevel(item.TypeItem);
            //    var upgradeStats = item.GetUpgradeItemConfig(level);
            //    var cal = upgradeStats.StatCalculator;
            //    for (int j = 0; j < cal.Count; j++)
            //    {
            //        var assetCalculator = cal[j];
            //        IStatDefinition iStatDefinition = statDefinition;
            //        if (assetCalculator.Definition == iStatDefinition)
            //        {
            //            if(assetCalculator.ModeCalculation == ModeCalculation.Add || assetCalculator.ModeCalculation == ModeCalculation.MultiBase)
            //            {
            //                var value = GetValueCalculator(assetCalculator, baseValue);
            //                result += value;
            //            }
            //        }
            //    }
            //}
            return result;
        }
        private double GetValueCalculator(ICalculator calculator, double baseValue)
        {
            return calculator.Calculate(baseValue);
        }
    }
}
