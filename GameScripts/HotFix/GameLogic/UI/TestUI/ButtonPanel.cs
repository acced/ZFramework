// using System.Collections;
// using System.Collections.Generic;
// using UnityEngine;
// using UnityEngine.UI;

// namespace GameLogic
// {
//     public class ButtonPanel
//     {
//         public LoopListView mLoopListView;
//         public DataSourceMgr<ItemData> mDataSourceMgr;
//         Button mSetCountButton;
//         InputField mSetCountInput;
//         Button mScrollToButton;
//         InputField mScrollToInput;
//         Button mAddButton;
//         InputField mAddInput;
//         Button mBackButton;        

//         public AnimationHelper mAnimationHelper ;

//         public void Start()
//         {
//             mSetCountButton = GameObject.Find("ButtonPanel/ButtonGroupSetCount/SetCountButton").GetComponent<Button>();
//             mSetCountInput = GameObject.Find("ButtonPanel/ButtonGroupSetCount/SetCountInputField").GetComponent<InputField>();
//             mSetCountButton.onClick.AddListener(OnSetCountButtonClicked);
            
//             mScrollToButton = GameObject.Find("ButtonPanel/ButtonGroupScrollTo/ScrollToButton").GetComponent<Button>();
//             mScrollToInput = GameObject.Find("ButtonPanel/ButtonGroupScrollTo/ScrollToInputField").GetComponent<InputField>();
//             mScrollToButton.onClick.AddListener(OnScrollToButtonClicked);

//             mAddButton = GameObject.Find("ButtonPanel/ButtonGroupAdd/AddButton").GetComponent<Button>();
//             mAddButton.onClick.AddListener(OnAddButtonClicked);
//             mAddInput = GameObject.Find("ButtonPanel/ButtonGroupAdd/AddInputField").GetComponent<InputField>();

//             mBackButton = GameObject.Find("ButtonPanel/BackButton").GetComponent<Button>();
//             mBackButton.onClick.AddListener(OnBackButtonClicked);
//         }              

//         void OnSetCountButtonClicked()
//         {
//             int count = 0;
//             if (int.TryParse(mSetCountInput.text, out count) == false)
//             {
//                 return;
//             }
//             if (count < 0)
//             {
//                 return;
//             }
//             mDataSourceMgr.SetDataTotalCount(count);
//             mLoopListView.SetListItemCount(count, false);
//             mLoopListView.RefreshAllShownItem(); 
//         }

//         void OnScrollToButtonClicked()
//         {
//             int itemIndex = 0;
//             if (int.TryParse(mScrollToInput.text, out itemIndex) == false)
//             {
//                 return;
//             }
//             if((itemIndex < 0) || (itemIndex >= mDataSourceMgr.TotalItemCount))
//             {
//                 return;
//             }
//             mLoopListView.MovePanelToItemIndex(itemIndex, 0);
//         }

//         void OnAddButtonClicked()
//         {
//             int itemIndex = 0;
//             if (int.TryParse(mAddInput.text, out itemIndex) == false)
//             {
//                 return;
//             }
//             if ((itemIndex < 0) || (itemIndex > mDataSourceMgr.TotalItemCount))
//             {
//                 return;
//             } 
//             ItemData newData = mDataSourceMgr.InsertData(itemIndex);
//             Debug.Log("OnAddButtonClicked newData: " + newData.mId);
//             mLoopListView.SetListItemCount(mDataSourceMgr.TotalItemCount, false);
//             //mAnimationHelper.StartAnimation(newData.mId,0,1,1f);
//             mLoopListView.RefreshAllShownItem();
//         }       

//         public void Update()
//         {
//             mAnimationHelper.UpdateAllAnimation(Time.deltaTime);
//             List<int> allAnimationKeys = mAnimationHelper.AllAnimationKeys;
//             if(allAnimationKeys.Count > 0)
//             {
//                 Debug.Log("Update allAnimationKeys: " + allAnimationKeys.Count);
//                 foreach(int itemId in allAnimationKeys)
//                 {
//                     Debug.Log("Update itemId: " + itemId);
//                     float val = mAnimationHelper.GetCurAnimationValue(itemId);
//                     LoopListViewItem item = mLoopListView.GetShownItemByItemId(itemId);
//                     Debug.Log("Update item: " + item.ItemIndex);
//                     if (item != null)
//                     {
//                         item.GetComponent<AddAnimationItem>().SetAnimationValue(val);
//                         mLoopListView.OnItemSizeChanged(item.ItemIndex);
//                     }
//                     if(mAnimationHelper.IsAnimationFinished(itemId))
//                     {
//                         mAnimationHelper.RemoveAnimation(itemId);
//                     }
//                 }
//             }
//         }

//         void OnBackButtonClicked()
//         {
//            // ButtonPanelMenuList.BackToMainMenu();
//         }         
//     }
// }
