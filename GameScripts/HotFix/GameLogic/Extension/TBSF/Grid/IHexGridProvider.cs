using System.Collections.Generic;
using TBSF.Core;

namespace TBSF.Grid
{
    /// <summary>
    /// 网格数据提供者接口 - 解耦网格实现与使用方
    /// GasSkill 等外部系统通过此接口访问网格, 无需直接依赖 HexGrid
    /// </summary>
    public interface IHexGridProvider
    {
        HexCell GetCell(HexCoordinates coordinates);
        bool HasCell(HexCoordinates coordinates);
        List<HexCell> GetNeighbors(HexCoordinates coordinates);
        List<HexCell> GetCellsInRange(HexCoordinates center, int range);
        List<HexCell> GetCellsOnRing(HexCoordinates center, int radius);
        List<HexCell> GetCellsOnLine(HexCoordinates from, HexCoordinates to);
    }
}
