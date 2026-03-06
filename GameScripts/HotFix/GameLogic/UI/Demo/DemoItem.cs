using UnityEngine;
using UnityEngine.UI;
using TEngine;

namespace GameLogic
{
    /// <summary>
    /// 示例列表Item - 展示如何在UILoopAniItemWidget中使用动画
    /// </summary>
    public class DemoItem : UILoopAniItemWidget, IListDataItem<DemoData>
    {
        #region UI组件
        
        private UIBindComponent m_bindComponent;
        private Text m_txtName;
        private Text m_txtDesc;
        private Image m_imgIcon;
        private Button m_btnItem;
        private Image m_imgBg;
        
        #endregion
        
        #region 数据
        
        private DemoData m_data;
        
        /// <summary>
        /// 获取当前数据
        /// </summary>
        public DemoData Data => m_data;
        
        #endregion

        #region 生命周期

        protected override void ScriptGenerator()
        {
            // 方式1: 使用UIBindComponent获取组件（如果有）
            m_bindComponent = gameObject.GetComponent<UIBindComponent>();
            if (m_bindComponent != null)
            {
                
                // m_imgIcon = m_bindComponent.GetComponent<Image>(1);
                // m_txtName = m_bindComponent.GetComponent<Text>(2);
                // m_txtDesc = m_bindComponent.GetComponent<Text>(3);
                // m_imgBg = m_bindComponent.GetComponent<Image>(0);
            }
            
            // 方式2: 使用Find获取组件（通用方式）
            if (m_txtName == null)
            {
                var nameTrans = transform.Find("Content/TxtName");
                if (nameTrans != null) m_txtName = nameTrans.GetComponent<Text>();
            }
            
            if (m_txtDesc == null)
            {
                var descTrans = transform.Find("Content/TxtDesc");
                if (descTrans != null) m_txtDesc = descTrans.GetComponent<Text>();
            }
            
            if (m_imgIcon == null)
            {
                var iconTrans = transform.Find("Content/ImgIcon");
                if (iconTrans != null) m_imgIcon = iconTrans.GetComponent<Image>();
            }
            
            if (m_imgBg == null)
            {
                m_imgBg = transform.Find("Content/ImgBg")?.GetComponent<Image>();
                if (m_imgBg == null) m_imgBg = gameObject.GetComponent<Image>();
            }
            
            // 获取按钮
            m_btnItem = m_bindComponent.GetComponent<Button>(10);
        }
        
        protected override void OnCreate()
        {
            base.OnCreate();
            
            // 绑定点击事件
            if (m_btnItem != null)
            {
                m_btnItem.onClick.AddListener(OnItemClicked);
            }
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            
            if (m_btnItem != null)
            {
                m_btnItem.onClick.RemoveAllListeners();
            }
        }

        #endregion
        
        #region IListDataItem 实现
        
        /// <summary>
        /// 设置Item数据 - IListDataItem接口实现
        /// </summary>
        public void SetItemData(DemoData data)
        {
            m_data = data;
            
            // 空值检查，防止删除动画期间数据已被移除
            if (data == null) return;
            
            // 更新DataId（用于动画系统识别）
            DataId = data.Id;
            
            // 更新UI显示
            RefreshUI();
        }
        
        /// <summary>
        /// 刷新UI显示
        /// </summary>
        private void RefreshUI()
        {
            if (m_data == null) return;
            
            if (m_txtName != null)
            {
                m_txtName.text = m_data.Name;
            }
            
            if (m_txtDesc != null)
            {
                m_txtDesc.text = m_data.Desc;
            }
            
            // 加载图标（示例）
            // LoadIcon(m_data.IconIndex);
        }
        
        #endregion
        
        #region 事件处理
        
        /// <summary>
        /// Item点击事件
        /// </summary>
        private void OnItemClicked()
        {
            if (m_data == null) return;
            
            Debug.Log($"[DemoItem] 点击了Item: {m_data.Name} (Id: {m_data.Id}, Index: {Index})");
            
            // 可以发送事件通知外部
            // GameEvent.Send("OnDemoItemClicked", m_data);
        }
        
        #endregion
        
        #region 动画相关重写（可选）
        
        /// <summary>
        /// 重写动画应用方法（可选，用于自定义动画效果）
        /// </summary>

        /// <summary>
        /// 重写回收方法
        /// </summary>
        public override void OnRecycle()
        {
            base.OnRecycle();
            
            // 清理数据引用
            m_data = null;
            
            // 重置背景颜色
            if (m_imgBg != null)
            {
                Color bgColor = m_imgBg.color;
                bgColor.a = 1f;
                m_imgBg.color = bgColor;
            }
        }
        
        #endregion
    }
}
