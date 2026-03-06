using TBSF.Unit;

namespace TBSF.AI
{
    /// <summary>
    /// 效用评分接口 - Strategy 模式, 可替换不同评分策略
    /// </summary>
    public interface IUtilityScorer
    {
        /// <summary>
        /// 为一个行动打分 (0-100, 越高越优)
        /// </summary>
        float ScoreAction(AIAction action, TBSUnit unit);
    }
}
