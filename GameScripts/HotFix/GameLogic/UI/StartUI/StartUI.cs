using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using TEngine;

namespace GameLogic
{
	[Window(UILayer.UI,location:"StartUI")]
	class StartUI : UIWindow
	{
		#region 脚本工具生成的代码
		private Button _btn_start;
		private Button _btn_cn;
		private Button _btn_en;
		private Button _btn_save;
		private Button _btn_load;
		protected override void ScriptGenerator()
		{
			_btn_start = FindChildComponent<Button>("m_btn_start");
			_btn_cn = FindChildComponent<Button>("m_btn_cn");
			_btn_en = FindChildComponent<Button>("m_btn_en");
			_btn_save = FindChildComponent<Button>("m_btn_save");
			_btn_load = FindChildComponent<Button>("m_btn_load");
			_btn_start.onClick.AddListener(UniTask.UnityAction(OnClick_startBtn));
			_btn_cn.onClick.AddListener(UniTask.UnityAction(OnClick_cnBtn));
			_btn_en.onClick.AddListener(UniTask.UnityAction(OnClick_enBtn));
			if (_btn_save != null) _btn_save.onClick.AddListener(UniTask.UnityAction(OnClick_saveBtn));
			if (_btn_load != null) _btn_load.onClick.AddListener(UniTask.UnityAction(OnClick_loadBtn));
		}
		#endregion

		#region 事件
		private async UniTaskVoid OnClick_startBtn()
		{
 await UniTask.Yield();
 
			GameModule.UI.HideUI<StartUI>();
			//StartMenuSystem.Instance.Init().Forget();
		}
		private async UniTaskVoid OnClick_cnBtn()
		{
 await UniTask.Yield();
 GameModule.Localization.Language = Language.ChineseSimplified;
 ConfigSystem.Instance.Load();
        foreach (var item in ConfigSystem.Instance.Tables.TbCharcClassGrowth.DataMap)
        {
            Log.Debug(item.Value.Id + " |" + item.Value.ClassName);
        }
		}
		private async UniTaskVoid OnClick_enBtn()
		{
 await UniTask.Yield();
 GameModule.Localization.Language = Language.English;
		}
		private async UniTaskVoid OnClick_saveBtn()
		{
			await UniTask.Yield();
			GameModule.UI.ShowUI<SavePanel>(SavePanelMode.Save);
		}
		private async UniTaskVoid OnClick_loadBtn()
		{
			await UniTask.Yield();
			GameModule.UI.ShowUI<SavePanel>(SavePanelMode.Load);
		}
		#endregion

	}
}
