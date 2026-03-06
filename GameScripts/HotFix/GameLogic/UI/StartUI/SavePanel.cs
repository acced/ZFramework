using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using TEngine;
using TMPro;

namespace GameLogic
{
    /// <summary>存档面板工作模式</summary>
    public enum SavePanelMode
    {
        Load,
        Save,
    }

    /// <summary>
    /// 存档槽列表项数据，传递给 SavePanelListItem 显示用
    /// </summary>
    public class SaveSlotItemData
    {
        public int    slotId;
        public bool   isEmpty;        // true = 该槽位尚无存档
        public bool   isAutoSave;     // true = 自动存档槽位
        public string slotLabel;      // 存档序号，如 "存档 1" 或 "自动存档"
        public string locationName;
        public string goldText;
        public string levelText;
        public string playTimeText;
        public string saveTimeText;

        /// <summary>有存档数据时构造</summary>
        public SaveSlotItemData(int slotId, GameSaveData d)
        {
            this.slotId  = slotId;
            isEmpty      = false;
            isAutoSave   = d.isAutoSave;
            slotLabel    = d.isAutoSave ? "自动存档" : $"存档 {slotId}";
            locationName = string.IsNullOrEmpty(d.locationName) ? "未知地点" : d.locationName;
            goldText     = d.gold.ToString("N0");
            levelText    = $"Lv.{d.level}";
            playTimeText = d.FormatPlayTime();
            saveTimeText = d.saveTime;
        }

        /// <summary>空槽位（无存档文件）</summary>
        public static SaveSlotItemData Empty(int slotId) => new SaveSlotItemData
        {
            slotId    = slotId,
            isEmpty   = true,
            slotLabel = $"存档 {slotId}",
        };

        /// <summary>存档文件存在但数据损坏</summary>
        public static SaveSlotItemData Error(int slotId) => new SaveSlotItemData
        {
            slotId       = slotId,
            isEmpty      = false,
            slotLabel    = $"存档 {slotId}",
            locationName = "数据异常",
            goldText     = "--",
            levelText    = "--",
            playTimeText = "--",
            saveTimeText = "--",
        };

        private SaveSlotItemData() { }
    }

    [Window(UILayer.UI, location: "SavePanel")]
    public partial class SavePanel : UIWindow
    {
        #region 脚本工具生成的代码

        private UIBindComponent m_bindComponent;
        private ScrollRect      m_scrollView;
        private GameObject      m_itel;
        private Scrollbar       m_scrollBar_Vertical;
        private Button          m_btn_delete;
        private Button          m_btn_action;
        private TextMeshProUGUI m_tmp_actionLabel;

        private Button m_btn_back;

        private CancellationTokenSource m_cts;

        private UILoopListWidgetOptimized<SavePanelListItem, SaveSlotItemData> m_list;
        private GameObject m_itemPrefab;

        private const float ANIM_TOTAL = 2.1f;   // MAX_STAGGER + ANIM_DURATION + buffer
        private List<SaveSlotItemData> m_dataList = new List<SaveSlotItemData>();

        protected override void ScriptGenerator()
        {
            m_bindComponent      = gameObject.GetComponent<UIBindComponent>();
            m_scrollView         = m_bindComponent.GetComponent<ScrollRect>(0);
            m_itel               = m_bindComponent.GetComponent<RectTransform>(1).gameObject;
            m_scrollBar_Vertical = m_bindComponent.GetComponent<Scrollbar>(2);
            m_btn_delete         = m_bindComponent.GetComponent<Button>(3);
            m_btn_action         = m_bindComponent.GetComponent<Button>(4);
            m_tmp_actionLabel    = m_btn_action?.GetComponentInChildren<TextMeshProUGUI>();
            m_btn_back           = m_bindComponent.GetComponent<Button>(5);

        }

        #endregion

        // ── 模式 ──────────────────────────────────────────────
        private SavePanelMode m_mode = SavePanelMode.Load;

        protected override void OnCreate()
        {
            base.OnCreate();
            m_cts  = new CancellationTokenSource();
            m_mode = UserData is SavePanelMode pm ? pm : SavePanelMode.Load;

            InitList();
            InitBottomButtons();
            RefreshSaveList();
        }

        protected override void OnRefresh()
        {
            base.OnRefresh();
            // Hide 后再 Show 时 UserData 已更新，需重新读取模式并更新 UI
            m_mode = UserData is SavePanelMode pm ? pm : SavePanelMode.Load;
            if (m_tmp_actionLabel != null)
                m_tmp_actionLabel.SetText(m_mode == SavePanelMode.Load ? "读取" : "保存");
            UpdateBottomButtonsState();
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            m_cts?.Cancel();
            m_cts?.Dispose();
            m_cts = null;
            m_dataList.Clear();
        }

        #region 初始化列表

        private void InitList()
        {
            if (m_scrollView == null)
            {
                Log.Error("[SavePanel] ScrollRect 未找到！");
                return;
            }

            m_list = CreateWidget<UILoopListWidgetOptimized<SavePanelListItem, SaveSlotItemData>>(m_scrollView.gameObject);

            m_itemPrefab = m_itel.gameObject;
            if (m_itemPrefab != null)
                m_list.itemBase = m_itemPrefab;

            // 选中变化时更新底部按钮状态
            m_list.funcOnSelectChange = OnListSelectChanged;
        }

        #endregion

        #region 底部按钮初始化

