using SkillEditor.Data;
using SkillEditor.Runtime.Utils;

namespace SkillEditor.Runtime
{
    /// <summary>
    /// 冷却效果Spec
    /// 支持普通CD、充能CD和回合CD三种模式
    /// </summary>
    public class CooldownEffectSpec : GameplayEffectSpec
    {
        private CooldownEffectNodeData CooldownNodeData => NodeData as CooldownEffectNodeData;

        // ============ 充能CD状态 ============

        public int CurrentCharges { get; private set; }
        public int MaxCharges { get; private set; }
        public float ChargeTime { get; private set; }
        public float ChargeTimer { get; private set; }

        public bool IsChargeCooldown => CooldownNodeData?.cooldownType == CooldownType.Charge;
        public bool IsCharging => IsChargeCooldown && CurrentCharges < MaxCharges;
        public float ChargeProgress => ChargeTime > 0 ? 1f - (ChargeTimer / ChargeTime) : 1f;

        // ============ 回合CD状态 ============

        public bool IsTurnBasedCooldown => CooldownNodeData?.cooldownType == CooldownType.TurnBased;
        public int TotalCooldownTurns { get; private set; }
        public int RemainingCooldownTurns { get; private set; }

        protected override void OnInitialize()
        {
            base.OnInitialize();

            var nodeData = CooldownNodeData;
            if (nodeData == null) return;

            if (nodeData.cooldownType == CooldownType.Charge)
            {
                MaxCharges = nodeData.maxCharges;
                ChargeTime = FormulaEvaluator.EvaluateSimple(nodeData.chargeTime, 10f);
                CurrentCharges = MaxCharges;
                ChargeTimer = 0f;
            }
            else if (nodeData.cooldownType == CooldownType.TurnBased)
            {
                TotalCooldownTurns = nodeData.cooldownTurns;
                RemainingCooldownTurns = 0;
            }
        }

        public override void Execute()
        {
            if (Context == null) return;

            var nodeData = CooldownNodeData;
            if (nodeData == null) return;

            switch (nodeData.cooldownType)
            {
                case CooldownType.Normal:
                    base.Execute();
                    break;
                case CooldownType.Charge:
                    ExecuteChargeCooldown();
                    break;
                case CooldownType.TurnBased:
                    ExecuteTurnBasedCooldown();
                    break;
            }
        }

        // ============ 回合CD ============

        private void ExecuteTurnBasedCooldown()
        {
            RemainingCooldownTurns = TotalCooldownTurns;
            UpdateTurnCooldownTag();
            EnsureRegistered();
        }

        /// <summary>
        /// 由 TurnManager 通过 ITurnListener 回调触发, 每回合结束时递减
        /// </summary>
        public void OnTurnEnd()
        {
            if (!IsTurnBasedCooldown || RemainingCooldownTurns <= 0) return;

            RemainingCooldownTurns--;
            UpdateTurnCooldownTag();

            if (RemainingCooldownTurns <= 0)
            {
                IsRunning = false;
                if (Target != null)
                    Target.EffectContainer.RemoveEffect(this);
            }
        }

        private void UpdateTurnCooldownTag()
        {
            if (Target == null) return;

            if (RemainingCooldownTurns > 0)
            {
                if (!Tags.GrantedTags.IsEmpty)
                    Target.OwnedTags.AddTags(Tags.GrantedTags);
            }
            else
            {
                if (!Tags.GrantedTags.IsEmpty)
                    Target.OwnedTags.RemoveTags(Tags.GrantedTags);
            }
        }

        // ============ 充能CD ============

        private void ExecuteChargeCooldown()
        {
            if (CurrentCharges > 0)
            {
                CurrentCharges--;

                if (ChargeTimer <= 0 && CurrentCharges < MaxCharges)
                    ChargeTimer = ChargeTime;

                UpdateChargeCooldownTag();
                EnsureRegistered();
            }
        }

        private void EnsureRegistered()
        {
            var target = GetTarget();
            if (target == null) return;

            Target = target;
            IsRunning = true;

            var existingEffect = target.EffectContainer.FindEffectByNodeGuid(NodeGuid);
            if (existingEffect == null)
                target.EffectContainer.AddEffect(this);
        }

        public override void Tick(float deltaTime)
        {
            var nodeData = CooldownNodeData;
            if (nodeData == null) return;

            switch (nodeData.cooldownType)
            {
                case CooldownType.Normal:
                    base.Tick(deltaTime);
                    break;
                case CooldownType.Charge:
                    TickChargeCooldown(deltaTime);
                    break;
                case CooldownType.TurnBased:
                    // 回合CD不走时间 Tick, 由 OnTurnEnd 驱动
                    break;
            }
        }

        private void TickChargeCooldown(float deltaTime)
        {
            if (CurrentCharges < MaxCharges && ChargeTimer > 0)
            {
                ChargeTimer -= deltaTime;

                if (ChargeTimer <= 0)
                {
                    CurrentCharges++;

                    if (CurrentCharges < MaxCharges)
                        ChargeTimer = ChargeTime;
                    else
                        ChargeTimer = 0f;

                    UpdateChargeCooldownTag();
                }
            }
        }

        private void UpdateChargeCooldownTag()
        {
            if (Target == null) return;

            if (CurrentCharges <= 0)
            {
                if (!Tags.GrantedTags.IsEmpty)
                    Target.OwnedTags.AddTags(Tags.GrantedTags);
            }
            else
            {
                if (!Tags.GrantedTags.IsEmpty)
                    Target.OwnedTags.RemoveTags(Tags.GrantedTags);
            }
        }

        public override void Reset()
        {
            base.Reset();

            if (IsChargeCooldown)
            {
                CurrentCharges = MaxCharges;
                ChargeTimer = 0f;
            }

            if (IsTurnBasedCooldown)
            {
                RemainingCooldownTurns = 0;
            }
        }
    }
}
