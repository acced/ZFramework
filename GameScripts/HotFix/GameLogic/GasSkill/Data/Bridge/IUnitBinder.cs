using UnityEngine;

namespace SkillEditor.Data
{
    /// <summary>
    /// 从物理碰撞体获取ASC的桥接接口
    /// 由游戏业务层实现，技能系统通过此接口解耦对具体Unit类的依赖
    /// </summary>
    public interface IUnitBinder
    {
        /// <summary>
        /// 从Collider2D获取关联的AbilitySystemComponent标识
        /// </summary>
        /// <returns>关联的GameObject，技能系统通过它获取ASC；未找到返回null</returns>
        GameObject GetOwnerFromCollider(Collider2D collider);

        /// <summary>
        /// 通过UnitId查找关联的GameObject
        /// </summary>
        GameObject FindOwnerByUnitId(int unitId);
    }
}
