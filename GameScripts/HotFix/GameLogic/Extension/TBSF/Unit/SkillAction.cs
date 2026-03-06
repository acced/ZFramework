using TBSF.Core;
using TBSF.Grid;

namespace TBSF.Unit
{
    /// <summary>
    /// 技能行动 - 桥接 GasSkill 系统的 Adapter
    /// 将 GAS 技能封装为战棋行动, 支持六边形目标选择
    /// </summary>
    public sealed class SkillAction : UnitAction
    {
        public override string ActionName => $"技能:{SkillId}";
        public override int ActionPointCost => _actionPointCost;

        public string SkillId { get; }

        private readonly int _actionPointCost;
        private readonly HexCoordinates _targetCoord;
        private readonly HexGrid _grid;

        /// <summary>
        /// 外部注入的技能执行委托 (由 GasSkill 提供)
        /// 参数: 施法单位, 技能ID, 目标坐标
        /// 返回: 是否成功
        /// </summary>
        public delegate bool SkillExecuteDelegate(TBSUnit caster, string skillId, HexCoordinates target);
        public delegate bool SkillCanExecuteDelegate(TBSUnit caster, string skillId);

        private readonly SkillExecuteDelegate _executeDelegate;
        private readonly SkillCanExecuteDelegate _canExecuteDelegate;

        public SkillAction(
            string skillId,
            HexCoordinates target,
            HexGrid grid,
            int actionPointCost,
            SkillExecuteDelegate executeDelegate,
            SkillCanExecuteDelegate canExecuteDelegate = null)
        {
            SkillId = skillId;
            _targetCoord = target;
            _grid = grid;
            _actionPointCost = actionPointCost;
            _executeDelegate = executeDelegate;
            _canExecuteDelegate = canExecuteDelegate;
        }

        public override bool CanExecute(TBSUnit unit)
        {
            if (!base.CanExecute(unit)) return false;
            if (_canExecuteDelegate != null && !_canExecuteDelegate(unit, SkillId))
                return false;
            return true;
        }

        protected override ActionResult OnExecute(TBSUnit unit)
        {
            var targetCell = _grid.GetCell(_targetCoord);
            if (targetCell == null)
                return ActionResult.InvalidTarget;

            // 更新朝向
            if (_targetCoord != unit.Position)
                unit.Facing = HexCoordinates.GetDirection(unit.Position, _targetCoord);

            if (_executeDelegate == null)
                return ActionResult.Failed;

            return _executeDelegate(unit, SkillId, _targetCoord)
                ? ActionResult.Success
                : ActionResult.Failed;
        }
    }
}
