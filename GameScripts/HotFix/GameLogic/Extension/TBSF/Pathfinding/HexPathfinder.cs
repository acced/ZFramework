using System.Collections.Generic;
using TBSF.Core;
using TBSF.Grid;

namespace TBSF.Pathfinding
{
    /// <summary>
    /// A* 六边形寻路器
    /// </summary>
    public sealed class HexPathfinder
    {
        private static readonly IComparer<HexPathNode> s_NodeComparer =
            Comparer<HexPathNode>.Create((a, b) =>
            {
                int c = a.FCost.CompareTo(b.FCost);
                if (c != 0) return c;
                c = a.HCost.CompareTo(b.HCost);
                if (c != 0) return c;
                c = a.Coordinates.Q.CompareTo(b.Coordinates.Q);
                if (c != 0) return c;
                return a.Coordinates.R.CompareTo(b.Coordinates.R);
            });

        private readonly HexGrid _grid;
        private readonly IPathCostCalculator _costCalc;

        private readonly Dictionary<HexCoordinates, HexPathNode> _nodeMap = new Dictionary<HexCoordinates, HexPathNode>();
        private readonly HashSet<HexCoordinates> _closedSet = new HashSet<HexCoordinates>();
        private readonly List<HexCell> _pathBuffer = new List<HexCell>();
        private readonly HexCoordinates[] _neighborBuffer = new HexCoordinates[6];

        public HexPathfinder(HexGrid grid, IPathCostCalculator costCalculator = null)
        {
            _grid = grid;
            _costCalc = costCalculator ?? new DefaultPathCostCalculator();
        }

        /// <summary>
        /// A* 寻路, 返回从 start 到 goal 的格子路径 (含起点和终点)
        /// 找不到路径返回 null
        /// </summary>
        public List<HexCell> FindPath(HexCoordinates start, HexCoordinates goal)
        {
            var startCell = _grid.GetCell(start);
            var goalCell = _grid.GetCell(goal);
            if (startCell == null || goalCell == null)
                return null;

            _nodeMap.Clear();
            _closedSet.Clear();
            var openSet = new SortedSet<HexPathNode>(s_NodeComparer);

            var startNode = new HexPathNode(start) { GCost = 0, HCost = _costCalc.CalculateHeuristic(startCell, goalCell) };
            openSet.Add(startNode);
            _nodeMap[start] = startNode;

            while (openSet.Count > 0)
            {
                var current = openSet.Min;
                openSet.Remove(current);

                if (current.Coordinates == goal)
                    return ReconstructPath(current);

                _closedSet.Add(current.Coordinates);
                var currentCell = _grid.GetCell(current.Coordinates);

                currentCell.Coordinates.GetAllNeighborsNonAlloc(_neighborBuffer);
                for (int i = 0; i < 6; i++)
                {
                    var nCoord = _neighborBuffer[i];
                    if (_closedSet.Contains(nCoord)) continue;

                    var nCell = _grid.GetCell(nCoord);
                    if (nCell == null) continue;

                    float moveCost = _costCalc.CalculateMoveCost(currentCell, nCell);
                    if (moveCost < 0) continue;

                    float tentativeG = current.GCost + moveCost;

                    if (_nodeMap.TryGetValue(nCoord, out var existingNode))
                    {
                        if (tentativeG >= existingNode.GCost) continue;
                        openSet.Remove(existingNode);
                        existingNode.GCost = tentativeG;
                        existingNode.Parent = current;
                        openSet.Add(existingNode);
                    }
                    else
                    {
                        var newNode = new HexPathNode(nCoord)
                        {
                            GCost = tentativeG,
                            HCost = _costCalc.CalculateHeuristic(nCell, goalCell),
                            Parent = current
                        };
                        openSet.Add(newNode);
                        _nodeMap[nCoord] = newNode;
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// 计算路径总代价 (不含起点)
        /// </summary>
        public float GetPathCost(List<HexCell> path)
        {
            if (path == null || path.Count < 2) return 0f;
            float total = 0f;
            for (int i = 1; i < path.Count; i++)
            {
                total += _costCalc.CalculateMoveCost(path[i - 1], path[i]);
            }
            return total;
        }

        private List<HexCell> ReconstructPath(HexPathNode endNode)
        {
            _pathBuffer.Clear();
            var node = endNode;
            while (node != null)
            {
                var cell = _grid.GetCell(node.Coordinates);
                if (cell != null) _pathBuffer.Add(cell);
                node = node.Parent;
            }
            _pathBuffer.Reverse();
            return new List<HexCell>(_pathBuffer);
        }
    }
}
