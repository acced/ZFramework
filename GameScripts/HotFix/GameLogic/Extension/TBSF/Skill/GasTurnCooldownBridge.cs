using System.Collections.Generic;
using SkillEditor.Runtime;
using TBSF.Turn;

namespace TBSF.Skill
{
    /// <summary>
    /// GAS 回合冷却桥接器 - 将 TurnManager 回合事件转发给所有 CooldownEffectSpec
    /// 实现 ITurnListener, 注册到 TurnManager 后自动驱动回合CD递减
    /// 包含失效引用自动清理机制
    /// </summary>
    public sealed class GasTurnCooldownBridge : ITurnListener
    {
        private readonly List<AbilitySystemComponent> _trackedASCs = new List<AbilitySystemComponent>();
        private readonly List<AbilitySystemComponent> _staleBuffer = new List<AbilitySystemComponent>();

        public void TrackASC(AbilitySystemComponent asc)
        {
            if (asc != null && !_trackedASCs.Contains(asc))
                _trackedASCs.Add(asc);
        }

        public void UntrackASC(AbilitySystemComponent asc)
        {
            _trackedASCs.Remove(asc);
        }

        public void OnTurnStart(int turnNumber) { }

        public void OnTurnEnd(int turnNumber)
        {
            _staleBuffer.Clear();

            for (int i = 0; i < _trackedASCs.Count; i++)
            {
                var asc = _trackedASCs[i];
                if (asc == null || asc.Owner == null)
                {
                    _staleBuffer.Add(asc);
                    continue;
                }

                if (asc.EffectContainer == null) continue;

                var effects = asc.EffectContainer.GetActiveEffects();
                for (int j = 0; j < effects.Count; j++)
                {
                    if (effects[j] is CooldownEffectSpec cdSpec && cdSpec.IsTurnBasedCooldown)
                        cdSpec.OnTurnEnd();
                }
            }

            for (int i = 0; i < _staleBuffer.Count; i++)
                _trackedASCs.Remove(_staleBuffer[i]);
        }

        public void OnUnitTurnStart(int unitId, int turnNumber) { }
        public void OnUnitTurnEnd(int unitId, int turnNumber) { }

        public void Clear()
        {
            _trackedASCs.Clear();
            _staleBuffer.Clear();
        }
    }
}
