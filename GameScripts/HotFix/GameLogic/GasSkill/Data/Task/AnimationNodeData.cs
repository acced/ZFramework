using System;
using System.Collections.Generic;
using UnityEngine;


namespace SkillEditor.Data
{
    /// <summary>
    /// 动画节点数据 - 用于播放动画并在时间轴上触发效果和Cue
    /// </summary>
    [Serializable]
    public class AnimationNodeData : NodeData
    {
        // ============ 动画资源 ============

        /// <summary>
        /// 动画控制器资源
        /// </summary>
        public RuntimeAnimatorController animatorController;

        // ============ 动画配置 ============

        /// <summary>
        /// 动画名称
        /// </summary>
        public string animationName = "";

        /// <summary>
        /// 动画帧数（支持公式）
        /// </summary>
        public string animationDuration = "10";

        /// <summary>
        /// 是否循环播放动画
        /// </summary>
        public bool isAnimationLooping = false;

        // ============ 时间效果配置 ============

        /// <summary>
        /// 时间效果列表（只有触发时间，没有结束时间）
        /// </summary>
        public List<TimeEffectData> timeEffects = new List<TimeEffectData>();

        // ============ 时间Cue配置 ============

        /// <summary>
        /// 时间Cue列表（有开始和结束时间，用于控制Cue生命周期）
        /// </summary>
        public List<TimeCueData> timeCues = new List<TimeCueData>();
    }
}
