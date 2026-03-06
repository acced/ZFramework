namespace GameLogic
{
    /// <summary>
    /// 循环列表Item Widget基类
    /// 提供基础的Item功能，不包含动画
    /// </summary>
    public class UILoopItemWidget : SelectItemBase
    {
        #region 基础属性
        
        /// <summary>
        /// 关联的LoopListViewItem组件
        /// </summary>
        public LoopListViewItem LoopItem { set; get; }

        /// <summary>
        /// Item索引
        /// </summary>
        public int Index { private set; get; }
        
        /// <summary>
        /// 数据ID（用于数据识别）
        /// </summary>
        public int DataId { get; set; } = -1;
        
        #endregion

        #region 生命周期

        protected override void BindMemberProperty()
        {
            base.BindMemberProperty();
        }

        /// <summary>
        /// 更新Item数据
        /// </summary>
        public virtual void UpdateItem(int index)
        {
            Index = index;
        }
        
        /// <summary>
        /// 当Item被回收时调用
        /// </summary>
        public virtual void OnRecycle()
        {
            DataId = -1;
        }
        
        #endregion
    }
}
