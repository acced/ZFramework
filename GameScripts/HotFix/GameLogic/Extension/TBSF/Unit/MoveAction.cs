using System.Collections.Generic;
using TBSF.Core;
using TBSF.Grid;
using TBSF.Pathfinding;

namespace TBSF.Unit
{
    /// <summary>
    /// 移动行动 - 沿路径移动单位到目标格子
    /// </summary>
    public sealed class MoveAction : UnitAction
    {
        public override string ActionName => "移动";
        public override int ActionPointCost => 1;

        private readonly HexGrid _grid;
        private readonly HexPathfinder _pathfinder;
        private readonly HexCoordinates _targetPosition;

        /// <summary>
        /// 实际路径 (Execute 后可读取, 用于动画)
        /// </summary>
        public List<HexCell> Path { get; private set; }

        public MoveAction(HexGrid grid, HexPathfinder pathfinder, HexCoordinates target)
        {
            _grid = grid;
            _pathfinder = pathfinder;
            _targetPosition = target;
        }

        public override bool CanExecute(TBSUnit unit)
        {
            if (!base.CanExecute(unit)) return false;

            var targetCell = _grid.GetCell(_targetPosition);
            if (targetCell == null || !targetCell.IsWalkable || targetCell.IsOccupied) return false;

            int dist = unit.Position.DistanceTo(_targetPosition);
            if (dist == 0 || dist > unit.MoveRange) return false;

            var path = _pathfinder.FindPath(unit.Position, _targetPosition);
            return path != null && _pathfinder.GetPathCost(path) <= unit.MoveRange;
        }

        protected override ActionResult OnExecute(TBSUnit unit)
        {
            Path = _pathfinder.FindPath(unit.Position, _targetPosition);
            if (Path == null)
                return ActionResult.InvalidTarget;

            float cost = _pathfinder.GetPathCost(Path);
            if (cost > unit.MoveRange)
                return ActionResult.OutOfRange;

            unit.MoveTo(_targetPosition, _grid);
            return ActionResult.Success;
        }
    }
}
