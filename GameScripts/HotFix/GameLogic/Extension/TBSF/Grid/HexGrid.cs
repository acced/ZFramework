using System.Collections.Generic;
using TBSF.Core;
using UnityEngine;

namespace TBSF.Grid
{
    /// <summary>
    /// 六边形网格管理器 - 存储和查询格子数据
    /// 提供零分配和标准两套查询 API
    /// </summary>
    public sealed class HexGrid : IHexGridProvider
    {
        private readonly Dictionary<HexCoordinates, HexCell> _cells = new Dictionary<HexCoordinates, HexCell>();
        private readonly HexCoordinates[] _neighborBuffer = new HexCoordinates[6];

        private readonly Dictionary<HexCoordinates, int> _reachableCache = new Dictionary<HexCoordinates, int>();
        private readonly Queue<HexCoordinates> _bfsQueue = new Queue<HexCoordinates>();
        private readonly List<HexCoordinates> _coordBuffer = new List<HexCoordinates>();

        public int CellCount => _cells.Count;

        public IEnumerable<HexCell> AllCells => _cells.Values;

        // ============ 增删改查 ============

        public void AddCell(HexCell cell)
        {
            _cells[cell.Coordinates] = cell;
        }

        public void RemoveCell(HexCoordinates coordinates)
        {
            _cells.Remove(coordinates);
        }

        public HexCell GetCell(HexCoordinates coordinates)
        {
            _cells.TryGetValue(coordinates, out var cell);
            return cell;
        }

        public bool HasCell(HexCoordinates coordinates)
        {
            return _cells.ContainsKey(coordinates);
        }

        public HexCell GetCellFromWorldPosition(Vector3 worldPos)
        {
            var coord = HexCoordinates.FromWorldPosition(worldPos);
            return GetCell(coord);
        }

        // ============ 邻居查询 ============

        public List<HexCell> GetNeighbors(HexCoordinates coordinates)
        {
            var result = new List<HexCell>(6);
            coordinates.GetAllNeighborsNonAlloc(_neighborBuffer);
            for (int i = 0; i < 6; i++)
            {
                if (_cells.TryGetValue(_neighborBuffer[i], out var cell))
                    result.Add(cell);
            }
            return result;
        }

        public HexCell GetNeighbor(HexCoordinates coordinates, HexDirection direction)
        {
            var neighborCoord = coordinates.GetNeighbor(direction);
            _cells.TryGetValue(neighborCoord, out var cell);
            return cell;
        }

        public List<HexCell> GetWalkableNeighbors(HexCoordinates coordinates)
        {
            var result = new List<HexCell>(6);
            coordinates.GetAllNeighborsNonAlloc(_neighborBuffer);
            for (int i = 0; i < 6; i++)
            {
                if (_cells.TryGetValue(_neighborBuffer[i], out var cell) && cell.IsWalkable && !cell.IsOccupied)
                    result.Add(cell);
            }
            return result;
        }

        // ============ 范围查询 ============

        public List<HexCell> GetCellsInRange(HexCoordinates center, int range)
        {
            var result = new List<HexCell>();
            CollectCellsInRange(center, range, result);
            return result;
        }

        /// <summary>
        /// 零分配版本：结果写入 caller 提供的列表（先 Clear 再 Add）
        /// </summary>
        public void CollectCellsInRange(HexCoordinates center, int range, List<HexCell> output)
        {
            output.Clear();
            for (int dq = -range; dq <= range; dq++)
            {
                int rMin = System.Math.Max(-range, -dq - range);
                int rMax = System.Math.Min(range, -dq + range);
                for (int dr = rMin; dr <= rMax; dr++)
                {
                    var coord = new HexCoordinates(center.Q + dq, center.R + dr);
                    if (_cells.TryGetValue(coord, out var cell))
                        output.Add(cell);
                }
            }
        }

        public List<HexCell> GetCellsOnRing(HexCoordinates center, int radius)
        {
            var result = new List<HexCell>();
            CollectCellsOnRing(center, radius, result);
            return result;
        }

        public void CollectCellsOnRing(HexCoordinates center, int radius, List<HexCell> output)
        {
            output.Clear();
            if (radius <= 0)
            {
                if (_cells.TryGetValue(center, out var c))
                    output.Add(c);
                return;
            }

            var current = new HexCoordinates(center.Q - radius, center.R + radius);
            for (int dir = 0; dir < 6; dir++)
            {
                for (int step = 0; step < radius; step++)
                {
                    if (_cells.TryGetValue(current, out var cell))
                        output.Add(cell);
                    current = current.GetNeighbor((HexDirection)dir);
                }
            }
        }

        public List<HexCell> GetCellsOnLine(HexCoordinates from, HexCoordinates to)
        {
            var result = new List<HexCell>();
            CollectCellsOnLine(from, to, result);
            return result;
        }

        public void CollectCellsOnLine(HexCoordinates from, HexCoordinates to, List<HexCell> output)
        {
            output.Clear();
            int dist = HexCoordinates.Distance(from, to);
            if (dist == 0)
            {
                if (_cells.TryGetValue(from, out var c))
                    output.Add(c);
                return;
            }
            for (int i = 0; i <= dist; i++)
            {
                float t = (float)i / dist;
                float lerpQ = from.Q + (to.Q - from.Q) * t;
                float lerpR = from.R + (to.R - from.R) * t;
                float lerpS = -lerpQ - lerpR;
                var coord = CubeRound(lerpQ, lerpR, lerpS);
                if (_cells.TryGetValue(coord, out var cell))
                    output.Add(cell);
            }
        }

        // ============ 移动范围 (BFS) ============

        /// <summary>
        /// 计算从 origin 出发、最大移动点数 maxCost 内可达的格子（考虑移动代价）
        /// 返回的 Dictionary 由内部 buffer 持有，调用方应立即消费或拷贝
        /// </summary>
        public Dictionary<HexCoordinates, int> GetReachableCells(HexCoordinates origin, int maxCost)
        {
            _reachableCache.Clear();
            _bfsQueue.Clear();
            _reachableCache[origin] = 0;
            _bfsQueue.Enqueue(origin);

            while (_bfsQueue.Count > 0)
            {
                var current = _bfsQueue.Dequeue();
                int currentCost = _reachableCache[current];

                current.GetAllNeighborsNonAlloc(_neighborBuffer);
                for (int i = 0; i < 6; i++)
                {
                    var n = _neighborBuffer[i];
                    if (!_cells.TryGetValue(n, out var ncell)) continue;
                    if (!ncell.IsWalkable || ncell.IsOccupied) continue;

                    int newCost = currentCost + ncell.MovementCost;
                    if (newCost > maxCost) continue;

                    if (!_reachableCache.ContainsKey(n) || _reachableCache[n] > newCost)
                    {
                        _reachableCache[n] = newCost;
                        _bfsQueue.Enqueue(n);
                    }
                }
            }

            return _reachableCache;
        }

        public void Clear()
        {
            _cells.Clear();
        }

        private static HexCoordinates CubeRound(float fq, float fr, float fs)
        {
            int rq = Mathf.RoundToInt(fq);
            int rr = Mathf.RoundToInt(fr);
            int rs = Mathf.RoundToInt(fs);

            float dq = System.Math.Abs(rq - fq);
            float dr = System.Math.Abs(rr - fr);
            float ds = System.Math.Abs(rs - fs);

            if (dq > dr && dq > ds)
                rq = -rr - rs;
            else if (dr > ds)
                rr = -rq - rs;

            return new HexCoordinates(rq, rr);
        }
    }
}
