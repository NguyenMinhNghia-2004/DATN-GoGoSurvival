using System;
using UnityEngine;
namespace Luzart
{
    [System.Serializable]
    public class SerializableCostCreator_CommonlyUsed : ICostCreator
    {
        [SerializeField] Modes mode;
        [SerializeField] ResourcePool resourcePool;
        [SerializeField] NumberPicker resourceAmount;
        [SerializeField] AssetCost assetCost;
        ICost ICostCreator.CreateCost(IDomain domain)
        {
            switch (mode)
            {
                case Modes.Resource:
                    return new RuntimeCost_ResourcePool(resourcePool, resourceAmount.PickNumber());
                case Modes.AssetSpecific:
                    return assetCost as ICost;
                default:
                    throw new Exception("Can create Cost because dont has this mode");
            }
        }
        public Modes ModeEditor
        {
            get { return mode; }
            set { mode = value; }
        }
        public ResourcePool ResourcePoolEditor
        {
            get { return resourcePool; }
            set { resourcePool = value; }
        }
        public NumberPicker NumberPickerEditor
        {
            get { return resourceAmount; }
            set { resourceAmount = value; }
        }
    }
    public enum Modes
    {
        Resource = 1,
        RewardedVideo = 2,
        AssetSpecific = 10,
    }
}
