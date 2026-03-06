// using System.Collections;
// using System.Collections.Generic;
// using UnityEngine;
// using UnityEngine.UI;

// namespace GameLogic
// {
//     public class ListViewTopToBottomDemoScript : MonoBehaviour
//     {
//         public LoopListView mLoopListView;
//         public int mTotalDataCount = 10000;
//         DataSourceMgr<ItemData> mDataSourceMgr;
//         ButtonPanel mButtonPanel;

//          public AnimationType mAnimaionType = AnimationType.Clip;
//         public float mAnimationTime = 1f;
//         AnimationHelper mAnimationHelper = new AnimationHelper(); 

//         // Use this for initialization
//         void Start()
//         {
//             mDataSourceMgr = new DataSourceMgr<ItemData>(mTotalDataCount);
//             mLoopListView.InitListView(mDataSourceMgr.TotalItemCount, OnGetItemByIndex);
//             InitButtonPanel();
//         }

//         void InitButtonPanel()
//         {
//             mButtonPanel = new ButtonPanel();
//             mButtonPanel.mAnimationHelper = mAnimationHelper;
//             mButtonPanel.mLoopListView = mLoopListView;
//             mButtonPanel.mDataSourceMgr = mDataSourceMgr;
//             mButtonPanel.Start();
//         }       
       
//         LoopListViewItem OnGetItemByIndex(LoopListView listView, int index)
//         {
//             if (index < 0 || index >= mDataSourceMgr.TotalItemCount)
//             {
//                 return null;
//             }
//             //get the data to showing
//             ItemData itemData = mDataSourceMgr.GetItemDataByIndex(index);
//             if(itemData == null)
//             {
//                 return null;
//             }
//             // /*get a new item. Every item can use a different prefab, 
//             //  the parameter of the NewListViewItem is the prefab’name. 
//             // And all the prefabs should be listed in ItemPrefabList in LoopListView Inspector Setting*/
//             // LoopListViewItem item = listView.NewListViewItem("ItemPrefab");
//             // //get your own component
//             // BaseVerticalItem itemScript = item.GetComponent<BaseVerticalItem>();
//             // //IsInitHandlerCalled is false means this item is new created but not fetched from pool.
//             // if (item.IsInitHandlerCalled == false)
//             // {
//             //     item.IsInitHandlerCalled = true;
//             //     itemScript.Init();// here to init the item, such as add button click event listener.
//             // }
//             // //update the item’s content for showing, such as image,text.
//             // itemScript.SetItemData(itemData,index);
//             // return item;

//             //get a new item. Every item can use a different prefab, the parameter of the NewListViewItem is the prefab’name. 
//             //And all the prefabs should be listed in ItemPrefabList in LoopListView2 Inspector Setting
//             LoopListViewItem item = listView.NewListViewItem("ItemPrefab");
//             BaseVerticalItem itemScript = item.GetComponent<BaseVerticalItem>();
//            // UpdateItemColor(itemScript,itemData.mId);
//             if (item.IsInitHandlerCalled == false)
//             {
//                 item.IsInitHandlerCalled = true;
//                 itemScript.Init();
//             }
//             itemScript.SetItemData(itemData,index);
//             // item.ItemId = itemData.mId;
//             // itemScript.SetAnimationType(mAnimaionType);
//             // itemScript.SetItemData(itemData);
//             //itemScript.SetItemSelected(mCurrentSelectItemId == itemData.mId);


//             // float animationValue = mAnimationHelper.GetCurAnimationValue(itemData.mId);
//             // if (animationValue >= 0)
//             // {
//             //     itemScript.SetAnimationValue(animationValue);
//             // }
//             return item;
//         }

//         // void Update()
//         // {
//         //     if(mAnimationHelper == null)
//         //     {
//         //         return;
//         //     }
//         //     mAnimationHelper.UpdateAllAnimation(Time.deltaTime);
//         //     List<int> allAnimationKeys = mAnimationHelper.AllAnimationKeys;
//         //     //Debug.Log("Update allAnimationKeys: " + allAnimationKeys.Count);
//         //     if(allAnimationKeys.Count > 0)
//         //     {
                
//         //         foreach(int itemId in allAnimationKeys)
//         //         {
//         //             float val = mAnimationHelper.GetCurAnimationValue(itemId);
//         //             LoopListViewItem item = mLoopListView.GetShownItemByItemId(itemId);
//         //             if (item != null)
//         //             {
//         //                 item.GetComponent<AddAnimationItem>().SetAnimationValue(val);
//         //                 mLoopListView.OnItemSizeChanged(item.ItemIndex);
//         //             }
//         //             if(mAnimationHelper.IsAnimationFinished(itemId))
//         //             {
//         //                 mAnimationHelper.RemoveAnimation(itemId);
//         //             }
//         //         }
//         //     }
//         // }
//     }
// }
