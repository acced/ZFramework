using TBSF.Grid;

namespace TBSF.Pathfinding
{
    /// <summary>
    /// 路径代价计算接口 - Strategy 模式, 可替换不同代价策略
    /// </summary>
    public interface IPathCostCalculator
    {
        /// <summary>
        /// 计算从 from 移动到 to 的代价, 返回 -1 表示不可通行
        /// </summary>
        float CalculateMoveCost(HexCell from, HexCell to);

        /// <summary>
        /// 计算启发式估算代价 (用于 A*)
        /// </summary>
        float CalculateHeuristic(HexCell from, HexCell to);
    }

    /// <summary>
    /// 默认代价计算器 - 考虑地形代价和高度差
    /// </summary>
    public sealed class DefaultPathCostCalculator : IPathCostCalculator
    {
        public float MaxClimbHeight { get; set; } = 2f;
        public float HeightCostMultiplier { get; set; } = 1f;

        public float CalculateMoveCost(HexCell from, HexCell to)
        {
            if (!to.IsWalkable || to.IsOccupied)
                return -1f;

            float heightDiff = UnityEngine.Mathf.Abs(to.Height - from.Height);
            if (heightDiff > MaxClimbHeight)
                return -1f;

            return to.MovementCost + heightDiff * HeightCostMultiplier;
        }

        public float CalculateHeuristic(HexCell from, HexCell to)
        {
            return from.Coordinates.DistanceTo(to.Coordinates);
        }
    }
}
