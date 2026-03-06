using UnityEngine;

namespace SkillEditor.Data
{
    /// <summary>
    /// GAS桥接注册中心
    /// 游戏业务层在初始化时注册具体实现，技能系统通过此类获取桥接接口
    /// </summary>
    public static class GASBridge
    {
        public static IUnitBinder UnitBinder { get; private set; }

        /// <summary>
        /// 从GameObject获取IAnimationPlayer的委托
        /// 业务层注册后，技能系统通过此委托获取动画播放器
        /// </summary>
        public static System.Func<GameObject, IAnimationPlayer> AnimationPlayerProvider { get; private set; }

        public static void RegisterUnitBinder(IUnitBinder binder)
        {
            UnitBinder = binder;
        }

        public static void RegisterAnimationPlayerProvider(System.Func<GameObject, IAnimationPlayer> provider)
        {
            AnimationPlayerProvider = provider;
        }

        public static void Clear()
        {
            UnitBinder = null;
            AnimationPlayerProvider = null;
        }
    }
}
