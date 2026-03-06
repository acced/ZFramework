using System;
using System.Collections.Generic;

namespace TBSF.Turn
{
    /// <summary>
    /// 先攻值排序数据
    /// </summary>
    public struct InitiativeEntry
    {
        public int UnitId;
        public int Initiative;
        public int TeamId;

        public InitiativeEntry(int unitId, int initiative, int teamId)
        {
            UnitId = unitId;
            Initiative = initiative;
            TeamId = teamId;
        }
    }

    /// <summary>
    /// 先攻值排序系统 - 决定单位行动顺序
    /// </summary>
    public sealed class InitiativeSystem
    {
        private static readonly Comparison<InitiativeEntry> s_InitiativeComparison =
            (a, b) => b.Initiative.CompareTo(a.Initiative);

        private readonly List<InitiativeEntry> _entries = new List<InitiativeEntry>();
        private readonly List<InitiativeEntry> _turnOrder = new List<InitiativeEntry>();
        private int _currentIndex;

        public int CurrentIndex => _currentIndex;
        public int Count => _turnOrder.Count;
        public IReadOnlyList<InitiativeEntry> TurnOrder => _turnOrder;

        public void AddUnit(int unitId, int initiative, int teamId)
        {
            _entries.Add(new InitiativeEntry(unitId, initiative, teamId));
        }

        public void RemoveUnit(int unitId)
        {
            _entries.RemoveAll(e => e.UnitId == unitId);
            _turnOrder.RemoveAll(e => e.UnitId == unitId);
            if (_currentIndex >= _turnOrder.Count)
                _currentIndex = 0;
        }

        /// <summary>
        /// 按先攻值降序排列, 生成本回合行动顺序
        /// </summary>
        public void SortForNewTurn()
        {
            _turnOrder.Clear();
            _turnOrder.AddRange(_entries);
            _turnOrder.Sort(s_InitiativeComparison);
            _currentIndex = 0;
        }

        /// <summary>
        /// 获取当前行动单位ID, 返回-1表示本回合所有单位已行动
        /// </summary>
        public int GetCurrentUnitId()
        {
            if (_currentIndex < 0 || _currentIndex >= _turnOrder.Count)
                return -1;
            return _turnOrder[_currentIndex].UnitId;
        }

        /// <summary>
        /// 推进到下一个单位, 返回 false 表示回合结束
        /// </summary>
        public bool AdvanceToNext()
        {
            _currentIndex++;
            return _currentIndex < _turnOrder.Count;
        }

        public void Reset()
        {
            _currentIndex = 0;
        }

        public void Clear()
        {
            _entries.Clear();
            _turnOrder.Clear();
            _currentIndex = 0;
        }
    }
}
