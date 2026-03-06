using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using TEngine;
using TMPro;

namespace GameLogic
{
    [Window(UILayer.UI, location: "AnimationListDemoUI")]
    public class AnimationListDemoUI : UIWindow
    {
        #region UI组件
        
        private UIBindComponent m_bindComponent;
        private ScrollRect m_scrollView;
        private GameObject m_itemPrefab;
        
        // 操作按钮
        private Button m_btnAddOne;
        private Button m_btnAddMultiple;
        private Button m_btnRemove;
        private Button m_btnRemoveMultiple;
        private Button m_btnClear;
        private Button m_btnReset;
        
        // 配置控件
        private TMP_Dropdown m_dropAnimType;
        private Slider m_sliderDuration;
        private Text m_txtDuration;
        private InputField m_inputIndex;
        private InputField m_inputCount;
        private Text m_txtInfo;
        
        #endregion
        
        #region 核心变量
        
        /// <summary>
        /// 优化版列表 Widget
        /// </summary>
        private UILoopListWidgetOptimized<DemoItem, DemoData> m_list;
        
        /// <summary>
        /// 动画时长
        /// </summary>
        private float m_animDuration = 0.3f;
        
        /// <summary>
        /// 手动取消操作的 TokenSource (用于点击 Clear/Reset 时打断当前动画)
        /// </summary>
        private CancellationTokenSource m_operationCts;
        
        /// <summary>
        /// 锁，防止狂点按钮导致逻辑错乱
        /// </summary>
        private bool m_isOperating = false;
        
        #endregion

        #region 生命周期

        protected override void ScriptGenerator()
        {
            m_bindComponent = gameObject.GetComponent<UIBindComponent>();
            m_scrollView = m_bindComponent.GetComponent<ScrollRect>(0);
            m_itemPrefab = m_bindComponent.GetComponent<RectTransform>(1).gameObject;
            
            // 查找组件 (根据实际路径调整)
            var btnPanel = transform.Find("ButtonPanel");
            m_btnAddOne = btnPanel.Find("BtnAddOne")?.GetComponent<Button>();
            m_btnAddMultiple = btnPanel.Find("BtnAddMultiple")?.GetComponent<Button>();
            m_btnRemove = btnPanel.Find("BtnRemove")?.GetComponent<Button>();
            m_btnRemoveMultiple = btnPanel.Find("BtnRemoveMultiple")?.GetComponent<Button>();
            m_btnClear = btnPanel.Find("BtnClear")?.GetComponent<Button>();
            m_btnReset = btnPanel.Find("BtnReset")?.GetComponent<Button>();
            
            m_dropAnimType = btnPanel.Find("DropAnimType")?.GetComponent<TMP_Dropdown>();
            m_sliderDuration = btnPanel.Find("SliderDuration")?.GetComponent<Slider>();
            m_txtDuration = btnPanel.Find("TxtDuration")?.GetComponent<Text>();
            
            m_inputIndex = btnPanel.Find("InputIndex")?.GetComponent<InputField>();
            m_inputCount = btnPanel.Find("InputCount")?.GetComponent<InputField>();
            m_txtInfo = btnPanel.Find("TxtInfo")?.GetComponent<Text>();
        }

        protected override void OnCreate()
        {
            base.OnCreate();
            
            m_operationCts = new CancellationTokenSource();
            
            InitList();
            BindEvents();
            InitUI();
            
            // 加载初始数据
            LoadInitialData();
            UpdateInfo();
        }

        protected override void OnDestroy()
        {
            // 销毁时彻底取消所有任务
            CancelCurrentOperations();
            m_operationCts?.Dispose();
            m_operationCts = null;
            base.OnDestroy();
        }

        #endregion
        
        #region 初始化
        
        private void InitList()
        {
            // 1. 创建组件
            m_list = CreateWidget<UILoopListWidgetOptimized<DemoItem, DemoData>>(m_scrollView.gameObject);
            
            if (m_itemPrefab != null)
            {
                m_list.itemBase = m_itemPrefab;
            }
            
            // 2. 启用动画 (API 已简化，只需传时长)
            // 缓动类型已在底层优化为 EaseOutQuad，不再需要配置
            m_list.EnableAnimation(m_animDuration);
            
            Debug.Log("[AnimationListDemoUI] 列表初始化完成");
        }
        
