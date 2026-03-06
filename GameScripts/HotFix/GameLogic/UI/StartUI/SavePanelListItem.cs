using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using TEngine;
using System;
using System.Threading;
using TMPro;

namespace GameLogic
{
    /// <summary>
    /// 存档列表项。
    /// UIBindComponent 绑定顺序（与 prefab 一一对应）：
    ///   0 - Image           m_img_ItemIcon  （图标）
    ///   1 - Button          m_btn_select    （整项点击）
    ///   2 - TextMeshProUGUI m_tmp_slotLabel （存档序号/标签）
    ///   3 - TextMeshProUGUI m_tmp_location  （位置）
    ///   4 - TextMeshProUGUI m_tmp_gold      （金币）
    ///   5 - TextMeshProUGUI m_tmp_level     （等级）
    ///   6 - TextMeshProUGUI m_tmp_playTime  （游玩时长）
    ///   7 - TextMeshProUGUI m_tmp_saveTime  （存档时间）
    /// </summary>
    public partial class SavePanelListItem : UILoopAniItemWidget, IListDataItem<SaveSlotItemData>
    {
        #region 脚本工具生成的代码

        private UIBindComponent m_bindComponent;
        private TextMeshProUGUI   m_tmp_slotLabel;
        private TextMeshProUGUI   m_tmp_location;
        private TextMeshProUGUI   m_tmp_gold;
        private TextMeshProUGUI   m_tmp_level;
        private TextMeshProUGUI   m_tmp_playTime;
        private TextMeshProUGUI   m_tmp_saveTime;
        private Button m_btn_select;

        private Image m_img_ItemIcon;

        private int  m_slotId;
        private bool m_isEmpty;

        #endregion


        protected override void ScriptGenerator()
        {
            m_bindComponent = gameObject.GetComponent<UIBindComponent>();
            m_img_ItemIcon  = m_bindComponent.GetComponent<Image>(0);
            m_btn_select    = m_bindComponent.GetComponent<Button>(1);
            m_tmp_slotLabel = m_bindComponent.GetComponent<TextMeshProUGUI>(2);
            m_tmp_location  = m_bindComponent.GetComponent<TextMeshProUGUI>(3);
            m_tmp_gold      = m_bindComponent.GetComponent<TextMeshProUGUI>(4);
            m_tmp_level     = m_bindComponent.GetComponent<TextMeshProUGUI>(5);
            m_tmp_playTime  = m_bindComponent.GetComponent<TextMeshProUGUI>(6);
            m_tmp_saveTime  = m_bindComponent.GetComponent<TextMeshProUGUI>(7);
        }

        /// <summary>
        /// 使用已有的 m_btn_select 作为选中触发器，接入 SelectItemBase 机制，
        /// 避免在根节点额外创建重复 Button。
        /// </summary>
        protected override void AddSelectEvt()
        {
            if (m_btn_select != null)
                m_btn_select.onClick.AddListener(OnSelectClick);
        }

        private const float SELECT_OFFSET_X = -30f;

        /// <summary>
        /// 选中状态变化时由框架自动调用，实现向左偏移/复原效果。
        /// </summary>
        public override void UpdateSelect()
        {
            if (rectTransform == null) return;
            var pos = rectTransform.anchoredPosition;
            rectTransform.anchoredPosition = new Vector2(
                m_isSelected ? SELECT_OFFSET_X : 0f,
                pos.y);
        }

        #region 交错滑入动画

        private CancellationTokenSource _enterAnimCts;

        private const float STAGGER       = 0.06f;
        private const float MAX_STAGGER   = 0.36f;
        private const float SLIDE_DIST    = 900f;
        private const float ANIM_DURATION = 1.6f;

        protected override void OnDestroy()
        {
            CancelEnterAnim();
            base.OnDestroy();
        }

