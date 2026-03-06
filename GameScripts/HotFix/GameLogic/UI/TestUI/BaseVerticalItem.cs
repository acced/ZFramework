using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace GameLogic
{
    public class BaseVerticalItem : MonoBehaviour
    {
        public Text mNameText;
        public Image mIcon;
        public Image[] mStarArray;
        public Text mDesc;
        public Text mDescExtend;
        public Color32 mRedStarColor = new Color32(249, 227, 101, 255);
        public Color32 mGrayStarColor = new Color32(215, 215, 215, 255);
        
        ItemData mItemData;
        int mItemDataIndex = -1;

        public void Init()
        {
            if (mStarArray != null)
            {
                for(int i = 0;i<mStarArray.Length;++i)
                {
                    int index = i;
                    ClickEventListener listener = ClickEventListener.Get(mStarArray[i].gameObject);
                    listener.SetClickEventHandler(delegate (GameObject obj) { OnStarClicked(index); });
                }
            }           
        }

        void OnStarClicked(int index)
        {
            if(index == 0 && mItemData.mStarCount == 1)
            {
                mItemData.mStarCount = 0;
            }
            else
            {
                mItemData.mStarCount = index + 1;
            }
            SetStarCount(mItemData.mStarCount);
        }

        public void SetStarCount(int count)
        {
            int i = 0;
            for(; i<count;++i)
            {
                mStarArray[i].color = mRedStarColor;
            }
            for (; i < mStarArray.Length; ++i)
            {
                mStarArray[i].color = mGrayStarColor;
            }      
        }

        public void SetItemData(ItemData itemData,int itemIndex)
        {
            mItemData = itemData;
            mItemDataIndex = itemIndex;
            mNameText.text = itemData.mName;            
            mDesc.text = itemData.mDesc;   
            mDescExtend.text = itemData.mDescExtend;
            
            // 直接加载 Sprite，不通过 GameModule
            Sprite[] allSprites = Resources.LoadAll<Sprite>("terrain");

       // Debug.Log($"加载了 {allSprites.Length} 个切片");

        // 2. 遍历使用
        foreach (var s in allSprites)
        {
           if(s.name == itemData.mIcon)
           {
            mIcon.sprite = s;
            break;
           }
        }
          
            
            SetStarCount(itemData.mStarCount);
        }
        
        /// <summary>
        /// 直接加载 Sprite（不通过 GameModule）
        /// </summary>
        private Sprite LoadSpriteDirectly(string path)
        {
            Sprite sprite = null;
            
#if UNITY_EDITOR
            // 编辑器模式：直接从 AssetDatabase 加载
            sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (sprite != null)
            {
                return sprite;
            }
#endif
            
            // 运行时：从 Resources 文件夹加载
            // 如果路径是 "Assets/..." 格式，需要转换为 Resources 路径
            // 例如: "Assets/AssetRaw/UIRaw/Atlas/terrain/icon.png" -> "AssetRaw/UIRaw/Atlas/terrain/icon"
            string resourcesPath;
            if (path.StartsWith("Assets/"))
            {
                // 移除 "Assets/" 前缀和 ".png" 后缀
                resourcesPath = path.Substring(7); // 移除 "Assets/"
                resourcesPath = resourcesPath.Replace(".png", ""); // 移除 ".png"
            }
            else
            {
                // 直接使用传入的路径（假设已经是 Resources 路径格式）
                resourcesPath = path.Replace(".png", "");
            }
            
            // 尝试从 Resources 加载
            sprite = Resources.Load<Sprite>(resourcesPath);
            
            return sprite;
        }      
    }
}