        private void BindEvents()
        {
            // 使用 UniTaskVoid 处理异步事件
            m_btnAddOne?.onClick.AddListener(() => OnAddOneClicked().Forget());
            m_btnAddMultiple?.onClick.AddListener(() => OnAddMultipleClicked().Forget());
            m_btnRemove?.onClick.AddListener(() => OnRemoveClicked().Forget());
            m_btnRemoveMultiple?.onClick.AddListener(() => OnRemoveMultipleClicked().Forget());
            
            // 同步事件
            m_btnClear?.onClick.AddListener(OnClearClicked);
            m_btnReset?.onClick.AddListener(OnResetClicked);
            
            m_dropAnimType?.onValueChanged.AddListener(OnAnimTypeChanged);
            
            if (m_sliderDuration != null)
            {
                m_sliderDuration.onValueChanged.AddListener(OnDurationChanged);
            }
        }

        private void InitUI()
        {
            if (m_sliderDuration != null)
            {
                m_sliderDuration.minValue = 0.1f;
                m_sliderDuration.maxValue = 1.5f;
                m_sliderDuration.value = m_animDuration;
            }

            if (m_dropAnimType != null)
            {
                m_dropAnimType.ClearOptions();
                m_dropAnimType.AddOptions(new List<string> { "无动画", "启用动画" });
                m_dropAnimType.value = 1;
            }
        }

        private void LoadInitialData()
        {
            // 直接设置数据给 Widget，Widget 内部会持有这个列表
            var initialData = DemoData.CreateBatch(6);
            m_list.SetDatas(initialData);
        }
        
        #endregion
        
        #region 异步操作逻辑 (核心)
        
        /// <summary>
        /// 获取安全的 Token：
        /// 1. 如果窗口销毁，触发 Cancel
        /// 2. 如果点击了 Clear/Reset，触发 Cancel
        /// </summary>
        private CancellationToken GetOperationToken()
        {
            // 链接 "组件销毁Token" 和 "手动操作Token"
            return m_operationCts.Token;
        }

        private async UniTaskVoid OnAddOneClicked()
        {
            if (m_isOperating) return;
            m_isOperating = true;
            SetButtonsInteractable(false);

            try
            {
                int index = GetInputIndex();
                var newData = DemoData.Create(); // 创建数据

                // ★ 核心变更：
                // 不需要维护 demo 层的 dataList。
                // 直接调用 m_list.InsertItemAsync，它会修改内部数据并播放动画。
                await m_list.InsertItemAsync(index, newData, GetOperationToken());
                
                Debug.Log($"[Add] {newData.Name} at {index}");
            }
            catch (System.OperationCanceledException)
            {
                Debug.Log("[Add] 操作被取消");
            }
            finally
            {
                m_isOperating = false;
                SetButtonsInteractable(true);
                UpdateInfo();
            }
        }

        private async UniTaskVoid OnAddMultipleClicked()
        {
            if (m_isOperating) return;
            m_isOperating = true;
            SetButtonsInteractable(false);

            try
            {
                int index = GetInputIndex();
                int count = GetInputCount();
                var newDatas = DemoData.CreateBatch(count);

                // ★ 核心变更：批量插入 API
                // stagger: 0.05f 是每个 Item 动画的间隔时间
                await m_list.InsertItemsAsync(index, newDatas, stagger: 0.05f, GetOperationToken());
                
                Debug.Log($"[AddMulti] {count} items at {index}");
            }
            catch (System.OperationCanceledException)
            {
                Debug.Log("[AddMulti] 操作被取消");
            }
            finally
            {
                m_isOperating = false;
                SetButtonsInteractable(true);
                UpdateInfo();
            }
        }

        private async UniTaskVoid OnRemoveClicked()
        {
            if (m_isOperating) return;
            
            // 注意：一定要读取 m_list.datas.Count，因为它是唯一事实来源
            int currentCount = m_list.datas?.Count ?? 0;
            int index = GetInputIndex();

            if (index < 0 || index >= currentCount)
            {
                Debug.LogWarning("索引越界");
                return;
            }

            m_isOperating = true;
            SetButtonsInteractable(false);

            try
            {
                // ★ 核心变更：直接调用移除
                // Widget 内部会先播放收缩动画，再移除数据，再刷新列表
                await m_list.RemoveItemAsync(index, GetOperationToken());
                
                Debug.Log($"[Remove] at {index}");
            }
            catch (System.OperationCanceledException)
            {
                Debug.Log("[Remove] 操作被取消");
            }
            finally
            {
                m_isOperating = false;
                SetButtonsInteractable(true);
                UpdateInfo();
            }
        }

