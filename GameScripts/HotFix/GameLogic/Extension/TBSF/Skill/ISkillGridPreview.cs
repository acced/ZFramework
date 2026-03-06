using System.Collections.Generic;
using TBSF.Core;

namespace TBSF.Skill
{
    /// <summary>
    /// 技能格子预览接口 - Bridge 模式, 解耦 TBSF 与 GasSkill
    /// GasSkill 侧实现此接口, TBSF 侧消费
    /// </summary>
    public interface ISkillGridPreview
    {
        /// <summary>
        /// 获取技能的形状定义
        /// </summary>
        HexShapeDefinition GetSkillShape(string skillId);

        /// <summary>
        /// 获取技能释放前的预览格子 (可选目标范围)
        /// </summary>
        List<HexCoordinates> GetPreviewCells(string skillId, HexCoordinates origin, HexDirection facing);

        /// <summary>
        /// 获取技能释放后的受影响格子 (效果区域)
        /// </summary>
        List<HexCoordinates> GetAffectedCells(string skillId, HexCoordinates origin, HexCoordinates target);
    }
}
