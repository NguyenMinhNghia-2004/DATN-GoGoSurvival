using System;
using UnityEngine;
namespace Luzart
{
    [System.Serializable]
    public class AssetCost : AbstractScriptableContent, ICost
    {
        [SerializeField] private BaseAssetBool assetBool;
        IBool ICost.IsSpendable => assetBool;
        ICostVisualResolver ICost.VisualResolver => DoGetVisualResolver();
        bool ICost.TrySpend()
        {
            return DoTrySpend();
        }
        protected virtual bool DoTrySpend()
        {
            return false;
        }
        protected virtual ICostVisualResolver DoGetVisualResolver()
        {
            return null;
        }
    }
}
