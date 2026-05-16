using UnityEngine;
namespace Luzart
{
    public enum SortingLayerRender
    {
        Background = 0,
        Ground = 100,
        Items = 200,
        Player = 300,
        Enemies = 400,
        Projectiles = 500,
        Effects = 600,
        UI = 700
    }
    [System.Serializable]
    public struct SortingDataRender : System.IEquatable<SortingDataRender>
    {
        public SortingLayerRender layer;
        public int orderInLayer;
        public int GetSortingOrder()
        {
            return (int)layer + orderInLayer;
        }
        public SortingDataRender(SortingLayerRender layer, int orderInLayer = 0)
        {
            this.layer = layer;
            this.orderInLayer = orderInLayer;
        }
        public bool Equals(SortingDataRender other)
        {
            return layer == other.layer && orderInLayer == other.orderInLayer;
        }
        public override bool Equals(object obj)
        {
            return obj is SortingDataRender other && Equals(other);
        }
        public override int GetHashCode()
        {
            return ((int)layer * 1000) + orderInLayer;
        }
        public static SortingDataRender Default => new SortingDataRender(SortingLayerRender.Ground, 0);
    }
}