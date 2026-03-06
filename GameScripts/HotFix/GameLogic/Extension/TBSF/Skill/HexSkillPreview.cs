using System.Collections.Generic;
using TBSF.Core;
using TBSF.Grid;
using TBSF.Visual;

namespace TBSF.Skill
{
    /// <summary>
    /// 技能格子范围预览 - 释放前高亮可选目标和影响范围
    /// </summary>
    public sealed class HexSkillPreview
    {
        private readonly IHexGridProvider _grid;
        private readonly CellHighlighter _highlighter;
        private ISkillGridPreview _skillPreview;

        private static readonly HexShapeDefinition s_DefaultShape = HexShapeDefinition.SingleTarget(3);

        private string _currentSkillId;
        private bool _isShowingCastRange;
        private bool _isShowingAffectedArea;

        public HexSkillPreview(IHexGridProvider grid, CellHighlighter highlighter)
        {
            _grid = grid;
            _highlighter = highlighter;
        }

        public void SetSkillPreviewProvider(ISkillGridPreview provider)
        {
            _skillPreview = provider;
        }

        /// <summary>
        /// 显示技能施法范围 (可选目标格子)
        /// </summary>
        public void ShowCastRange(string skillId, HexCoordinates origin, HexDirection facing)
        {
            HideCastRange();
            _currentSkillId = skillId;

            List<HexCoordinates> cells;
            if (_skillPreview != null)
            {
                cells = _skillPreview.GetPreviewCells(skillId, origin, facing);
            }
            else
            {
                var shape = GetDefaultShape(skillId);
                cells = HexTargetResolver.GetCastRange(origin, shape.CastRange, _grid);
            }

            if (cells != null)
            {
                _highlighter.SetHighlights(cells, HighlightType.SkillPreview);
                _isShowingCastRange = true;
            }
        }

        /// <summary>
        /// 显示技能效果区域 (选定目标后的受影响格子)
        /// </summary>
        public void ShowAffectedArea(
            string skillId, HexCoordinates origin, HexCoordinates target, HexDirection facing)
        {
            HideAffectedArea();

            List<HexCoordinates> cells;
            if (_skillPreview != null)
            {
                cells = _skillPreview.GetAffectedCells(skillId, origin, target);
            }
            else
            {
                var shape = GetDefaultShape(skillId);
                cells = HexTargetResolver.ResolveAffectedCells(shape, origin, target, facing, _grid);
            }

            if (cells != null)
            {
                _highlighter.SetHighlights(cells, HighlightType.SkillEffect);
                _isShowingAffectedArea = true;
            }
        }

        public void HideCastRange()
        {
            if (_isShowingCastRange)
            {
                _highlighter.ClearHighlightType(HighlightType.SkillPreview);
                _isShowingCastRange = false;
            }
        }

        public void HideAffectedArea()
        {
            if (_isShowingAffectedArea)
            {
                _highlighter.ClearHighlightType(HighlightType.SkillEffect);
                _isShowingAffectedArea = false;
            }
        }

        public void HideAll()
        {
            HideCastRange();
            HideAffectedArea();
            _currentSkillId = null;
        }

        private HexShapeDefinition GetDefaultShape(string skillId)
        {
            if (_skillPreview != null)
                return _skillPreview.GetSkillShape(skillId);

            return s_DefaultShape;
        }
    }
}
