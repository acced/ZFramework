using System;
using System.Collections.Generic;

namespace TBSF.Turn
{
    /// <summary>
    /// 回合管理器 - 控制回合流转、阶段切换、事件广播
    /// </summary>
    public sealed class TurnManager
    {
#if UNITY_EDITOR
        [UnityEngine.RuntimeInitializeOnLoadMethod(UnityEngine.RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void DomainReloadReset()
        {
            _instance = null;
        }
#endif

        private static TurnManager _instance;
        public static TurnManager Instance => _instance ??= new TurnManager();

        public int CurrentTurn { get; private set; }
        public TurnPhase CurrentPhase { get; private set; } = TurnPhase.Idle;
        public int ActiveUnitId { get; private set; } = -1;

        public InitiativeSystem Initiative { get; } = new InitiativeSystem();

        private readonly List<ITurnListener> _listeners = new List<ITurnListener>();

        // ============ 事件 ============

        public event Action<int> OnTurnStarted;
        public event Action<int> OnTurnEnded;
        public event Action<int, int> OnUnitTurnStarted;  // unitId, turnNumber
        public event Action<int, int> OnUnitTurnEnded;
        public event Action OnBattleStarted;
        public event Action<int> OnBattleEnded; // winningTeamId

        // ============ 监听器管理 ============

        public void RegisterListener(ITurnListener listener)
        {
            if (!_listeners.Contains(listener))
                _listeners.Add(listener);
        }

        public void UnregisterListener(ITurnListener listener)
        {
            _listeners.Remove(listener);
        }

        // ============ 战斗生命周期 ============

        public void StartBattle()
        {
            CurrentTurn = 0;
            CurrentPhase = TurnPhase.Idle;
            OnBattleStarted?.Invoke();
            StartNextTurn();
        }

        public void EndBattle(int winningTeamId)
        {
            CurrentPhase = TurnPhase.BattleOver;
            ActiveUnitId = -1;
            OnBattleEnded?.Invoke(winningTeamId);
        }

        // ============ 回合流转 ============

        public void StartNextTurn()
        {
            CurrentTurn++;
            CurrentPhase = TurnPhase.TurnStart;

            Initiative.SortForNewTurn();

            foreach (var l in _listeners) l.OnTurnStart(CurrentTurn);
            OnTurnStarted?.Invoke(CurrentTurn);

            StartNextUnitTurn();
        }

        /// <summary>
        /// 开始下一个单位的行动回合
        /// </summary>
        public void StartNextUnitTurn()
        {
            int unitId = Initiative.GetCurrentUnitId();
            if (unitId < 0)
            {
                EndCurrentTurn();
                return;
            }

            CurrentPhase = TurnPhase.UnitAction;
            ActiveUnitId = unitId;

            foreach (var l in _listeners) l.OnUnitTurnStart(unitId, CurrentTurn);
            OnUnitTurnStarted?.Invoke(unitId, CurrentTurn);
        }

        /// <summary>
        /// 当前单位行动结束, 推进到下一个单位
        /// </summary>
        public void EndCurrentUnitTurn()
        {
            int unitId = ActiveUnitId;
            foreach (var l in _listeners) l.OnUnitTurnEnd(unitId, CurrentTurn);
            OnUnitTurnEnded?.Invoke(unitId, CurrentTurn);

            if (Initiative.AdvanceToNext())
                StartNextUnitTurn();
            else
                EndCurrentTurn();
        }

        private void EndCurrentTurn()
        {
            CurrentPhase = TurnPhase.TurnEnd;
            ActiveUnitId = -1;

            foreach (var l in _listeners) l.OnTurnEnd(CurrentTurn);
            OnTurnEnded?.Invoke(CurrentTurn);
        }

        /// <summary>
        /// 由外部调用, 在 TurnEnd 之后推进到下一回合
        /// 分离 EndTurn 和 StartNextTurn 允许在回合间插入逻辑
        /// </summary>
        public void AdvanceTurn()
        {
            if (CurrentPhase == TurnPhase.TurnEnd)
                StartNextTurn();
        }

        public void Reset()
        {
            CurrentTurn = 0;
            CurrentPhase = TurnPhase.Idle;
            ActiveUnitId = -1;
            Initiative.Clear();
            _listeners.Clear();
        }

        /// <summary>
        /// 供测试和重置使用
        /// </summary>
        public static void ResetInstance()
        {
            _instance?.Reset();
            _instance = null;
        }
    }
}
