using System;
using System.Collections.Generic;
using TBSF.Core;
using TBSF.Grid;
using TBSF.Turn;
using UnityEngine;

namespace TBSF.Unit
{
    /// <summary>
    /// 单位控制类型
    /// </summary>
    public enum UnitControlType
    {
        Human,
        AI
    }

    /// <summary>
    /// 战棋单位基类
    /// </summary>
    public class TBSUnit
    {
#if UNITY_EDITOR
        [UnityEngine.RuntimeInitializeOnLoadMethod(UnityEngine.RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void DomainReloadReset()
        {
            _nextId = 1;
        }
#endif

        private static int _nextId = 1;

        public int UnitId { get; }
        public string UnitName { get; set; }
        public int TeamId { get; set; }
        public UnitControlType ControlType { get; set; }

        // ============ 位置 ============
        public HexCoordinates Position { get; private set; }
        public HexDirection Facing { get; set; } = HexDirection.E;

        // ============ 属性 ============
        public int MaxHealth { get; set; } = 100;
        public int CurrentHealth { get; set; } = 100;
        public int MoveRange { get; set; } = 3;
        public int Initiative { get; set; } = 10;
        public int AttackRange { get; set; } = 1;
        public bool IsAlive => CurrentHealth > 0;

        // ============ 行动点 ============
        public int MaxActionPoints { get; set; } = 2;
        public int RemainingActionPoints { get; private set; }
        public bool HasActionsRemaining => RemainingActionPoints > 0;

        // ============ GAS 桥接 ============
        /// <summary>
        /// 关联的 GameObject (挂载 AbilitySystemComponent 等)
        /// </summary>
        public GameObject Owner { get; set; }

        /// <summary>
        /// 技能ID列表（只读视图）
        /// </summary>
        private readonly List<string> _skillIds = new List<string>();
        public IReadOnlyList<string> SkillIds => _skillIds;

        public void AddSkill(string skillId)
        {
            if (!string.IsNullOrEmpty(skillId) && !_skillIds.Contains(skillId))
                _skillIds.Add(skillId);
        }

        public void RemoveSkill(string skillId)
        {
            _skillIds.Remove(skillId);
        }

        // ============ 事件 ============
        public event Action<TBSUnit> OnDeath;
        public event Action<TBSUnit, HexCoordinates, HexCoordinates> OnMoved;

        public TBSUnit()
        {
            UnitId = _nextId++;
        }

        public TBSUnit(string name, int teamId, UnitControlType controlType) : this()
        {
            UnitName = name;
            TeamId = teamId;
            ControlType = controlType;
        }

        // ============ 位置管理 ============

        /// <summary>
        /// 放置到指定格子
        /// </summary>
        public void PlaceAt(HexCoordinates position, HexGrid grid)
        {
            var oldCell = grid.GetCell(Position);
            if (oldCell != null && oldCell.OccupyingUnitId == UnitId)
                oldCell.OccupyingUnitId = 0;

            Position = position;
            var newCell = grid.GetCell(position);
            if (newCell != null)
                newCell.OccupyingUnitId = UnitId;
        }

        /// <summary>
        /// 移动到目标格子
        /// </summary>
        public void MoveTo(HexCoordinates target, HexGrid grid)
        {
            var oldPos = Position;
            PlaceAt(target, grid);

            if (target != oldPos)
                Facing = HexCoordinates.GetDirection(oldPos, target);

            OnMoved?.Invoke(this, oldPos, target);
        }

        // ============ 行动点 ============

        public void ResetActionPoints()
        {
            RemainingActionPoints = MaxActionPoints;
        }

        public bool SpendActionPoints(int cost)
        {
            if (RemainingActionPoints < cost) return false;
            RemainingActionPoints -= cost;
            return true;
        }

        // ============ 生命值 ============

        public void TakeDamage(int damage)
        {
            CurrentHealth = Math.Max(0, CurrentHealth - damage);
            if (CurrentHealth <= 0)
                OnDeath?.Invoke(this);
        }

        public void Heal(int amount)
        {
            CurrentHealth = Math.Min(MaxHealth, CurrentHealth + amount);
        }

        public override string ToString()
        {
            return $"[Unit:{UnitId}] {UnitName} Team={TeamId} HP={CurrentHealth}/{MaxHealth} Pos={Position}";
        }

        public static void ResetIdCounter()
        {
            _nextId = 1;
        }
    }
}