        private async UniTaskVoid OnRemoveMultipleClicked()
        {
            if (m_isOperating) return;

            int currentCount = m_list.datas?.Count ?? 0;
            int index = GetInputIndex();
            int count = GetInputCount();

            if (index < 0 || index >= currentCount) return;

            m_isOperating = true;
            SetButtonsInteractable(false);

            try
            {
                // 我们没有提供 RemoveItemsAsync (批量移除) 的 API?
                // 在优化版代码中，为了简化逻辑，建议一个个删，或者在 Demo 层循环调用
                // 但为了性能，循环调用 RemoveItemAsync 会导致多次 Refresh。
                // 如果必须批量删，可以在 Widget 中实现。
                // 这里演示 循环调用 (串行删除，会有“吃豆人”效果)
                
                var token = GetOperationToken();
                // 倒序删除以保证索引正确 (虽然 Widget 内部处理了 Shift，但外部调用还是倒序安全)
                // 或者直接删同一个位置 count 次
                for (int i = 0; i < count; i++)
                {
                    if (index >= (m_list.datas?.Count ?? 0)) break;
                    
                    // 每次删完一个等待一小会儿，形成连续删除动画
                    await m_list.RemoveItemAsync(index, token);
                    // 稍微停顿一下（可选）
                    // await UniTask.Delay(TimeSpan.FromSeconds(0.05f), cancellationToken: token);
                }
            }
            catch (System.OperationCanceledException)
            {
                Debug.Log("[RemoveMulti] 操作被取消");
            }
            finally
            {
                m_isOperating = false;
                SetButtonsInteractable(true);
                UpdateInfo();
            }
        }

        #endregion

        #region 同步操作
        
        private void OnClearClicked()
        {
            // 打断当前所有动画
            CancelCurrentOperations();

            // 直接设置空数据
            m_list.SetDatas(new List<DemoData>());
            
            m_isOperating = false;
            SetButtonsInteractable(true);
            UpdateInfo();
            Debug.Log("[Clear] 数据已清空");
        }

        private void OnResetClicked()
        {
            CancelCurrentOperations();

            DemoData.ResetIdCounter();
            LoadInitialData();
            
            m_isOperating = false;
            SetButtonsInteractable(true);
            UpdateInfo();
            Debug.Log("[Reset] 重置为默认数据");
        }

        /// <summary>
        /// 打断当前操作（如正在播放的动画）
        /// </summary>
        private void CancelCurrentOperations()
        {
            if (m_operationCts != null)
            {
                m_operationCts.Cancel();
                m_operationCts.Dispose();
                m_operationCts = new CancellationTokenSource();
            }
        }

        #endregion

        #region 设置与辅助

        private void OnAnimTypeChanged(int index)
        {
            bool enable = index == 1;
            if (enable)
                m_list.EnableAnimation(m_animDuration);
            else
                // 假设 Widget 有 Disable 方法，或者设置时长为 0
                m_list.EnableAnimation(0f); 
            
            UpdateInfo();
        }

        private void OnDurationChanged(float value)
        {
            m_animDuration = value;
            m_txtDuration.text = $"{value:F2}s";
            
            // 更新 Widget 配置
            m_list.EnableAnimation(m_animDuration);
        }

        private int GetInputIndex()
        {
            if (m_inputIndex != null && int.TryParse(m_inputIndex.text, out int val))
                return Mathf.Max(0, val);
            return 0;
        }

        private int GetInputCount()
        {
            if (m_inputCount != null && int.TryParse(m_inputCount.text, out int val))
                return Mathf.Clamp(val, 1, 20);
            return 1;
        }

        private void SetButtonsInteractable(bool enable)
        {
            if (m_btnAddOne) m_btnAddOne.interactable = enable;
            if (m_btnAddMultiple) m_btnAddMultiple.interactable = enable;
            if (m_btnRemove) m_btnRemove.interactable = enable;
            if (m_btnRemoveMultiple) m_btnRemoveMultiple.interactable = enable;
        }

        private void UpdateInfo()
        {
            if (m_txtInfo == null) return;
            
            int count = m_list.datas?.Count ?? 0;
            m_txtInfo.text = $"当前数据量: {count}\n" +
                             $"操作中: {m_isOperating}\n" +
                             $"动画时长: {m_animDuration:F2}s";
        }

        #endregion
    }
}