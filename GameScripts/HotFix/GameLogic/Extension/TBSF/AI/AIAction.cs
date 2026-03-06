using TBSF.Core;
using TBSF.Unit;

namespace TBSF.AI
{
    /// <summary>
    /// AI 行动评估数据 - 封装一个候选行动及其评分
    /// </summary>
    public class AIAction
    {
        public enum AIActionType
        {
            Move,
            Skill,
            EndTurn
        }

        public AIActionType Type { get; set; }
        public HexCoordinates TargetPosition { get; set; }
        public string SkillId { get; set; }
        public float Score { get; set; }
        public UnitAction Action { get; set; }

        public static AIAction CreateMove(HexCoordinates target, UnitAction action)
        {
            return new AIAction
            {
                Type = AIActionType.Move,
                TargetPosition = target,
                Action = action
            };
        }

        public static AIAction CreateSkill(string skillId, HexCoordinates target, UnitAction action)
        {
            return new AIAction
            {
                Type = AIActionType.Skill,
                SkillId = skillId,
                TargetPosition = target,
                Action = action
            };
        }

        public static AIAction CreateEndTurn()
        {
            return new AIAction { Type = AIActionType.EndTurn, Score = 0 };
        }
    }
}
