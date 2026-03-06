using TBSF.Combat;
using TBSF.Core;
using TBSF.Unit;

namespace TBSF.AI
{
    /// <summary>
    /// 默认效用评分器 - 基于简单启发式规则
    /// </summary>
    public sealed class SimpleUtilityScorer : IUtilityScorer
    {
        private readonly CombatManager _combat;

        public SimpleUtilityScorer(CombatManager combat)
        {
            _combat = combat;
        }

        public float ScoreAction(AIAction action, TBSUnit unit)
        {
            switch (action.Type)
            {
                case AIAction.AIActionType.Move:
                    return ScoreMove(action, unit);
                case AIAction.AIActionType.Skill:
                    return ScoreSkill(action, unit);
                case AIAction.AIActionType.EndTurn:
                    return 1f;
                default:
                    return 0f;
            }
        }

        private float ScoreMove(AIAction action, TBSUnit unit)
        {
            float score = 10f;

            var nearestEnemy = FindNearestEnemy(unit);
            if (nearestEnemy == null) return score;

            int currentDist = unit.Position.DistanceTo(nearestEnemy.Position);
            int newDist = action.TargetPosition.DistanceTo(nearestEnemy.Position);

            if (unit.AttackRange >= newDist)
                score += 30f;
            else if (newDist < currentDist)
                score += (currentDist - newDist) * 5f;

            return score;
        }

        private float ScoreSkill(AIAction action, TBSUnit unit)
        {
            float score = 40f;

            var targetCell = _combat.Grid.GetCell(action.TargetPosition);
            if (targetCell != null && targetCell.IsOccupied)
            {
                var targetUnit = _combat.GetUnit(targetCell.OccupyingUnitId);
                if (targetUnit != null && targetUnit.TeamId != unit.TeamId)
                {
                    score += 30f;
                    float hpRatio = (float)targetUnit.CurrentHealth / targetUnit.MaxHealth;
                    if (hpRatio < 0.3f) score += 20f;
                }
            }

            return score;
        }

        private TBSUnit FindNearestEnemy(TBSUnit unit)
        {
            TBSUnit nearest = null;
            int minDist = int.MaxValue;

            foreach (var other in _combat.Units.Values)
            {
                if (other.TeamId == unit.TeamId || !other.IsAlive) continue;
                int dist = unit.Position.DistanceTo(other.Position);
                if (dist < minDist)
                {
                    minDist = dist;
                    nearest = other;
                }
            }

            return nearest;
        }
    }
}
