namespace SkillEditor.Data
{
    /// <summary>
    /// 动画播放桥接接口
    /// 由游戏业务层实现，技能系统通过此接口解耦对具体AnimationComponent的依赖
    /// </summary>
    public interface IAnimationPlayer
    {
        void PlayAnimation(string animationName, bool loop);
    }
}