        public override void OnRecycle()
        {
            CancelEnterAnim();
            if (rectTransform != null)
            {
                var pos = rectTransform.anchoredPosition;
                rectTransform.anchoredPosition = new Vector2(0f, pos.y);
            }
            base.OnRecycle();
        }

        private void CancelEnterAnim()
        {
            _enterAnimCts?.Cancel();
            _enterAnimCts?.Dispose();
            _enterAnimCts = null;
        }

        private void PlayEnterAnimation(int index)
{
    CancelEnterAnim();
    _enterAnimCts = new CancellationTokenSource();
    RunEnterAnimAsync(index, _enterAnimCts.Token).Forget();
}

        private async UniTaskVoid RunEnterAnimAsync(int itemIndex, CancellationToken ct)
        {
            var rt = rectTransform;
            var cg = gameObject.GetComponent<CanvasGroup>();
            if (rt == null || cg == null) return;

            // 1. 确定起点 X（左右交错入场）
            float fromX = (itemIndex % 2 == 0) ? -SLIDE_DIST : SLIDE_DIST;

            rt.anchoredPosition = new Vector2(fromX, rt.anchoredPosition.y);
            cg.alpha = 0f;

            // 2. 等待交错延迟
            float delay = Mathf.Min(itemIndex * STAGGER, MAX_STAGGER);
            if (delay > 0f)
            {
                try { await UniTask.Delay(TimeSpan.FromSeconds(delay), cancellationToken: ct); }
                catch (OperationCanceledException) { return; }
            }

            float startX  = fromX;
            float elapsed = 0f;

            while (elapsed < ANIM_DURATION)
            {
                if (rt == null) return;

                float t     = Mathf.Clamp01(elapsed / ANIM_DURATION);
                float eased = EaseOutQuint(t);

                // 终点 X 需考虑当前是否处于选中状态，避免与 UpdateSelect 冲突
                float targetX = m_isSelected ? SELECT_OFFSET_X : 0f;
                rt.anchoredPosition = new Vector2(Mathf.LerpUnclamped(startX, targetX, eased), rt.anchoredPosition.y);
                cg.alpha = Mathf.Clamp01(t / 0.4f);

                elapsed += Time.deltaTime;
                try { await UniTask.Yield(PlayerLoopTiming.Update, ct); }
                catch (OperationCanceledException) { return; }
            }

            // 动画结束时最终落点同样需要尊重选中状态
            rt.anchoredPosition = new Vector2(m_isSelected ? SELECT_OFFSET_X : 0f, rt.anchoredPosition.y);
            cg.alpha = 1f;
        }

        private static float EaseOutQuint(float t) => 1f - Mathf.Pow(1f - Mathf.Clamp01(t), 5f);

        #endregion

        #region IListDataItem<SaveSlotItemData> 实现

        public void SetItemData(SaveSlotItemData data)
        {
            m_slotId  = data.slotId;
            m_isEmpty = data.isEmpty;

            SetText(m_tmp_slotLabel, data.slotLabel);

            // 空槽位：内容字段全部清空
            if (data.isEmpty)
            {
                SetText(m_tmp_location,  string.Empty);
                SetText(m_tmp_gold,      string.Empty);
                SetText(m_tmp_level,     string.Empty);
                SetText(m_tmp_playTime,  string.Empty);
                SetText(m_tmp_saveTime,  string.Empty);
            }
            else
            {
                SetText(m_tmp_location,  data.locationName);
                SetText(m_tmp_gold,      data.goldText);
                SetText(m_tmp_level,     data.levelText);
                SetText(m_tmp_playTime,  data.playTimeText);
                SetText(m_tmp_saveTime,  data.saveTimeText);
            }

            // DataId 由循环列表框架赋值，等于该条目在列表中的索引，用于错开动画
            PlayEnterAnimation(DataId);
        }

        private static void SetText(TextMeshProUGUI t, string v)
        {
            if (t != null) t.SetText(v);
        }

        #endregion
    }
}
