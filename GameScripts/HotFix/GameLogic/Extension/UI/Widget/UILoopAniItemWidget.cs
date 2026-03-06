using UnityEngine;
using UnityEngine.UI;

namespace GameLogic
{
    /// <summary>
    /// 循环列表 Item 动画组件 (无状态、尺寸驱动版)
    /// </summary>
    public class UILoopAniItemWidget : UILoopItemWidget
    {
        #region 缓存组件
        
        private RectTransform _rectTransform;
        private CanvasGroup _canvasGroup;
        private LayoutElement _layoutElement;
        
        #endregion

        #region 内部状态

        private bool _isSizeCaptured = false;
        private Vector2 _initialSize;
        
        // 动画配置 (默认开启透明度过渡)
        private bool _useFadeEffect = true;

        #endregion

        #region 生命周期

        protected override void BindMemberProperty()
        {
            base.BindMemberProperty();
            
            _rectTransform = rectTransform;
            
            // 自动获取或添加 CanvasGroup
            if (!gameObject.TryGetComponent(out _canvasGroup))
            {
                _canvasGroup = gameObject.AddComponent<CanvasGroup>();
            }

            // 获取 LayoutElement (兼容 LayoutGroup 布局)
            _layoutElement = gameObject.GetComponent<LayoutElement>();
        }

        protected override void OnDestroy()
        {
            _rectTransform = null;
            _canvasGroup = null;
            _layoutElement = null;
            base.OnDestroy();
        }

        public override void OnRecycle()
        {
            base.OnRecycle();
            // 回收时必须重置为满状态，防止复用时大小错误
            ResetAnimationState();
        }

        #endregion

        #region 核心逻辑

        /// <summary>
        /// 捕获初始尺寸
        /// 必须在 Item 刚创建且未被压缩前调用
        /// </summary>
        public void CaptureInitialSize()
        {
            if (_isSizeCaptured) return;
            if (_rectTransform == null) return;

            // 获取真实的尺寸 (使用 rect 而非 sizeDelta，更准确)
            float w = _rectTransform.rect.width;
            float h = _rectTransform.rect.height;

            // 只有尺寸有效时才记录
            if (w > 1 && h > 1)
            {
                _initialSize = new Vector2(w, h);
                _isSizeCaptured = true;
            }
        }

        /// <summary>
        /// 驱动动画 (每帧调用)
        /// </summary>
        /// <param name="value">0.0 (折叠) ~ 1.0 (展开)</param>
        public void SetAnimationValue(float value)
        {
            if (!_isSizeCaptured) CaptureInitialSize();
            if (LoopItem == null) return;

            value = Mathf.Clamp01(value);

            // 1. 更新物理尺寸
            UpdateSize(value);

            // 2. 更新视觉透明度
            if (_useFadeEffect && _canvasGroup != null)
            {
                _canvasGroup.alpha = value;
            }
        }

        /// <summary>
        /// 立即重置为正常显示状态
        /// </summary>
        public void ResetAnimationState()
        {
            SetAnimationValue(1.0f);
            if (_rectTransform != null) _rectTransform.localScale = Vector3.one;
        }

        private void UpdateSize(float progress)
        {
            bool isVertical = true;
            if (LoopItem.ParentListView != null)
            {
                isVertical = LoopItem.ParentListView.IsVertList;
            }

            // 计算目标宽高
            float targetWidth = isVertical ? _initialSize.x : _initialSize.x * progress;
            float targetHeight = isVertical ? _initialSize.y * progress : _initialSize.y;

            // 方案 A: 优先使用 LayoutElement (如果存在)
            if (_layoutElement != null)
            {
                _layoutElement.ignoreLayout = false;
                _layoutElement.preferredWidth = targetWidth;
                _layoutElement.minWidth = targetWidth;
                _layoutElement.preferredHeight = targetHeight;
                _layoutElement.minHeight = targetHeight;
            }
            // 方案 B: 直接设置 RectTransform (SuperScrollView 标准做法)
            else
            {
                _rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, targetWidth);
                _rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, targetHeight);
            }
        }

        #endregion
        
        #region 兼容接口
        public void SetAnimationConfig(bool useFade) => _useFadeEffect = useFade;
        // 兼容旧接口，不做操作
        public void SetAnimationType(AnimationType type) { } 
        #endregion
    }
}