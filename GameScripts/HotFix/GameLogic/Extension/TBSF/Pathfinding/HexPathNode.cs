using System;
using TBSF.Core;

namespace TBSF.Pathfinding
{
    /// <summary>
    /// A* 寻路节点
    /// </summary>
    public class HexPathNode : IComparable<HexPathNode>
    {
        public HexCoordinates Coordinates { get; }
        public float GCost { get; set; }
        public float HCost { get; set; }
        public float FCost => GCost + HCost;
        public HexPathNode Parent { get; set; }

        public HexPathNode(HexCoordinates coordinates)
        {
            Coordinates = coordinates;
        }

        public int CompareTo(HexPathNode other)
        {
            int compare = FCost.CompareTo(other.FCost);
            if (compare == 0)
                compare = HCost.CompareTo(other.HCost);
            return compare;
        }
    }
}
