using System;
using UnityEngine;
namespace Luzart
{
    public sealed class AssetBool_Constant : BaseAssetBool
    {
        [SerializeField] bool value;
        protected override bool DoGetValue()
        {
            throw new NotImplementedException();
        }
    }
}
