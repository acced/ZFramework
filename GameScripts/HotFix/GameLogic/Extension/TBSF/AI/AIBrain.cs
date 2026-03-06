using System;
using System.Collections.Generic;
using TBSF.Combat;
using TBSF.Core;
using TBSF.Grid;
using TBSF.Pathfinding;
using TBSF.Unit;

namespace TBSF.AI
{
    /// <summary>
    /// AI 决策引擎 - Utility AI 模式
    /// 枚举所有可能行动 -> 打分 -> 选择最高分执行
    /// </summary>
    public sealed class AIBrain
    {
        private static readonly Comparison<AIAction> s_ScoreComparison =
            (a, b) => b.Score.CompareTo(a.Score);

        private readonly CombatManager _combat;
        private readonly IUtilityScorer _scorer;

        private readonly List<AIAction> _candidatesBuffer = new List<AIAction>();
        private readonly List<AIAction> _executedBuffer = new List<AIAction>();
        private readonly List<TBSUnit> _enemyBuffer = new List<TBSUnit>();

        public AIBrain(CombatManager combat, IUtilityScorer scorer = null)
        {
            _combat = combat;
            _scorer = scorer ?? new SimpleUtilityScorer(combat);
        }

        /// <summary>
        /// 为指定单位决策下一个行动
        /// </summary>
        public AIAction DecideAction(TBSUnit unit)
        {
            GenerateCandidates(unit);
            if (_candidatesBuffer.Count == 0)
                return AIAction.CreateEndTurn();

            for (int i = 0; i < _candidatesBuffer.Count; i++)
                _candidatesBuffer[i].Score = _scorer.ScoreAction(_candidatesBuffer[i], unit);

            _candidatesBuffer.Sort(s_ScoreComparison);
            return _candidatesBuffer[0];
        }

        /// <summary>
        /// 执行完整的 AI 回合 (多步行动), 返回已执行的行动列表副本
        /// </summary>
        public List<AIAction> ExecuteFullTurn(TBSUnit unit)
        {
            _executedBuffer.Clear();

            while (unit.HasActionsRemaining && unit.IsAlive)
            {
                var action = DecideAction(unit);
                if (action.Type == AIAction.AIActionType.EndTurn)
                    break;

                if (action.Action != null)
                {
                    var result = action.Action.Execute(unit);
                    if (result == ActionResult.Success)
                        _executedBuffer.Add(action);
                    else
                        break;
                }
                else
                {
                    break;
                }
            }

            return new List<AIAction>(_executedBuffer);
        }

        private void GenerateCandidates(TBSUnit unit)
        {
            _candidatesBuffer.Clear();

            if (unit.RemainingActionPoints >= 1)
                GenerateMoveCandidates(unit);

            if (unit.RemainingActionPoints >= 1)
                GenerateSkillCandidates(unit);

            _candidatesBuffer.Add(AIAction.CreateEndTurn());
        }

        private void GenerateMoveCandidates(TBSUnit unit)
        {
            var movement = _combat.CreateUnitMovement();
            var reachable = movement.GetReachableCells(unit);

            for (int i = 0; i < reachable.Count; i++)
            {
                var moveAction = movement.CreateMoveAction(reachable[i].Coordinates);
                if (moveAction.CanExecute(unit))
                    _candidatesBuffer.Add(AIAction.CreateMove(reachable[i].Coordinates, moveAction));
            }
        }

        private void GenerateSkillCandidates(TBSUnit unit)
        {
            var skillIds = unit.SkillIds;
            for (int s = 0; s < skillIds.Count; s++)
            {
                var skillId = skillIds[s];
                GetEnemyUnits(unit);

                for (int e = 0; e < _enemyBuffer.Count; e++)
                {
                    var enemy = _enemyBuffer[e];
                    int dist = unit.Position.DistanceTo(enemy.Position);
                    if (dist <= unit.AttackRange)
                    {
                        var skillAction = new SkillAction(
                            skillId, enemy.Position, _combat.Grid, 1, null);

                        _candidatesBuffer.Add(AIAction.CreateSkill(skillId, enemy.Position, skillAction));
                    }
                }
            }
        }

        private void GetEnemyUnits(TBSUnit unit)
        {
            _enemyBuffer.Clear();
            foreach (var other in _combat.Units.Values)
            {
                if (other.TeamId != unit.TeamId && other.IsAlive)
                    _enemyBuffer.Add(other);
            }
        }
    }
}
