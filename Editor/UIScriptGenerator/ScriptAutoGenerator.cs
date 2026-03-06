using System;
using System.IO;
using System.Text;
using GameLogic;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace TEngine.Editor.UI
{
    public partial class ScriptGenerator
    {
        private static TextEditor m_textEditor = new TextEditor();
        private static string[] VARIABLE_NAME_REGEX;
        private static void CheckVariableNames()
        {
            var cnt = (int)UIFieldCodeStyle.Max;
            VARIABLE_NAME_REGEX = new string[cnt];

            for (int i = 0; i < cnt; i++)
            {
                VARIABLE_NAME_REGEX[i] = GetPrefixNameByCodeStyle((UIFieldCodeStyle)i);
            }
        }

        [MenuItem("GameObject/ScriptGenerator/UIPropertyBindComponent", priority = 84)]
        public static void UIPropertyBindComponent()
        {
            GenerateCSharpScript(false);
        }

        [MenuItem("GameObject/ScriptGenerator/UIPropertyBindComponent", true)]
        public static bool ValidateUIPropertyBindComponent()
        {
            return ScriptGeneratorSetting.Instance.UseBindComponent;
        }

        [MenuItem("GameObject/ScriptGenerator/UIPropertyBindComponent - UniTask", priority = 85)]
        public static void UIPropertyBindComponentUniTask()
        {
            GenerateCSharpScript(false, true);
        }

        [MenuItem("GameObject/ScriptGenerator/UIPropertyBindComponent - UniTask", true)]
        public static bool ValidateUIPropertyBindComponentUniTask()
        {
            return ScriptGeneratorSetting.Instance.UseBindComponent;
        }

        [MenuItem("GameObject/ScriptGenerator/UIPropertyAndListenerBindComponent", priority = 86)]
        public static void UIPropertyAndListenerBindComponent()
        {
            GenerateCSharpScript(true);
        }

        [MenuItem("GameObject/ScriptGenerator/UIPropertyAndListenerBindComponent", true)]
        public static bool ValidateUIPropertyAndListenerBindComponent()
        {
            return ScriptGeneratorSetting.Instance.UseBindComponent;
        }

        [MenuItem("GameObject/ScriptGenerator/UIPropertyAndListenerBindComponentUniTask - UniTask", priority = 87)]
        public static void UIPropertyAndListenerBindComponentUniTask()
        {
            GenerateCSharpScript(true, true);
        }

        [MenuItem("GameObject/ScriptGenerator/UIPropertyAndListenerBindComponentUniTask - UniTask", true)]
        public static bool ValidateUIPropertyAndListenerBindComponentUniTask()
        {
            return ScriptGeneratorSetting.Instance.UseBindComponent;
        }

        [MenuItem("GameObject/ScriptGenerator/UILoopListWidgetOptimized 模板", priority = 88)]
        public static void GenerateUILoopListWidgetOptimizedTemplate()
        {
            var root = Selection.activeTransform;
            if (root == null)
            {
                EditorUtility.DisplayDialog("错误", "请先选中一个UI窗口对象", "确定");
                return;
            }

            // 打开配置窗口让用户输入数据类型
            var configWindow = EditorWindow.GetWindow<UILoopListConfigWindow>();
            configWindow.titleContent = new GUIContent("配置列表数据类型");
            configWindow.minSize = new Vector2(500, 250);
            configWindow.Initialize(root, GenerateUILoopListWidgetOptimizedTemplateWithConfig);
            configWindow.Show();
        }

        [MenuItem("GameObject/ScriptGenerator/UILoopListWidgetOptimized 模板", true)]
        public static bool ValidateGenerateUILoopListWidgetOptimizedTemplate()
        {
            return Selection.activeTransform != null;
        }

        /// <summary>
        /// 使用配置生成代码模板
        /// </summary>
        private static void GenerateUILoopListWidgetOptimizedTemplateWithConfig(Transform root, string itemTypeName, string dataTypeName, bool generateItemAndData)
        {
            if (string.IsNullOrEmpty(itemTypeName))
            {
                itemTypeName = "UILoopAniItemWidget";
            }
            if (string.IsNullOrEmpty(dataTypeName))
            {
                dataTypeName = "DemoData";
            }

            CheckVariableNames();
            StringBuilder strVar = new StringBuilder();
            StringBuilder strBind = new StringBuilder();
            StringBuilder strOnCreate = new StringBuilder();
            StringBuilder strCallback = new StringBuilder();

            // 生成基础UI组件绑定代码
            strVar.AppendLine($"\t\tprivate UIBindComponent m_bindComponent;");
            strBind.AppendLine($"\t\t\tm_bindComponent = gameObject.GetComponent<UIBindComponent>();");
            
            m_bindIndex = 0;
            AutoErgodic(root, root, ref strVar, ref strBind, ref strOnCreate, ref strCallback, true);

            // 添加 UILoopListWidgetOptimized 相关代码
            strVar.AppendLine();
            strVar.AppendLine("\t\t/// <summary>");
            strVar.AppendLine("\t\t/// 取消令牌源（用于取消进行中的操作）");
            strVar.AppendLine("\t\t/// </summary>");
            strVar.AppendLine("\t\tprivate CancellationTokenSource m_cts;");
            strVar.AppendLine();
            strVar.AppendLine($"\t\tprivate UILoopListWidgetOptimized<{itemTypeName}, {dataTypeName}> m_list;");
            strVar.AppendLine();
            strVar.AppendLine("\t\tprivate GameObject m_itemPrefab;");
            strVar.AppendLine();
            strVar.AppendLine("\t\t/// <summary>");
            strVar.AppendLine("\t\t/// 当前动画类型");
            strVar.AppendLine("\t\t/// </summary>");
            strVar.AppendLine("\t\tprivate AnimationType m_animationType = AnimationType.Clip;");
            strVar.AppendLine();
            strVar.AppendLine("\t\t/// <summary>");
            strVar.AppendLine("\t\t/// 当前缓动类型");
            strVar.AppendLine("\t\t/// </summary>");
            strVar.AppendLine("\t\tprivate AnimationEaseType m_easeType = AnimationEaseType.EaseOutQuad;");
            strVar.AppendLine();
            strVar.AppendLine("\t\t/// <summary>");
            strVar.AppendLine("\t\t/// 动画时长");
            strVar.AppendLine("\t\t/// </summary>");
            strVar.AppendLine("\t\tprivate float m_animDuration = 0.3f;");
            strVar.AppendLine();
            strVar.AppendLine("\t\t/// <summary>");
            strVar.AppendLine("\t\t/// 数据列表");
            strVar.AppendLine("\t\t/// </summary>");
            strVar.AppendLine($"\t\tprivate List<{dataTypeName}> m_dataList = new List<{dataTypeName}>();");

            // 查找 ScrollRect 组件（用于列表初始化）
            Transform scrollViewTransform = FindDeepChild(root, "m_scrollView");
            if (scrollViewTransform == null)
            {
                // 尝试查找任何包含 ScrollRect 的子对象
                ScrollRect scrollRect = root.GetComponentInChildren<ScrollRect>();
                if (scrollRect != null)
                {
                    scrollViewTransform = scrollRect.transform;
                }
            }

            StringBuilder strFile = new StringBuilder();
            strFile.AppendLine("//----------------------------------------------------------");
            strFile.AppendLine("// <auto-generated>");
            strFile.AppendLine("// -This code was generated.");
            strFile.AppendLine("// -Changes to this file may cause incorrect behavior.");
            strFile.AppendLine("// -will be lost if the code is regenerated.");
            strFile.AppendLine("// <auto-generated/>");
            strFile.AppendLine("//----------------------------------------------------------");
            strFile.AppendLine("using Cysharp.Threading.Tasks;");
            strFile.AppendLine("using UnityEngine;");
            strFile.AppendLine("using UnityEngine.UI;");
            strFile.AppendLine("using TEngine;");
            strFile.AppendLine("using System.Threading;");
            strFile.AppendLine("using System.Collections.Generic;");
            strFile.AppendLine($"namespace {ScriptGeneratorSetting.GetUINameSpace()}");
            strFile.AppendLine("{");
            
            var widgetPrefix = GetUIWidgetName();
            if (root.name.StartsWith(widgetPrefix))
            {
                strFile.AppendLine($"\tpublic partial class {root.name.Replace(GetUIWidgetName(), string.Empty)} : UIWidget");
            }
            else
            {
                strFile.AppendLine($"\t[Window(UILayer.UI, location : \"{root.name}\")]");
                strFile.AppendLine($"\tpublic partial class {root.name} : UIWindow");
            }
            
            strFile.AppendLine("\t{");
            strFile.AppendLine("\t\t#region 脚本工具生成的代码");
            strFile.AppendLine();
            strFile.Append(strVar.ToString());
            strFile.AppendLine();
            strFile.AppendLine("\t\tprotected override void ScriptGenerator()");
            strFile.AppendLine("\t\t{");
            strFile.Append(strBind.ToString());
            strFile.Append(strOnCreate.ToString());
            strFile.AppendLine("\t\t}");
            strFile.AppendLine();
            strFile.AppendLine("\t\t#endregion");
            strFile.AppendLine();
            strFile.AppendLine("\t\tprotected override void OnCreate()");
            strFile.AppendLine("\t\t{");
            strFile.AppendLine("\t\t\tbase.OnCreate();");
            strFile.AppendLine();
            strFile.AppendLine("\t\t\t// 创建取消令牌");
            strFile.AppendLine("\t\tm_cts = new CancellationTokenSource();");
            strFile.AppendLine();
            strFile.AppendLine("\t\t// 初始化列表");
            strFile.AppendLine("\t\tInitList();");
            strFile.AppendLine();
            strFile.AppendLine("\t\t// 加载初始数据");
            strFile.AppendLine("\t\tLoadInitialData();");
            strFile.AppendLine("\t}");
            strFile.AppendLine();
            strFile.AppendLine("\t\tprotected override void OnDestroy()");
            strFile.AppendLine("\t\t{");
            strFile.AppendLine("\t\t\tbase.OnDestroy();");
            strFile.AppendLine();
            strFile.AppendLine("\t\t\t// ★ 取消所有进行中的操作");
            strFile.AppendLine("\t\tm_cts?.Cancel();");
            strFile.AppendLine("\t\tm_cts?.Dispose();");
            strFile.AppendLine("\t\tm_cts = null;");
            strFile.AppendLine();
            strFile.AppendLine("\t\tm_dataList.Clear();");
            // 调用 ResetIdCounter（如果 Data 类有这个方法）
            strFile.AppendLine($"\t\t{dataTypeName}.ResetIdCounter();");
            strFile.AppendLine("\t}");
            strFile.AppendLine();
            strFile.AppendLine("\t\t#region 初始化");
            strFile.AppendLine();
            strFile.AppendLine("\t\tprivate void InitList()");
            strFile.AppendLine("\t\t{");
            if (scrollViewTransform != null)
            {
                strFile.AppendLine($"\t\t\t// 获取 ScrollRect 组件（请根据实际情况调整索引）");
                strFile.AppendLine($"\t\t\tvar scrollView = m_bindComponent.GetComponent<ScrollRect>(0);");
            }
            else
            {
                strFile.AppendLine("\t\t\t// 请手动设置 ScrollRect 组件");
                strFile.AppendLine("\t\t\tvar scrollView = GetComponentInChildren<ScrollRect>();");
            }
            strFile.AppendLine("\t\t\tif (scrollView == null)");
            strFile.AppendLine("\t\t\t{");
            strFile.AppendLine("\t\t\t\tDebug.LogError(\"[UILoopListWidgetOptimized] ScrollView未找到！\");");
            strFile.AppendLine("\t\t\t\treturn;");
            strFile.AppendLine("\t\t\t}");
            strFile.AppendLine();
            strFile.AppendLine("\t\t\t// ★ 创建优化版列表 Widget");
            strFile.AppendLine($"\t\tm_list = CreateWidget<UILoopListWidgetOptimized<{itemTypeName}, {dataTypeName}>>(scrollView.gameObject);");
            strFile.AppendLine();
            strFile.AppendLine("\t\t\t// 设置Item预制体");
            strFile.AppendLine("\t\tif (m_itemPrefab != null)");
            strFile.AppendLine("\t\t{");
            strFile.AppendLine("\t\t\tm_list.itemBase = m_itemPrefab;");
            strFile.AppendLine("\t\t}");
            strFile.AppendLine();
            strFile.AppendLine("\t\t\t// ★ 启用动画功能");
            strFile.AppendLine("\t\tm_list.EnableAnimationFeature(");
            strFile.AppendLine("\t\t\tanimationType: m_animationType,");
            strFile.AppendLine("\t\t\tduration: m_animDuration,");
            strFile.AppendLine("\t\t\teaseType: m_easeType");
            strFile.AppendLine("\t\t);");
            strFile.AppendLine();
            strFile.AppendLine("\t\t\tDebug.Log(\"[UILoopListWidgetOptimized] 优化版列表初始化完成，动画功能已启用\");");
            strFile.AppendLine("\t\t}");
            strFile.AppendLine();
            strFile.AppendLine("\t\t/// <summary>");
            strFile.AppendLine("\t\t/// 加载初始数据");
            strFile.AppendLine("\t\t/// </summary>");
            strFile.AppendLine("\t\tprivate void LoadInitialData()");
            strFile.AppendLine("\t\t{");
            // 使用 CreateBatch 方法创建初始数据
            strFile.AppendLine($"\t\t\tm_dataList = {dataTypeName}.CreateBatch(10);");
            strFile.AppendLine("\t\t\tm_list.SetDatas(m_dataList);");
            strFile.AppendLine();
            strFile.AppendLine("\t\t\tDebug.Log($\"[UILoopListWidgetOptimized] 加载了 {m_dataList.Count} 条初始数据\");");
            strFile.AppendLine("\t\t}");
            strFile.AppendLine("\t\t#endregion");
            strFile.AppendLine("\t}");
            strFile.AppendLine("}");

            m_textEditor.Delete();
            m_textEditor.text = strFile.ToString();
            m_textEditor.SelectAll();
            m_textEditor.Copy();

            Debug.Log($"<color=#1E90FF>UILoopListWidgetOptimized 代码模板已生成到剪贴板，请自行Ctl+V粘贴</color>");
        }

        /// <summary>
        /// 生成 Item 类代码（已废弃，不再使用）
        /// </summary>
        [System.Obsolete("不再生成 Item 类，只生成 UIWindow 代码")]
        private static string GenerateItemClassCode(string itemTypeName, string dataTypeName)
        {
            StringBuilder strFile = new StringBuilder();
            strFile.AppendLine("//----------------------------------------------------------");
            strFile.AppendLine("// <auto-generated>");
            strFile.AppendLine("// -This code was generated.");
            strFile.AppendLine("// -Changes to this file may cause incorrect behavior.");
            strFile.AppendLine("// -will be lost if the code is regenerated.");
            strFile.AppendLine("// <auto-generated/>");
            strFile.AppendLine("//----------------------------------------------------------");
            strFile.AppendLine("using UnityEngine;");
            strFile.AppendLine("using UnityEngine.UI;");
            strFile.AppendLine("using TEngine;");
            strFile.AppendLine();
            strFile.AppendLine($"namespace {ScriptGeneratorSetting.GetUINameSpace()}");
            strFile.AppendLine("{");
            strFile.AppendLine($"\t/// <summary>");
            strFile.AppendLine($"\t/// {itemTypeName} - 列表Item基类");
            strFile.AppendLine($"\t/// </summary>");
            strFile.AppendLine($"\tpublic class {itemTypeName} : UILoopAniItemWidget, IListDataItem<{dataTypeName}>");
            strFile.AppendLine("\t{");
            strFile.AppendLine("\t\t#region UI组件");
            strFile.AppendLine();
            strFile.AppendLine("\t\tprivate UIBindComponent m_bindComponent;");
            strFile.AppendLine();
            strFile.AppendLine("\t\t#endregion");
            strFile.AppendLine();
            strFile.AppendLine("\t\t#region 数据");
            strFile.AppendLine();
            strFile.AppendLine($"\t\tprivate {dataTypeName} m_data;");
            strFile.AppendLine();
            strFile.AppendLine("\t\t/// <summary>");
            strFile.AppendLine("\t\t/// 获取当前数据");
            strFile.AppendLine("\t\t/// </summary>");
            strFile.AppendLine($"\t\tpublic {dataTypeName} Data => m_data;");
            strFile.AppendLine();
            strFile.AppendLine("\t\t#endregion");
            strFile.AppendLine();
            strFile.AppendLine("\t\t#region 生命周期");
            strFile.AppendLine();
            strFile.AppendLine("\t\tprotected override void ScriptGenerator()");
            strFile.AppendLine("\t\t{");
            strFile.AppendLine("\t\t\tm_bindComponent = gameObject.GetComponent<UIBindComponent>();");
            strFile.AppendLine("\t\t\t");
            strFile.AppendLine("\t\t\t// TODO: 在这里绑定UI组件");
            strFile.AppendLine("\t\t\t// 例如: m_txtName = m_bindComponent.GetComponent<Text>(0);");
            strFile.AppendLine("\t\t}");
            strFile.AppendLine();
            strFile.AppendLine("\t\tprotected override void OnCreate()");
            strFile.AppendLine("\t\t{");
            strFile.AppendLine("\t\t\tbase.OnCreate();");
            strFile.AppendLine("\t\t\t");
            strFile.AppendLine("\t\t\t// TODO: 在这里绑定事件");
            strFile.AppendLine("\t\t}");
            strFile.AppendLine();
            strFile.AppendLine("\t\tprotected override void OnDestroy()");
            strFile.AppendLine("\t\t{");
            strFile.AppendLine("\t\t\tbase.OnDestroy();");
            strFile.AppendLine("\t\t\t");
            strFile.AppendLine("\t\t\t// TODO: 在这里清理事件");
            strFile.AppendLine("\t\t}");
            strFile.AppendLine();
            strFile.AppendLine("\t\t#endregion");
            strFile.AppendLine();
            strFile.AppendLine("\t\t#region IListDataItem 实现");
            strFile.AppendLine();
            strFile.AppendLine("\t\t/// <summary>");
            strFile.AppendLine("\t\t/// 设置Item数据 - IListDataItem接口实现");
            strFile.AppendLine("\t\t/// </summary>");
            strFile.AppendLine($"\t\tpublic void SetItemData({dataTypeName} data)");
            strFile.AppendLine("\t\t{");
            strFile.AppendLine("\t\t\tm_data = data;");
            strFile.AppendLine();
            strFile.AppendLine("\t\t\t// 空值检查，防止删除动画期间数据已被移除");
            strFile.AppendLine("\t\t\tif (data == null) return;");
            strFile.AppendLine();
            strFile.AppendLine("\t\t\t// 更新DataId（用于动画系统识别）");
            strFile.AppendLine("\t\t\tDataId = data.Id;");
            strFile.AppendLine();
            strFile.AppendLine("\t\t\t// 更新UI显示");
            strFile.AppendLine("\t\t\tRefreshUI();");
            strFile.AppendLine("\t\t}");
            strFile.AppendLine();
            strFile.AppendLine("\t\t/// <summary>");
            strFile.AppendLine("\t\t/// 刷新UI显示");
            strFile.AppendLine("\t\t/// </summary>");
            strFile.AppendLine("\t\tprivate void RefreshUI()");
            strFile.AppendLine("\t\t{");
            strFile.AppendLine("\t\t\tif (m_data == null) return;");
            strFile.AppendLine();
            strFile.AppendLine("\t\t\t// TODO: 在这里更新UI显示");
            strFile.AppendLine("\t\t\t// 例如: if (m_txtName != null) m_txtName.text = m_data.Name;");
            strFile.AppendLine("\t\t}");
            strFile.AppendLine();
            strFile.AppendLine("\t\t#endregion");
            strFile.AppendLine();
            strFile.AppendLine("\t\t#region 事件处理");
            strFile.AppendLine();
            strFile.AppendLine("\t\t/// <summary>");
            strFile.AppendLine("\t\t/// Item点击事件（可选）");
            strFile.AppendLine("\t\t/// </summary>");
            strFile.AppendLine("\t\tprivate void OnItemClicked()");
            strFile.AppendLine("\t\t{");
            strFile.AppendLine("\t\t\tif (m_data == null) return;");
            strFile.AppendLine();
            strFile.AppendLine("\t\t\tDebug.Log($\"[{itemTypeName}] 点击了Item: {m_data.Name} (Id: {m_data.Id}, Index: {Index})\");");
            strFile.AppendLine();
            strFile.AppendLine("\t\t\t// TODO: 处理点击事件");
            strFile.AppendLine("\t\t\t// 可以发送事件通知外部");
            strFile.AppendLine("\t\t\t// GameEvent.Send(\"OnItemClicked\", m_data);");
            strFile.AppendLine("\t\t}");
            strFile.AppendLine();
            strFile.AppendLine("\t\t#endregion");
            strFile.AppendLine();
            strFile.AppendLine("\t\t#region 动画相关重写（可选）");
            strFile.AppendLine();
            strFile.AppendLine("\t\t/// <summary>");
            strFile.AppendLine("\t\t/// 重写动画应用方法（可选，用于自定义动画效果）");
            strFile.AppendLine("\t\t/// </summary>");
            strFile.AppendLine("\t\tprotected override void ApplyAnimation()");
            strFile.AppendLine("\t\t{");
            strFile.AppendLine("\t\t\tbase.ApplyAnimation();");
            strFile.AppendLine();
            strFile.AppendLine("\t\t\t// TODO: 可以在这里添加额外的动画效果");
            strFile.AppendLine("\t\t}");
            strFile.AppendLine();
            strFile.AppendLine("\t\t/// <summary>");
            strFile.AppendLine("\t\t/// 重写回收方法");
            strFile.AppendLine("\t\t/// </summary>");
            strFile.AppendLine("\t\tpublic override void OnRecycle()");
            strFile.AppendLine("\t\t{");
            strFile.AppendLine("\t\t\tbase.OnRecycle();");
            strFile.AppendLine();
            strFile.AppendLine("\t\t\t// 清理数据引用");
            strFile.AppendLine("\t\tm_data = null;");
            strFile.AppendLine("\t\t}");
            strFile.AppendLine();
            strFile.AppendLine("\t\t#endregion");
            strFile.AppendLine("\t}");
            strFile.AppendLine("}");

            return strFile.ToString();
        }

        /// <summary>
        /// 生成 Data 类代码（已废弃，不再使用）
        /// </summary>
        [System.Obsolete("不再生成 Data 类，只生成 UIWindow 代码")]
        private static string GenerateDataClassCode(string dataTypeName)
        {
            StringBuilder strFile = new StringBuilder();
            strFile.AppendLine("//----------------------------------------------------------");
            strFile.AppendLine("// <auto-generated>");
            strFile.AppendLine("// -This code was generated.");
            strFile.AppendLine("// -Changes to this file may cause incorrect behavior.");
            strFile.AppendLine("// -will be lost if the code is regenerated.");
            strFile.AppendLine("// <auto-generated/>");
            strFile.AppendLine("//----------------------------------------------------------");
            strFile.AppendLine("using UnityEngine;");
            strFile.AppendLine();
            strFile.AppendLine($"namespace {ScriptGeneratorSetting.GetUINameSpace()}");
            strFile.AppendLine("{");
            strFile.AppendLine($"\t/// <summary>");
            strFile.AppendLine($"\t/// {dataTypeName} - 数据模型类");
            strFile.AppendLine($"\t/// </summary>");
            strFile.AppendLine($"\tpublic class {dataTypeName}");
            strFile.AppendLine("\t{");
            strFile.AppendLine("\t\t/// <summary>");
            strFile.AppendLine("\t\t/// 唯一ID");
            strFile.AppendLine("\t\t/// </summary>");
            strFile.AppendLine("\t\tpublic int Id;");
            strFile.AppendLine();
            strFile.AppendLine("\t\t/// <summary>");
            strFile.AppendLine("\t\t/// 名称");
            strFile.AppendLine("\t\t/// </summary>");
            strFile.AppendLine("\t\tpublic string Name;");
            strFile.AppendLine();
            strFile.AppendLine("\t\t/// <summary>");
            strFile.AppendLine("\t\t/// 描述");
            strFile.AppendLine("\t\t/// </summary>");
            strFile.AppendLine("\t\tpublic string Desc;");
            strFile.AppendLine();
            strFile.AppendLine("\t\t/// <summary>");
            strFile.AppendLine("\t\t/// 图标索引");
            strFile.AppendLine("\t\t/// </summary>");
            strFile.AppendLine("\t\tpublic int IconIndex;");
            strFile.AppendLine();
            strFile.AppendLine("\t\t/// <summary>");
            strFile.AppendLine("\t\t/// 创建时间戳");
            strFile.AppendLine("\t\t/// </summary>");
            strFile.AppendLine("\t\tpublic float CreateTime;");
            strFile.AppendLine();
            strFile.AppendLine("\t\tprivate static int _idCounter = 0;");
            strFile.AppendLine();
            strFile.AppendLine("\t\t/// <summary>");
            strFile.AppendLine("\t\t/// 创建新的数据");
            strFile.AppendLine("\t\t/// </summary>");
            strFile.AppendLine("\t\tpublic static " + dataTypeName + " Create(string name = null)");
            strFile.AppendLine("\t\t{");
            strFile.AppendLine("\t\t\t_idCounter++;");
            strFile.AppendLine($"\t\t\treturn new {dataTypeName}");
            strFile.AppendLine("\t\t\t{");
            strFile.AppendLine("\t\t\t\tId = _idCounter,");
            strFile.AppendLine("\t\t\t\tName = name ?? $\"Item {_idCounter}\",");
            strFile.AppendLine("\t\t\t\tDesc = $\"这是第 {_idCounter} 条数据\",");
            strFile.AppendLine("\t\t\t\tIconIndex = Random.Range(0, 50),");
            strFile.AppendLine("\t\t\t\tCreateTime = Time.time");
            strFile.AppendLine("\t\t\t};");
            strFile.AppendLine("\t\t}");
            strFile.AppendLine();
            strFile.AppendLine("\t\t/// <summary>");
            strFile.AppendLine("\t\t/// 批量创建数据");
            strFile.AppendLine("\t\t/// </summary>");
            strFile.AppendLine("\t\tpublic static System.Collections.Generic.List<" + dataTypeName + "> CreateBatch(int count)");
            strFile.AppendLine("\t\t{");
            strFile.AppendLine($"\t\t\tvar list = new System.Collections.Generic.List<{dataTypeName}>();");
            strFile.AppendLine("\t\t\tfor (int i = 0; i < count; i++)");
            strFile.AppendLine("\t\t\t{");
            strFile.AppendLine("\t\t\t\tlist.Add(Create());");
            strFile.AppendLine("\t\t\t}");
            strFile.AppendLine("\t\t\treturn list;");
            strFile.AppendLine("\t\t}");
            strFile.AppendLine();
            strFile.AppendLine("\t\t/// <summary>");
            strFile.AppendLine("\t\t/// 重置ID计数器");
            strFile.AppendLine("\t\t/// </summary>");
            strFile.AppendLine("\t\tpublic static void ResetIdCounter()");
            strFile.AppendLine("\t\t{");
            strFile.AppendLine("\t\t\t_idCounter = 0;");
            strFile.AppendLine("\t\t}");
            strFile.AppendLine("\t}");
            strFile.AppendLine("}");

            return strFile.ToString();
        }

        public static bool GenerateCSharpScript(bool includeListener, bool isUniTask = false,
            bool isAutoGenerate = false, string savePath = null, bool isAutoDiff = true, bool m_isWidget = false)
        {
            var root = Selection.activeTransform;

            if (root == null)
            {
                return false;
            }

            CheckVariableNames();
            StringBuilder strVar = new StringBuilder();
            StringBuilder strBind = new StringBuilder();
            StringBuilder strOnCreate = new StringBuilder();
            StringBuilder strCallback = new StringBuilder();

            var widgetPrefix = GetUIWidgetName();
            string fileName = $"{root.name}.cs";
            string uiType = "UIWindow";

            if (root.name.StartsWith(widgetPrefix))
            {
                uiType = "UIWidget";
                fileName = $"{root.name.Replace(GetUIWidgetName(), string.Empty)}.cs";
            }

            if (!isAutoDiff)
            {
                if (m_isWidget)
                {
                    uiType = "UIWidget";
                }
                else
                {
                    uiType = "UIWindow";
                }

                fileName = $"{root.name}.cs";
            }

            strVar.AppendLine($"\t\tprivate UIBindComponent m_bindComponent;");

            strBind.AppendLine($"\t\t\tm_bindComponent = gameObject.GetComponent<UIBindComponent>();");
            m_bindIndex = 0;
            AutoErgodic(root, root, ref strVar, ref strBind, ref strOnCreate, ref strCallback, isUniTask);
            StringBuilder strFile = new StringBuilder();

            if (includeListener)
            {
                strFile.AppendLine("//----------------------------------------------------------");
                strFile.AppendLine("// <auto-generated>");
                strFile.AppendLine("// -This code was generated.");
                strFile.AppendLine("// -Changes to this file may cause incorrect behavior.");
                strFile.AppendLine("// -will be lost if the code is regenerated.");
                strFile.AppendLine("// <auto-generated/>");
                strFile.AppendLine("//----------------------------------------------------------");
#if ENABLE_TEXTMESHPRO
                strFile.AppendLine("using TMPro;");
#endif
                if (isUniTask)
                {
                    strFile.AppendLine("using Cysharp.Threading.Tasks;");
                }

                strFile.AppendLine("using UnityEngine;");
                strFile.AppendLine("using UnityEngine.UI;");
                strFile.AppendLine("using TEngine;");
                strFile.AppendLine();
                strFile.AppendLine($"namespace {ScriptGeneratorSetting.GetUINameSpace()}");
                strFile.AppendLine("{");
                {
                    if (isAutoDiff)
                    {
                        if (root.name.StartsWith(widgetPrefix))
                        {
                            strFile.AppendLine($"\tpublic partial class {fileName.Replace(".cs", "")} : {uiType}");
                        }
                        else
                        {
                            strFile.AppendLine($"\t[Window(UILayer.UI, location : \"{fileName.Replace(".cs", "")}\")]");
                            strFile.AppendLine($"\tpublic partial class {fileName.Replace(".cs", "")} : {uiType}");
                        }
                    }
                    else
                    {
                        if (!m_isWidget)
                        {
                            strFile.AppendLine($"\t[Window(UILayer.UI, location : \"{fileName.Replace(".cs", "")}\")]");
                        }

                        strFile.AppendLine($"\tpublic partial class {fileName.Replace(".cs", "")} : {uiType}");
                    }

                    strFile.AppendLine("\t{");
                }
            }

            // 脚本工具生成的代码
            strFile.AppendLine($"\t\t#region 脚本工具生成的代码");
            strFile.AppendLine();
            strFile.AppendLine(strVar.ToString());
            strFile.AppendLine("\t\tprotected override void ScriptGenerator()");
            strFile.AppendLine("\t\t{");
            {
                strFile.Append(strBind.ToString());
                strFile.Append(strOnCreate.ToString());
            }
            strFile.AppendLine("\t\t}");
            strFile.AppendLine();
            strFile.Append($"\t\t#endregion");
            strFile.AppendLine();

            if (includeListener)
            {
                strFile.AppendLine();
                strFile.AppendLine("\t\t#region 事件");
                strFile.AppendLine();
                strFile.Append(strCallback.ToString());
                strFile.AppendLine($"\t\t#endregion");
                strFile.AppendLine("\t}");
                strFile.AppendLine("}");
            }

            m_textEditor.Delete();
            m_textEditor.text = strFile.ToString();
            m_textEditor.SelectAll();
            m_textEditor.Copy();

            if (isAutoGenerate)
            {
                string path = savePath?.Replace("\\", "/");

                if (string.IsNullOrEmpty(path))
                {
                    return false;
                }

                if (!Directory.Exists(path))
                {
                    Directory.CreateDirectory(path);
                }

                var saveFileName = fileName.Replace(".cs", ".g.cs");
                var filePath = Path.Combine(path, saveFileName).Replace("\\", "/");

                if (File.Exists(filePath))
                {
                    FileAttributes attributes = File.GetAttributes(filePath);
                    if ((attributes & FileAttributes.ReadOnly) == FileAttributes.ReadOnly)
                    {
                        File.SetAttributes(filePath, attributes & ~FileAttributes.ReadOnly);
                    }
                    File.Delete(filePath);
                    AssetDatabase.Refresh();
                }

                File.WriteAllText(filePath, strFile.ToString(), Encoding.UTF8);
                File.SetAttributes(filePath, File.GetAttributes(filePath) | FileAttributes.ReadOnly);
                AssetDatabase.Refresh();
            }
            else
            {
                Debug.Log($"<color=#1E90FF>脚本已生成到剪贴板，请自行Ctl+V粘贴</color>");
            }

            return true;
        }

        private static int m_bindIndex = 0;
        public static void AutoErgodic(Transform root, Transform transform, ref StringBuilder strVar,
            ref StringBuilder strBind, ref StringBuilder strOnCreate, ref StringBuilder strCallback, bool isUniTask)
        {
            for (int i = 0; i < transform.childCount; i++)
            {
                Transform child = transform.GetChild(i);
                WriteAutoScript(root, child, ref strVar, ref strBind, ref strOnCreate, ref strCallback, isUniTask);
                // 跳过 "m_item"
                if (child.name.StartsWith(GetUIWidgetName()))
                {
                    continue;
                }

                AutoErgodic(root, child, ref strVar, ref strBind, ref strOnCreate, ref strCallback, isUniTask);
            }
        }

        private static void WriteAutoScript(Transform root, Transform child, ref StringBuilder strVar,
            ref StringBuilder strBind, ref StringBuilder strOnCreate, ref StringBuilder strCallback, bool isUniTask)
        {
            string varName = child.name;
            // 查找相关的规则定义
            var rule = ScriptGeneratorSetting.GetScriptGenerateRule()
                .Find(r => varName.StartsWith(r.uiElementRegex));

            if (rule == null)
            {
                return;
            }

            var componentName = rule.componentName.ToString();

            if (string.IsNullOrEmpty(componentName))
            {
                return;
            }

            varName = GetVariableName(varName);

            if (string.IsNullOrEmpty(varName))
            {
                return;
            }

            // string varPath = GetRelativePath(child, root);
            strVar.AppendLine($"\t\tprivate {componentName} {varName};");

            if (rule.componentName == UIComponentName.GameObject)
            {
                strBind.AppendLine($"\t\t\t{varName} = m_bindComponent.GetComponent<RectTransform>({m_bindIndex}).gameObject;");
            }
            else
            {
                strBind.AppendLine($"\t\t\t{varName} = m_bindComponent.GetComponent<{componentName}>({m_bindIndex});");
            }
            m_bindIndex++;

            switch (rule.componentName)
            {
                case UIComponentName.Button:
                    var btnFuncName = GetBtnFuncName(varName);

                    if (isUniTask)
                    {
                        strOnCreate.AppendLine(
                            $"\t\t\t{varName}.onClick.AddListener(UniTask.UnityAction({btnFuncName}));");
                        strCallback.AppendLine($"\t\tprivate partial UniTaskVoid {btnFuncName}();");
                        // strCallback.AppendLine("\t\t{");
                        // strCallback.AppendLine("\t\t\tawait UniTask.Yield();");
                        // strCallback.AppendLine("\t\t}");
                    }
                    else
                    {
                        strOnCreate.AppendLine($"\t\t\t{varName}.onClick.AddListener({btnFuncName});");
                        strCallback.AppendLine($"\t\tprivate partial void {btnFuncName}();");
                        // strCallback.AppendLine("\t\t{");
                        // strCallback.AppendLine("\t\t}");
                    }

                    strCallback.AppendLine();
                    break;

                case UIComponentName.Toggle:
                    var toggleFuncName = GetToggleFuncName(varName);
                    strOnCreate.AppendLine($"\t\t\t{varName}.onValueChanged.AddListener({toggleFuncName});");
                    strCallback.AppendLine($"\t\tprivate partial void {toggleFuncName}(bool isOn);");
                    // strCallback.AppendLine("\t\t{");
                    // strCallback.AppendLine("\t\t}");
                    strCallback.AppendLine();
                    break;

                case UIComponentName.Slider:
                    var sliderFuncName = GetSliderFuncName(varName);
                    strOnCreate.AppendLine($"\t\t\t{varName}.onValueChanged.AddListener({sliderFuncName});");
                    strCallback.AppendLine($"\t\tprivate partial void {sliderFuncName}(float value);");
                    // strCallback.AppendLine("\t\t{");
                    // strCallback.AppendLine("\t\t}");
                    strCallback.AppendLine();
                    break;
            }
        }

        public static bool GenerateUIComponentScript()
        {
            var root = Selection.activeTransform;

            if (root == null)
            {
                return false;
            }
            // 检查是否在预制体编辑模式下
            bool isInPrefabMode = IsInPrefabMode(root.gameObject);

            CheckVariableNames();
            var uiComponent = AddComponent3Window();

            if (uiComponent == null)
            {
                return false;
            }

            ErgodicUIComponent(root, root, uiComponent);
            // 如果是预制体模式，需要特殊处理保存
            if (isInPrefabMode)
            {
                SavePrefabChanges(root.gameObject);
            }
            AssetDatabase.Refresh();
            return true;
            // Debug.Log($"<color=#1E90FF>脚本已生成到剪贴板，请自行Ctl+V粘贴</color>");
        }

        public static void ErgodicUIComponent(Transform root, Transform transform, UIBindComponent uiBindComponent)
        {
            for (int i = 0; i < transform.childCount; i++)
            {
                Transform child = transform.GetChild(i);
                WriteScriptUIComponent(root, child, uiBindComponent);

                // 跳过 "m_item"
                if (child.name.StartsWith(GetUIWidgetName()))
                {
                    continue;
                }

                ErgodicUIComponent(root, child, uiBindComponent);
            }
        }

        private static void WriteScriptUIComponent(Transform root, Transform child, UIBindComponent uiBindComponent)
        {
            string varName = child.name;
            // 查找相关的规则定义
            var rule = ScriptGeneratorSetting.GetScriptGenerateRule()
                .Find(r => varName.StartsWith(r.uiElementRegex));

            if (rule == null)
            {
                return;
            }

            var componentName = rule.componentName.ToString();

            if (string.IsNullOrEmpty(componentName))
            {
                return;
            }

            if (rule.componentName == UIComponentName.GameObject)
            {
                var c = child.gameObject.GetComponent<RectTransform>();
                uiBindComponent.AddComponent(c);
                return;
            }

            Type componentType = GetComponentTypeFromEnumName(rule.componentName);

            if (componentType == null)
            {
                componentType = GetComponentTypeFromEnumName(componentName);

                if (componentType == null)
                {
                    Debug.LogWarning($"未找到对应的组件类型: {componentName}");
                    return;
                }
            }

            varName = GetVariableName(varName);

            if (string.IsNullOrEmpty(varName))
            {
                return;
            }

            var com = child.GetComponent(componentType);
            uiBindComponent.AddComponent(com);
        }

        private static Type GetComponentTypeFromEnumName(string enumName)
        {
            Type type = Type.GetType($"UnityEngine.{enumName}, UnityEngine");
            if (type != null) return type;

            type = Type.GetType($"UnityEngine.UI.{enumName}, UnityEngine.UI");
            if (type != null) return type;

            type = Type.GetType($"GameLogic.{enumName}, GameLogic");
            if (type != null) return type;

            type = Type.GetType(enumName);
            if (type != null) return type;

            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                type = assembly.GetType(enumName);
                if (type != null) return type;

                type = assembly.GetType($"UnityEngine.{enumName}");
                if (type != null) return type;

                type = assembly.GetType($"UnityEngine.UI.{enumName}");
                if (type != null) return type;

                type = assembly.GetType($"GameLogic.{enumName}");
                if (type != null) return type;
            }

            return null;
        }

        private static Type GetComponentTypeFromEnumName(UIComponentName enumName)
        {
            return enumName switch
            {
                UIComponentName.GameObject => typeof(GameObject),
                UIComponentName.Button => typeof(Button),
                UIComponentName.Toggle => typeof(Toggle),
                UIComponentName.Slider => typeof(Slider),
                UIComponentName.Text => typeof(Text),
                UIComponentName.Canvas => typeof(Canvas),
                UIComponentName.Image => typeof(Image),
                UIComponentName.RectTransform => typeof(RectTransform),
                UIComponentName.Transform => typeof(Transform),
                UIComponentName.AnimationCurve => typeof(AnimationCurve),
                UIComponentName.Scrollbar => typeof(Scrollbar),
                UIComponentName.ScrollRect => typeof(ScrollRect),
                UIComponentName.CanvasGroup => typeof(CanvasGroup),
                UIComponentName.InputField => typeof(InputField),
                UIComponentName.ToggleGroup => typeof(ToggleGroup),
                UIComponentName.RawImage => typeof(RawImage),
                UIComponentName.GridLayoutGroup => typeof(GridLayoutGroup),
                UIComponentName.HorizontalLayoutGroup => typeof(HorizontalLayoutGroup),
                UIComponentName.VerticalLayoutGroup => typeof(VerticalLayoutGroup),
                UIComponentName.Dropdown => typeof(Dropdown),
                UIComponentName.TextMeshProUGUI => typeof(TextMeshProUGUI),
                _ => null,
            };
        }

        private static UIBindComponent AddComponent3Window()
        {
            var root = Selection.activeTransform;

            if (root == null)
            {
                Debug.LogWarning("请先选中一个物体，再进行脚本生成");
                return null;
            }

            GameObject rootObj = root.gameObject;

            var compt = rootObj.GetComponent<UIBindComponent>();

            if (compt == null)
            {
                compt = rootObj.AddComponent<UIBindComponent>();
            }

            compt.Clear();
            return compt;
        }

        private static bool IsInPrefabMode(GameObject gameObject)
        {
#if UNITY_EDITOR
            if (PrefabStageUtility.GetCurrentPrefabStage() != null)
            {
                return true;
            }

            var prefabAssetType = PrefabUtility.GetPrefabAssetType(gameObject);

            if (prefabAssetType != PrefabAssetType.NotAPrefab)
            {
                return true;
            }

            var prefabInstanceStatus = PrefabUtility.GetPrefabInstanceStatus(gameObject);
            return prefabInstanceStatus != PrefabInstanceStatus.NotAPrefab;
#else
            return false;
#endif
        }

        private static void SavePrefabChanges(GameObject prefabObject)
        {
#if UNITY_EDITOR
            try
            {
                EditorUtility.SetDirty(prefabObject);
                var prefabInstanceStatus = PrefabUtility.GetPrefabInstanceStatus(prefabObject);

                if (prefabInstanceStatus == PrefabInstanceStatus.Connected)
                {
                    var rootPrefab = PrefabUtility.GetCorrespondingObjectFromOriginalSource(prefabObject);

                    if (rootPrefab != null)
                    {
                        PrefabUtility.ApplyPrefabInstance(prefabObject,
                            InteractionMode.AutomatedAction);
                    }
                }
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                // Debug.Log("预制体修改已保存");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"保存预制体时出错: {e.Message}");
            }
#endif
        }

        private static Transform FindDeepChild(Transform parent, string childName)
        {
            // 先在直接子级中查找
            Transform result = parent.Find(childName);
            if (result != null)
                return result;

            // 递归在子级的子级中查找
            foreach (Transform child in parent)
            {
                result = FindDeepChild(child, childName);
                if (result != null)
                    return result;
            }

            return null;
        }

        private static string GetPrefixNameByCodeStyle(UIFieldCodeStyle style)
        {
            return ScriptGeneratorSetting.GetPrefixNameByCodeStyle(style);
        }

        private static string GetUIWidgetName()
        {
            return GetComponentName(ScriptGeneratorSetting.GetWidgetName());
        }

        private static string GetComponentName(string componentName)
        {
            return GetPrefixName() + componentName;
        }

        private static string GetPrefixName()
        {
            return ScriptGeneratorSetting.GetPrefixNameByCodeStyle(ScriptGeneratorSetting.Instance.CodeStyle);
        }

        private static string GetVariableName(string varName)
        {
            if (string.IsNullOrEmpty(varName))
            {
                return varName;
            }

            for (int i = 0; i < VARIABLE_NAME_REGEX.Length; i++)
            {
                var prefix = VARIABLE_NAME_REGEX[i];
                if (varName.StartsWith(prefix))
                {
                    varName = varName.Replace(prefix, string.Empty);
                    varName = GetComponentName(varName);
                    break;
                }
            }
            return varName;
        }
    }

    /// <summary>
    /// UILoopListWidgetOptimized 配置窗口
    /// 用于让用户输入 Item 类型和 Data 类型（仅用于泛型参数）
    /// </summary>
    public class UILoopListConfigWindow : EditorWindow
    {
        private Transform m_root;
        private System.Action<Transform, string, string, bool> m_callback;
        private string m_itemTypeName = "";
        private string m_dataTypeName = "";

        public void Initialize(Transform root, System.Action<Transform, string, string, bool> callback)
        {
            m_root = root;
            m_callback = callback;
            
            // 根据窗口名称自动生成默认的 Item 和 Data 类型名称
            if (root != null)
            {
                string windowName = root.name;
                // 移除可能的 UI 前缀
                if (windowName.EndsWith("UI"))
                {
                    windowName = windowName.Substring(0, windowName.Length - 2);
                }
                
                m_itemTypeName = windowName + "Item";
                m_dataTypeName = windowName + "Data";
            }
        }

        private void OnGUI()
        {
            GUILayout.Space(10);
            
            EditorGUILayout.LabelField("配置列表数据类型", EditorStyles.boldLabel);
            EditorGUILayout.Space();
            
            EditorGUILayout.HelpBox("请输入 Item 类型和 Data 类型（用于泛型参数），只生成 UIWindow 代码", MessageType.Info);
            EditorGUILayout.Space();
            
            EditorGUILayout.BeginVertical("Box");
            {
                EditorGUILayout.LabelField("Item 类型 (继承自 UILoopAniItemWidget):", EditorStyles.miniLabel);
                m_itemTypeName = EditorGUILayout.TextField("Item 类型:", m_itemTypeName);
                EditorGUILayout.HelpBox("例如: DemoItem, MyCustomItem 等", MessageType.None);
                
                EditorGUILayout.Space(10);
                
                EditorGUILayout.LabelField("Data 类型 (数据模型类):", EditorStyles.miniLabel);
                m_dataTypeName = EditorGUILayout.TextField("Data 类型:", m_dataTypeName);
                EditorGUILayout.HelpBox("例如: DemoData, PlayerData, ItemData 等", MessageType.None);
            }
            EditorGUILayout.EndVertical();
            
            EditorGUILayout.Space(10);
            
            EditorGUILayout.BeginHorizontal();
            {
                if (GUILayout.Button("取消", GUILayout.Height(30)))
                {
                    Close();
                }
                
                GUI.backgroundColor = new Color(0.2f, 0.8f, 0.3f);
                if (GUILayout.Button("生成代码", GUILayout.Height(30)))
                {
                    if (m_callback != null && m_root != null)
                    {
                        string itemType = string.IsNullOrWhiteSpace(m_itemTypeName) ? "UILoopAniItemWidget" : m_itemTypeName.Trim();
                        string dataType = string.IsNullOrWhiteSpace(m_dataTypeName) ? "DemoData" : m_dataTypeName.Trim();
                        
                        // 不再生成 Item 和 Data 类，只生成 UIWindow
                        m_callback(m_root, itemType, dataType, false);
                        Close();
                    }
                }
                GUI.backgroundColor = Color.white;
            }
            EditorGUILayout.EndHorizontal();
        }
    }
}