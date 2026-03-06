namespace GameLogic
{
    /// <summary>
    /// 列表动画类型
    /// 目前底层优化已统一为基于尺寸的缩放(Clip/Scale)，此枚举主要用于扩展或标记
    /// </summary>
    public enum AnimationType
    {
        None = 0,
        Default = 1, // 默认：尺寸伸缩 + 透明度
    }
    
    /// <summary>
    /// 缓动类型
    /// </summary>
    public enum AnimationEaseType
    {
        Linear,
        EaseOutQuad,
        EaseInQuad
    }
}