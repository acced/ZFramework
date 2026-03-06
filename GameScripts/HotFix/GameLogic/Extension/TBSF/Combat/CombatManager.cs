using System;
using System.Collections.Generic;
using SkillEditor.Runtime;
using TBSF.Grid;
using TBSF.Pathfinding;
using TBSF.Skill;
using TBSF.Turn;
using TBSF.Unit;

namespace TBSF.Combat
{
    /// <summary>
    /// 战斗管理器 - 协调回合系统、单位、网格和战斗逻辑
    /// </summary>
    public sealed class CombatManager : ITurnListener
    {
        private readonly HexGrid _grid;
        private readonly HexPathfinder _pathfinder;
        private readonly TurnManager _turnManager;
        private readonly Dictionary<int, TBSUnit> _units = new Dictionary<int, TBSUnit>();
        private readonly GasTurnCooldownBridge _cdBridge;

        private readonly List<TBSUnit> _teamUnitsBuffer = new List<TBSUnit>();
        private readonly HashSet<int> _aliveTeamsBuffer = new HashSet<int>();

        public CombatState State { get; private set; } = CombatState.NotStarted;
        public HexGrid Grid => _grid;
        public HexPathfinder Pathfinder => _pathfinder;
        public IReadOnlyDictionary<int, TBSUnit> Units => _units;
        public TBSUnit ActiveUnit => GetUnit(_turnManager.ActiveUnitId);

        public event Action<CombatState> OnStateChanged;
        public event Action<TBSUnit> OnUnitDefeated;
        public event Action<TBSUnit> OnActiveUnitChanged;

        public CombatManager(HexGrid grid, TurnManager turnManager = null, IPathCostCalculator costCalc = null)
        {
            _grid = grid;
            _pathfinder = new HexPathfinder(grid, costCalc);
            _turnManager = turnManager ?? TurnManager.Instance;
            _cdBridge = new GasTurnCooldownBridge();

            _turnManager.RegisterListener(this);
            _turnManager.RegisterListener(_cdBridge);
        }

        // ============ 单位管理 ============

        public void AddUnit(TBSUnit unit)
        {
            _units[unit.UnitId] = unit;
            _turnManager.Initiative.AddUnit(unit.UnitId, unit.Initiative, unit.TeamId);
            unit.OnDeath += HandleUnitDeath;
        }

        public void RemoveUnit(int unitId)
        {
            if (_units.TryGetValue(unitId, out var unit))
            {
                unit.OnDeath -= HandleUnitDeath;
                var cell = _grid.GetCell(unit.Position);
                if (cell != null && cell.OccupyingUnitId == unitId)
                    cell.OccupyingUnitId = 0;

                _turnManager.Initiative.RemoveUnit(unitId);
                _units.Remove(unitId);
            }
        }

        public TBSUnit GetUnit(int unitId)
        {
            _units.TryGetValue(unitId, out var unit);
            return unit;
        }

        public List<TBSUnit> GetTeamUnits(int teamId)
        {
            _teamUnitsBuffer.Clear();
            foreach (var unit in _units.Values)
            {
                if (unit.TeamId == teamId && unit.IsAlive)
                    _teamUnitsBuffer.Add(unit);
            }
            return _teamUnitsBuffer;
        }

        // ============ 战斗流程 ============

        public void StartCombat()
        {
            if (State != CombatState.NotStarted) return;

            SearchTargetTaskSpec.HexGridProvider = _grid;

            State = CombatState.InProgress;
            OnStateChanged?.Invoke(State);
            _turnManager.StartBattle();
        }

        public void EndActiveUnitTurn()
        {
            _turnManager.EndCurrentUnitTurn();
        }

        /// <summary>
        /// 由 TurnEnd 后外部调用, 推进到下一回合
        /// </summary>
        public void AdvanceTurn()
        {
            if (State != CombatState.InProgress) return;

            var result = CheckVictoryCondition();
            if (result != CombatState.InProgress)
            {
                State = result;
                OnStateChanged?.Invoke(State);
                _turnManager.EndBattle(result == CombatState.Victory ? 0 : 1);
                return;
            }

            _turnManager.AdvanceTurn();
        }

        // ============ 单位行动 ============

        public UnitMovement CreateUnitMovement()
        {
            return new UnitMovement(_grid, _pathfinder);
        }

        // ============ ITurnListener ============

        public void OnTurnStart(int turnNumber) { }

        public void OnTurnEnd(int turnNumber)
        {
            AdvanceTurn();
        }

        public void OnUnitTurnStart(int unitId, int turnNumber)
        {
            var unit = GetUnit(unitId);
            if (unit != null)
            {
                unit.ResetActionPoints();
                OnActiveUnitChanged?.Invoke(unit);
            }
        }

        public void OnUnitTurnEnd(int unitId, int turnNumber) { }

        // ============ 内部逻辑 ============

        private void HandleUnitDeath(TBSUnit unit)
        {
            OnUnitDefeated?.Invoke(unit);

            var cell = _grid.GetCell(unit.Position);
            if (cell != null && cell.OccupyingUnitId == unit.UnitId)
                cell.OccupyingUnitId = 0;
        }

        private CombatState CheckVictoryCondition()
        {
            _aliveTeamsBuffer.Clear();
            foreach (var unit in _units.Values)
            {
                if (unit.IsAlive)
                    _aliveTeamsBuffer.Add(unit.TeamId);
            }

            if (_aliveTeamsBuffer.Count <= 1)
            {
                if (_aliveTeamsBuffer.Count == 0) return CombatState.Draw;
                foreach (var survivingTeam in _aliveTeamsBuffer)
                    return survivingTeam == 0 ? CombatState.Victory : CombatState.Defeat;
            }

            return CombatState.InProgress;
        }

        public void Cleanup()
        {
            foreach (var unit in _units.Values)
                unit.OnDeath -= HandleUnitDeath;

            _turnManager.UnregisterListener(this);
            _turnManager.UnregisterListener(_cdBridge);
            _units.Clear();

            SearchTargetTaskSpec.HexGridProvider = null;
        }
    }
}
