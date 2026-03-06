using System.Collections.Generic;

namespace TBSF.Unit
{
    /// <summary>
    /// 行动提供者接口 - 返回某单位当前可用的行动列表
    /// </summary>
    public interface IActionProvider
    {
        List<UnitAction> GetAvailableActions(TBSUnit unit);
    }
}
