namespace TBSF.Unit
{
    /// <summary>
    /// 行动执行结果
    /// </summary>
    public enum ActionResult
    {
        Success,
        Failed,
        NotEnoughAP,
        InvalidTarget,
        OutOfRange,
        OnCooldown
    }

    /// <summary>
    /// 战棋行动基类 - Command 模式
    /// </summary>
    public abstract class UnitAction
    {
        public abstract string ActionName { get; }
        public abstract int ActionPointCost { get; }

        /// <summary>
        /// 是否可执行
        /// </summary>
        public virtual bool CanExecute(TBSUnit unit)
        {
            return unit != null && unit.IsAlive && unit.RemainingActionPoints >= ActionPointCost;
        }

        /// <summary>
        /// 执行行动
        /// </summary>
        public ActionResult Execute(TBSUnit unit)
        {
            if (!CanExecute(unit))
                return ActionResult.NotEnoughAP;

            var result = OnExecute(unit);
            if (result == ActionResult.Success)
                unit.SpendActionPoints(ActionPointCost);

            return result;
        }

        protected abstract ActionResult OnExecute(TBSUnit unit);
    }
}
