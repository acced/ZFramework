using System;
using TBSF.Core;

namespace TBSF.Grid
{
    /// <summary>
    /// 地形类型
    /// </summary>
    public enum HexTerrainType
    {
        Plain = 0,
        Forest = 1,
        Mountain = 2,
        Water = 3,
        Sand = 4,
        Swamp = 5
    }

    /// <summary>
    /// 六边形格子数据
    /// </summary>
    [Serializable]
    public class HexCell
    {
        public HexCoordinates Coordinates { get; }
        public float Height { get; set; }
        public HexTerrainType TerrainType { get; set; }
        public bool IsWalkable { get; set; } = true;
        public int MovementCost { get; set; } = 1;

        /// <summary>
        /// 当前占据此格的单位ID (0=空闲)
        /// 使用 int ID 而非直接引用, 避免循环依赖
        /// </summary>
        public int OccupyingUnitId { get; set; }

        public bool IsOccupied => OccupyingUnitId != 0;

        /// <summary>
        /// 附加数据 (供扩展使用, 如 buff zone / trap 等)
        /// </summary>
        public int ExtraFlags { get; set; }

        public HexCell(HexCoordinates coordinates)
        {
            Coordinates = coordinates;
        }

        public HexCell(HexCoordinates coordinates, float height, HexTerrainType terrain)
        {
            Coordinates = coordinates;
            Height = height;
            TerrainType = terrain;
            MovementCost = GetDefaultMoveCost(terrain);
            IsWalkable = terrain != HexTerrainType.Water && terrain != HexTerrainType.Mountain;
        }

        private static int GetDefaultMoveCost(HexTerrainType terrain)
        {
            switch (terrain)
            {
                case HexTerrainType.Plain: return 1;
                case HexTerrainType.Forest: return 2;
                case HexTerrainType.Sand: return 2;
                case HexTerrainType.Swamp: return 3;
                default: return 99;
            }
        }

        public override string ToString()
        {
            return $"Cell{Coordinates} H={Height:F1} T={TerrainType}";
        }
    }
}
