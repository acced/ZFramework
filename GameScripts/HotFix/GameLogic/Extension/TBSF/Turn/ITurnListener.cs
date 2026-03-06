namespace TBSF.Turn
{
    /// <summary>
    /// 回合事件监听接口 - Observer 模式
    /// 回合管理器与各系统 (CD/buff/AI 等) 解耦
    /// </summary>
    public interface ITurnListener
    {
        void OnTurnStart(int turnNumber);
        void OnTurnEnd(int turnNumber);
        void OnUnitTurnStart(int unitId, int turnNumber);
        void OnUnitTurnEnd(int unitId, int turnNumber);
    }
}