        private void InitBottomButtons()
        {
            if (m_btn_delete != null)
                m_btn_delete.onClick.AddListener(UniTask.UnityAction(OnClick_DeleteBtn));

            if (m_btn_action != null)
                m_btn_action.onClick.AddListener(UniTask.UnityAction(OnClick_ActionBtn));

            if (m_btn_back != null)
                m_btn_back.onClick.AddListener(UniTask.UnityAction(OnClick_BackBtn));

            // 根据模式设置操作按钮文字
            if (m_tmp_actionLabel != null)
                m_tmp_actionLabel.SetText(m_mode == SavePanelMode.Load ? "读取" : "保存");
        }

        #endregion

        #region 刷新存档列表

        private const int TOTAL_SLOTS = 10;

        private void RefreshSaveList()
        {
            m_dataList.Clear();

            // 自动存档（slot 0）优先放第一位
            int autoId = SaveModule.AUTO_SAVE_SLOT_ID;
            if (GameModule.Save.SlotExists(autoId))
            {
                var autoData = GameModule.Save.LoadSlot(autoId);
                m_dataList.Add(autoData != null
                    ? new SaveSlotItemData(autoId, autoData)
                    : SaveSlotItemData.Error(autoId));
            }

            // 普通存档 1–10
            for (int slotId = 1; slotId <= TOTAL_SLOTS; slotId++)
            {
                if (GameModule.Save.SlotExists(slotId))
                {
                    var saveData = GameModule.Save.LoadSlot(slotId);
                    m_dataList.Add(saveData != null
                        ? new SaveSlotItemData(slotId, saveData)
                        : SaveSlotItemData.Error(slotId));
                }
                else
                {
                    m_dataList.Add(SaveSlotItemData.Empty(slotId));
                }
            }

            if (m_list == null) return;

            // 动画播放期间隐藏滚动条
            if (m_scrollBar_Vertical != null)
                m_scrollBar_Vertical.gameObject.SetActive(false);

            m_list.SetDatas(m_dataList);

            // 默认选中第一个
            if (m_dataList.Count > 0)
                m_list.selectIndex = 0;

            UpdateBottomButtonsState();

            // 动画结束后再显示滚动条
            ShowScrollBarAfterAnim(m_cts.Token).Forget();

            int filledCount = m_dataList.FindAll(d => !d.isEmpty).Count;
            Log.Info($"[SavePanel] 共 {m_dataList.Count} 个槽位，其中 {filledCount} 个有存档");
        }

        private async UniTaskVoid ShowScrollBarAfterAnim(CancellationToken ct)
        {
            try
            {
                await UniTask.Delay(System.TimeSpan.FromSeconds(ANIM_TOTAL), cancellationToken: ct);
                if (m_scrollBar_Vertical != null)
                    m_scrollBar_Vertical.gameObject.SetActive(true);
            }
            catch (System.OperationCanceledException) { }
        }

        #endregion

        #region 选中变化

        private void OnListSelectChanged()
        {
            UpdateBottomButtonsState();
        }

        private void UpdateBottomButtonsState()
        {
            if (m_dataList.Count == 0) return;

            int idx = m_list.selectIndex;
            bool validIdx = idx >= 0 && idx < m_dataList.Count;
            var selected  = validIdx ? m_dataList[idx] : null;

            bool hasData   = selected != null && !selected.isEmpty;
            bool isAutoSave = selected != null && selected.isAutoSave;

            // 删除按钮：有数据且非自动存档时可用
            if (m_btn_delete != null)
                m_btn_delete.interactable = hasData && !isAutoSave;

            // 操作按钮：
            //   读取模式 — 有数据时可用
            //   保存模式 — 非自动存档时可用（可写入空槽或已有槽）
            if (m_btn_action != null)
            {
                m_btn_action.interactable = m_mode == SavePanelMode.Load
                    ? hasData
                    : (!isAutoSave && validIdx);
            }
        }

        #endregion

        #region 底部按钮事件

        private UniTaskVoid OnClick_DeleteBtn()
        {
            int idx = m_list.selectIndex;
            if (idx < 0 || idx >= m_dataList.Count) return default;

            var selected = m_dataList[idx];
            if (selected.isEmpty || selected.isAutoSave) return default;

            GameModule.Save.DeleteSlot(selected.slotId);
            Log.Info($"[SavePanel] 删除存档槽位 {selected.slotId}");
            RefreshSaveList();
            return default;
        }

        private UniTaskVoid OnClick_ActionBtn()
        {
            int idx = m_list.selectIndex;
            if (idx < 0 || idx >= m_dataList.Count) return default;

            var selected = m_dataList[idx];

            if (m_mode == SavePanelMode.Load)
            {
                if (selected.isEmpty) return default;
                Log.Info($"[SavePanel] 读取存档槽位 {selected.slotId}");
                GameModule.Save.LoadSlot(selected.slotId);
                GameModule.UI.HideUI<SavePanel>();
            }
            else
            {
                // 不允许覆盖自动存档
                if (selected.isAutoSave)
                {
                    Log.Warning("[SavePanel] 不能覆盖自动存档！");
                    return default;
                }

                Log.Info($"[SavePanel] 保存到存档槽位 {selected.slotId}");
               // 此处业务层负责构造真实的 GameSaveData，此处示范调用
               var data = new GameSaveData();
               data.playerName = "张三";
               data.level = Random.Range(1, 100);
               data.locationName = "新手村";
               data.gold = Random.Range(1000, 10000);
               data.playTimeSeconds = Random.Range(1000, 10000);
               data.saveTime = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
               data.isAutoSave = false;
               GameModule.Save.SaveSlot(selected.slotId, data);
               RefreshSaveList();
               //GameModule.UI.HideUI<SavePanel>();
            }
            return default;
        }

        private UniTaskVoid OnClick_BackBtn()
        {
            GameModule.UI.HideUI<SavePanel>();
            return default;
        }

        #endregion
    }
}
