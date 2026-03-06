namespace TBSF.Turn
{
    /// <summary>
    /// 回合阶段
    /// </summary>
    public enum TurnPhase
    {
        /// <summary>
        /// 等待开始 (战斗未开始或已结束)
        /// </summary>
        Idle,

        /// <summary>
        /// 回合开始 (全局效果结算, buff tick 等)
        /// </summary>
        TurnStart,

        /// <summary>
        /// 单位行动阶段 (逐个单位执行行动)
        /// </summary>
        UnitAction,

        /// <summary>
        /// 回合结束 (CD递减, 状态清理等)
        /// </summary>
        TurnEnd,

        /// <summary>
        /// 战斗结束
        /// </summary>
        BattleOver
    }
}
