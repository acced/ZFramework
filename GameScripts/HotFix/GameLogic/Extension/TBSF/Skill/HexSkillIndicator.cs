using System.Collections.Generic;
using TBSF.Core;
using TBSF.Visual;
using UnityEngine;

namespace TBSF.Skill
{
    /// <summary>
    /// 技能释放后格子提示 - 短暂高亮受影响区域
    /// </summary>
    public sealed class HexSkillIndicator
    {
        private readonly CellHighlighter _highlighter;
        private float _indicatorDuration = 1.5f;
        private float _currentTimer;
        private bool _isShowing;
        private List<HexCoordinates> _indicatedCells = new List<HexCoordinates>();

        public float IndicatorDuration
        {
            get => _indicatorDuration;
            set => _indicatorDuration = Mathf.Max(0.1f, value);
        }

        public HexSkillIndicator(CellHighlighter highlighter)
        {
            _highlighter = highlighter;
        }

        /// <summary>
        /// 显示技能效果区域指示 (释放后短暂闪烁)
        /// </summary>
        public void ShowIndicator(List<HexCoordinates> affectedCells)
        {
            Hide();

            _indicatedCells.Clear();
            if (affectedCells != null)
                _indicatedCells.AddRange(affectedCells);

            _highlighter.SetHighlights(_indicatedCells, HighlightType.SkillEffect);
            _currentTimer = _indicatorDuration;
            _isShowing = true;
        }

        /// <summary>
        /// 每帧更新 (外部调用)
        /// </summary>
        public void Tick(float deltaTime)
        {
            if (!_isShowing) return;

            _currentTimer -= deltaTime;
            if (_currentTimer <= 0)
                Hide();
        }

        public void Hide()
        {
            if (!_isShowing) return;

            _highlighter.ClearHighlightType(HighlightType.SkillEffect);
            _indicatedCells.Clear();
            _isShowing = false;
        }
    }
}
