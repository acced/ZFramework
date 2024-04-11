/****************************************************
	文件：TypeTextEffect.cs.cs
	作者：周杰
	日期：2024/04/11 10:25:04 星期四
	功能：TypeTextEffect.cs TMP打字效果
    Copyright © 2022-2023 DYFPS 周杰
*****************************************************/

using Cysharp.Threading.Tasks;
using TMPro;
using DG.Tweening;

namespace ZFramework;

public  static partial class MyClass
{
	public static async UniTaskVoid TypeTextEffect(TMP_Text textComponent,UnityAction action)
	{
		string fullText = textComponent.text; // 保存完整的文本内容
		for (int i = 0; i <= fullText.Length; i++)
		{
			textComponent.text = fullText.Substring(0, i);
			await UUniTask.Delay(10);
		}

		// 打字特效完成后，等待stayDuration时间，然后开始淡出效果
		await UUniTask.Delay(1000);
		textComponent.DOFade(0, fadeOutDuration).OnComplete(() =>
		{
			action?.invoke();
		});
		
	}
	
	
} 