using System.Collections.Generic;
using TBSF.Core;
using TBSF.Grid;
using TBSF.Pathfinding;

namespace TBSF.Unit
{
    /// <summary>
    /// 单位移动辅助 - 计算移动范围和路径
    /// </summary>
    public sealed class UnitMovement
    {
        private readonly HexGrid _grid;
        private readonly HexPathfinder _pathfinder;
        private readonly List<HexCell> _reachableBuffer = new List<HexCell>();

        public UnitMovement(HexGrid grid, HexPathfinder pathfinder)
        {
            _grid = grid;
            _pathfinder = pathfinder;
        }

        /// <summary>
        /// 获取单位可达的所有格子 (BFS, 考虑移动代价)
        /// 返回的列表由内部 buffer 持有，调用方应立即消费或拷贝
        /// </summary>
        public List<HexCell> GetReachableCells(TBSUnit unit)
        {
            _reachableBuffer.Clear();
            var reachable = _grid.GetReachableCells(unit.Position, unit.MoveRange);
            foreach (var kvp in reachable)
            {
                if (kvp.Key == unit.Position) continue;
                var cell = _grid.GetCell(kvp.Key);
                if (cell != null)
                    _reachableBuffer.Add(cell);
            }
            return _reachableBuffer;
        }

        public List<HexCell> GetPath(TBSUnit unit, HexCoordinates target)
        {
            return _pathfinder.FindPath(unit.Position, target);
        }

        public bool CanReach(TBSUnit unit, HexCoordinates target)
        {
            var reachable = _grid.GetReachableCells(unit.Position, unit.MoveRange);
            return reachable.ContainsKey(target);
        }

        public MoveAction CreateMoveAction(HexCoordinates target)
        {
            return new MoveAction(_grid, _pathfinder, target);
        }
    }
}
