using System.Collections.Generic;
using TBSF.Core;
using TBSF.Grid;

namespace TBSF.Skill
{
    /// <summary>
    /// 六边形目标解析器 - 根据形状定义计算受影响格子
    /// 提供 NonAlloc 重载以减少 GC
    /// </summary>
    public static class HexTargetResolver
    {
        [System.ThreadStatic] private static List<HexCell> s_CellBuffer;

        private static List<HexCell> CellBuffer
        {
            get
            {
                if (s_CellBuffer == null) s_CellBuffer = new List<HexCell>();
                return s_CellBuffer;
            }
        }

        /// <summary>
        /// 计算技能释放范围 (可选目标格子)
        /// </summary>
        public static List<HexCoordinates> GetCastRange(
            HexCoordinates origin, int range, IHexGridProvider grid)
        {
            var result = new List<HexCoordinates>();
            GetCastRangeNonAlloc(origin, range, grid, result);
            return result;
        }

        public static void GetCastRangeNonAlloc(
            HexCoordinates origin, int range, IHexGridProvider grid, List<HexCoordinates> output)
        {
            output.Clear();
            var cells = grid.GetCellsInRange(origin, range);
            for (int i = 0; i < cells.Count; i++)
            {
                if (cells[i].Coordinates != origin)
                    output.Add(cells[i].Coordinates);
            }
        }

        /// <summary>
        /// 根据形状定义计算受影响格子
        /// </summary>
        public static List<HexCoordinates> ResolveAffectedCells(
            HexShapeDefinition shape,
            HexCoordinates origin,
            HexCoordinates target,
            HexDirection facing,
            IHexGridProvider grid)
        {
            var result = new List<HexCoordinates>();
            ResolveAffectedCellsNonAlloc(shape, origin, target, facing, grid, result);
            return result;
        }

        public static void ResolveAffectedCellsNonAlloc(
            HexShapeDefinition shape,
            HexCoordinates origin,
            HexCoordinates target,
            HexDirection facing,
            IHexGridProvider grid,
            List<HexCoordinates> output)
        {
            output.Clear();
            if (shape == null || grid == null) return;

            switch (shape.ShapeType)
            {
                case HexShapeType.SingleCell:
                    if (grid.HasCell(target))
                        output.Add(target);
                    break;

                case HexShapeType.Area:
                    var areaCells = grid.GetCellsInRange(target, shape.Radius);
                    var centerCell = shape.CheckHeight ? grid.GetCell(target) : null;
                    for (int i = 0; i < areaCells.Count; i++)
                    {
                        var cell = areaCells[i];
                        if (!shape.IncludeSelf && cell.Coordinates == origin)
                            continue;
                        if (centerCell != null &&
                            UnityEngine.Mathf.Abs(cell.Height - centerCell.Height) > shape.MaxHeightDiff)
                            continue;
                        output.Add(cell.Coordinates);
                    }
                    break;

                case HexShapeType.Ring:
                    var ringCells = grid.GetCellsOnRing(target, shape.Radius);
                    for (int i = 0; i < ringCells.Count; i++)
                        output.Add(ringCells[i].Coordinates);
                    break;

                case HexShapeType.Line:
                    var dir = HexCoordinates.GetDirection(origin, target);
                    var lineEnd = HexUtils.Step(origin, dir, shape.LineLength);
                    var lineCells = grid.GetCellsOnLine(origin, lineEnd);
                    for (int i = 0; i < lineCells.Count; i++)
                    {
                        if (!shape.IncludeSelf && lineCells[i].Coordinates == origin)
                            continue;
                        output.Add(lineCells[i].Coordinates);
                    }
                    break;

                case HexShapeType.Sector:
                    var sectorCells = HexUtils.GetSector(origin, facing, shape.SectorSpread, shape.Radius);
                    for (int i = 0; i < sectorCells.Count; i++)
                    {
                        if (grid.HasCell(sectorCells[i]))
                            output.Add(sectorCells[i]);
                    }
                    break;
            }
        }
    }
}
