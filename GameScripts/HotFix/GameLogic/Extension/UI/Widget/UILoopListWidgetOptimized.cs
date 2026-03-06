using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using TEngine;
using UnityEngine;

namespace GameLogic
{
    /// <summary>
    /// 高性能异步动画循环列表
    /// </summary>
    public class UILoopListWidgetOptimized<TItem, TData> : UIListBase<TItem, TData> 
        where TItem : UILoopAniItemWidget, new()
    {
        #region 基础组件
        
        public LoopListView LoopRectView { get; private set; }
        private readonly GameFrameworkDictionary<int, TItem> m_itemCache = new GameFrameworkDictionary<int, TItem>();
        private Action<TItem, int> m_tpFuncItem;
        private CancellationTokenSource _destroyCts;

        #endregion

        #region 动画状态管理

        private bool _enableAnimation = false;
        private float _defaultDuration = 0.3f;

        // 记录非标准状态的 Item (Key: DataIndex, Value: Progress 0~1)
        private readonly Dictionary<int, float> _animatingStates = new Dictionary<int, float>();
        
        // 锁定正在操作的索引，防止并发冲突
        private readonly HashSet<int> _operatingIndices = new HashSet<int>();
        
        // 缓存列表，避免 ShiftAnimationStates 产生 GC
        private readonly List<int> _tempKeysBuffer = new List<int>(32);

        #endregion

        #region 生命周期

        protected override void BindMemberProperty()
        {
            base.BindMemberProperty();
            LoopRectView = rectTransform.GetComponent<LoopListView>();
            _destroyCts = new CancellationTokenSource();
        }

        protected override void OnCreate()
        {
            base.OnCreate();
            LoopRectView.InitListView(0, OnGetItemByIndex);
        }

        protected override void OnDestroy()
        {
            _destroyCts?.Cancel();
            _destroyCts?.Dispose();
            m_itemCache.Clear();
            _animatingStates.Clear();
            _operatingIndices.Clear();
            _tempKeysBuffer.Clear();
            base.OnDestroy();
        }

        #endregion

        #region 列表回调

        protected LoopListViewItem OnGetItemByIndex(LoopListView listView, int index)
        {
            if (index < 0 || index >= num) return null;


            LoopListViewItem loopItem = LoopRectView.NewListViewItem(typeof(TItem).Name);
            TItem widget = GetOrCreateWidget(loopItem);
            
            // 基础数据绑定
            widget.SetItemIndex(index);
            if (widget is UILoopAniItemWidget aniWidget)
            {
                aniWidget.LoopItem.ItemId = index;
                aniWidget.DataId = index;
                
                // ★ 状态恢复机制
                if (_enableAnimation && _animatingStates.TryGetValue(index, out float animValue))
                {
                    // 该 Item 处于动画中或非标准尺寸
                    aniWidget.SetAnimationValue(animValue);
                }
                else
                {
                    // 正常状态
                    aniWidget.ResetAnimationState();
                }
            }

            UpdateListItem(widget, index, m_tpFuncItem);

            // ★ 恢复 Item 被循环复用后可能丢失的选中状态
            if (widget is IListSelectItem selectItem)
            {
                selectItem.SetSelected(index == m_selectIndex);
            }

            return loopItem;
        }

        /// <summary>
        /// 重写以支持循环列表中按数据索引查找可见 Item，
        /// 使 SetSelectIndex 能正确更新旧 Item 的视觉状态。
        /// </summary>
        public override TItem GetItem(int i) => GetShownItemByDataIndex(i);

        private TItem GetOrCreateWidget(LoopListViewItem item)
        {
            if (!m_itemCache.TryGetValue(item.GoId, out TItem widget))
            {
                widget = CreateWidget<TItem>(item.gameObject);
                widget.LoopItem = item;
                m_itemCache.Add(item.GoId, widget);
                
                // 新生成的 Item，尝试捕获其初始尺寸
                widget.CaptureInitialSize();
            }
            return widget;
        }

        protected override void AdjustItemNum(int n, List<TData> datas = null, Action<TItem, int> funcItem = null)
        {
            // 全量刷新时，清除所有动画中间状态，防止错位
            _animatingStates.Clear();
            _operatingIndices.Clear();
            
            base.AdjustItemNum(n, datas, funcItem);
            m_tpFuncItem = funcItem;
            LoopRectView.SetListItemCount(n);
            LoopRectView.RefreshAllShownItem();
            m_tpFuncItem = null;
        }

        #endregion

        #region 对外 API (动画控制)

        public void EnableAnimation(float duration = 0.3f)
        {
            _enableAnimation = true;
            _defaultDuration = duration;
        }

        /// <summary>
        /// 异步插入数据
        /// </summary>
        public async UniTask InsertItemAsync(int index, TData data, CancellationToken ct = default)
        {
            if (m_datas == null) m_datas = new List<TData>();
            index = Mathf.Clamp(index, 0, m_datas.Count);

            // 1. 数据层平移：为新数据腾出空间
            ShiftAnimationStates(index, 1);
            
            m_datas.Insert(index, data);
            m_num = m_datas.Count;

            // 2. 标记新 Item 为折叠态 (0)
            _animatingStates[index] = 0f;

            // 3. 刷新视图 (此时该 Item 高度为 0)
            LoopRectView.SetListItemCount(m_num, false);
            LoopRectView.RefreshAllShownItem();

            if (!_enableAnimation)
            {
                _animatingStates.Remove(index);
                LoopRectView.RefreshAllShownItem();
                return;
            }

            // 4. 播放展开动画
            await RunAnimationAsync(index, 0f, 1f, ct);
            
            // 5. 动画结束，移除临时状态
            _animatingStates.Remove(index);
        }

        /// <summary>
        /// 异步移除数据
        /// </summary>
        public async UniTask RemoveItemAsync(int index, CancellationToken ct = default)
        {
            if (index < 0 || index >= m_num) return;
            // 如果正在操作该 Item，直接返回防止逻辑冲突
            if (_operatingIndices.Contains(index)) return; 

            if (!_enableAnimation)
            {
                RemoveItemDirect(index);
                return;
            }

            // 锁定该索引
            _operatingIndices.Add(index);

            // 1. 播放收缩动画 (1 -> 0)
            // 注意：此时数据尚未删除，只是视觉消失
            await RunAnimationAsync(index, 1f, 0f, ct);

            // 2. 逻辑删除数据
            if (index < m_datas.Count)
            {
                m_datas.RemoveAt(index);
                m_num = m_datas.Count;
            }

            // 3. 索引回填：后面的数据补上来
            _animatingStates.Remove(index);
            ShiftAnimationStates(index + 1, -1);
            
            // 解锁 (注意：此时 index 已经指向原来的下一个元素了，但我们的锁是基于旧索引的)
            _operatingIndices.Remove(index); 

            // 4. 刷新视图
          LoopRectView.SetListItemCount(m_num, true);
            LoopRectView.RefreshAllShownItem();
        }

        /// <summary>
        /// 批量插入（带交错效果）
        /// </summary>
        public async UniTask InsertItemsAsync(int index, List<TData> newDatas, float stagger = 0.05f, CancellationToken ct = default)
        {
            if (newDatas == null || newDatas.Count == 0) return;
            int count = newDatas.Count;
            index = Mathf.Clamp(index, 0, m_datas?.Count ?? 0);
            
            // 1. 批量腾挪空间
            ShiftAnimationStates(index, count);
            if (m_datas == null) m_datas = new List<TData>();
            m_datas.InsertRange(index, newDatas);
            m_num = m_datas.Count;

            // 2. 初始化所有新 Item 为折叠态
            for (int i = 0; i < count; i++)
            {
                _animatingStates[index + i] = 0f;
            }

            LoopRectView.SetListItemCount(m_num, false);
            LoopRectView.RefreshAllShownItem();

            if (!_enableAnimation) return;

            // 3. 创建 LinkedToken，确保组件销毁时停止
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(_destroyCts.Token, ct);
            
            // 4. 并行执行动画
            var tasks = new List<UniTask>(count);
            for (int i = 0; i < count; i++)
            {
                int targetIndex = index + i;
                float delay = i * stagger;
                
                // 启动动画任务
                tasks.Add(UniTask.Create(async () =>
                {
                    if (delay > 0) await UniTask.Delay(TimeSpan.FromSeconds(delay), cancellationToken: linkedCts.Token);
                    await RunAnimationAsync(targetIndex, 0f, 1f, linkedCts.Token);
                    _animatingStates.Remove(targetIndex);
                }));
            }

            await UniTask.WhenAll(tasks);
        }

        #endregion

        #region 内部核心逻辑

        private void RemoveItemDirect(int index)
        {
            m_datas.RemoveAt(index);
            m_num = m_datas.Count;
            LoopRectView.SetListItemCount(m_num, false);
            LoopRectView.RefreshAllShownItem();
        }

        /// <summary>
        /// 核心：调整动画状态字典的 Key (零 GC 实现)
        /// </summary>
        private void ShiftAnimationStates(int startIndex, int offset)
        {
            if (_animatingStates.Count == 0 && _operatingIndices.Count == 0) return;

            // 1. 收集所有需要移动的 Key
            _tempKeysBuffer.Clear();
            foreach (var key in _animatingStates.Keys)
            {
                if (key >= startIndex) _tempKeysBuffer.Add(key);
            }

            // 2. 排序 (关键步骤)
            // 插入(Offset>0): 倒序移动 (5->6, 4->5)，防止 4 把 5 覆盖了
            // 删除(Offset<0): 正序移动 (4->3, 5->4)，防止 5 把 4 覆盖了
            if (offset > 0) _tempKeysBuffer.Sort((a, b) => b.CompareTo(a));
            else _tempKeysBuffer.Sort();

            // 3. 执行平移
            foreach (var oldKey in _tempKeysBuffer)
            {
                if (_animatingStates.TryGetValue(oldKey, out float val))
                {
                    _animatingStates.Remove(oldKey);
                    // 安全检查：防止 Key 冲突 (理论上排序后不会发生)
                    if (!_animatingStates.ContainsKey(oldKey + offset))
                    {
                        _animatingStates[oldKey + offset] = val;
                    }
                }
            }
            
            // 4. 处理操作锁 (逻辑同上)
            _tempKeysBuffer.Clear();
            foreach (var key in _operatingIndices)
            {
                if (key >= startIndex) _tempKeysBuffer.Add(key);
            }
            
            _operatingIndices.RemoveWhere(k => k >= startIndex);
            foreach (var oldKey in _tempKeysBuffer)
            {
                _operatingIndices.Add(oldKey + offset);
            }
        }

        private async UniTask RunAnimationAsync(int dataIndex, float start, float end, CancellationToken ct)
        {
            // 双重保护 token
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(_destroyCts.Token, ct);
            
            await UIListAnimationHelper.TweenValueAsync(start, end, _defaultDuration, (val) =>
            {
                // 更新数据状态
                _animatingStates[dataIndex] = val;

                // 更新视图状态
                // 必须实时获取 Item，因为滚动过程中 Item 可能被回收或重分配
                var item = GetShownItemByDataIndex(dataIndex);
                if (item != null)
                {
                    item.SetAnimationValue(val);
                    // 通知 SuperScrollView 重新布局
                    LoopRectView.OnItemSizeChanged(item.LoopItem.ItemIndex);
                }
            }, linkedCts.Token);
        }

        public TItem GetShownItemByDataIndex(int dataIndex)
        {
            LoopListViewItem item = LoopRectView.GetShownItemByItemIndex(dataIndex);
            if (item != null && m_itemCache.TryGetValue(item.GoId, out TItem widget))
            {
                return widget;
            }
            return null;
        }

        #endregion
    }
}