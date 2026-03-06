using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace GameLogic
{
    public static class UIListAnimationHelper
    {
        // 硬编码缓动公式，避免 Delegate 调用开销
        public static float EaseOutQuad(float t) => t * (2 - t);
        
        /// <summary>
        /// 通用数值补间动画 (0 GC)
        /// </summary>
        public static async UniTask TweenValueAsync(
            float start, 
            float end, 
            float duration, 
            Action<float> onUpdate, 
            CancellationToken ct = default)
        {
            float elapsed = 0f;
            
            // 确保初始状态正确
            onUpdate?.Invoke(start);

            while (elapsed < duration)
            {
                // 使用 PlayerLoopTiming.Update 确保与 Unity 渲染帧同步
                await UniTask.Yield(PlayerLoopTiming.Update, ct);
                
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                
                // 应用缓动
                float value = Mathf.Lerp(start, end, EaseOutQuad(t));
                onUpdate?.Invoke(value);
            }

            // 确保结束状态精确
            onUpdate?.Invoke(end);
        }
    }
}