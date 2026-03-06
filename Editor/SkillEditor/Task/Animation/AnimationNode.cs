using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;
using SkillEditor.Data;

namespace SkillEditor.Editor
{
    /// <summary>
    /// 动画节点 - 支持拖拽AnimatorController、选择动画、预览播放，并在时间轴上触发效果和Cue
    /// </summary>
    public class AnimationNode : SkillNodeBase<AnimationNodeData>
    {
        private ObjectField _animatorControllerField;

        private PopupField<string> _animationPopup;
        private TextField _animationDurationField;
        private Toggle _isAnimationLoopingToggle;
        private List<string> _animationChoices = new List<string> { "(无)" };
        private VisualElement _animConfigRow;

        private IMGUIContainer _previewContainer;
        private VisualElement _previewSection;
        private Button _playPauseButton;

        private SpritePreviewRenderer _spriteRenderer;

        private TimelineView _timelineView;
        private VisualElement _timelineContainer;
        private bool _timelineSectionFolded = false;

        private bool _editorUpdateRegistered;

        public AnimationNode(Vector2 position) : base(NodeType.Animation, position) { }

        protected override string GetNodeTitle() => "动画";
        protected override float GetNodeWidth() => 1020;

        protected override void CreateContent()
        {
            CreateAnimationConfigSection();
            CreateTimelineSection();
        }

        #region 动画配置区域

        private void CreateAnimationConfigSection()
        {
            var container = new VisualElement
            {
                style =
                {
                    backgroundColor = new Color(56f / 255f, 56f / 255f, 56f / 255f),
                    borderTopLeftRadius = 8,
                    borderTopRightRadius = 8,
                    borderBottomLeftRadius = 8,
                    borderBottomRightRadius = 8,
                    paddingLeft = 8,
                    paddingRight = 8,
                    paddingTop = 6,
                    paddingBottom = 6,
                    marginTop = 8
                }
            };

            // === 第一行：动画控制器资源拖拽 ===
            var row1 = new VisualElement
            {
                style = { flexDirection = FlexDirection.Row, alignItems = Align.Center, marginBottom = 4 }
            };

            _animatorControllerField = new ObjectField("动画控制器")
            {
                objectType = typeof(RuntimeAnimatorController),
                value = TypedData?.animatorController
            };
            _animatorControllerField.style.flexGrow = 0;
            _animatorControllerField.style.width = 300;
            _animatorControllerField.labelElement.style.minWidth = 60;
            _animatorControllerField.RegisterValueChangedCallback(evt =>
            {
                if (TypedData != null)
                {
                    TypedData.animatorController = evt.newValue as RuntimeAnimatorController;
                    OnAnimatorControllerChanged();
                    NotifyDataChanged();
                }
            });
            row1.Add(_animatorControllerField);
            container.Add(row1);

            // === 第二行：动画选择 + 帧数 + 循环 ===
            _animConfigRow = new VisualElement
            {
                style = { flexDirection = FlexDirection.Row, alignItems = Align.Center, marginBottom = 4 }
            };

            _animationPopup = new PopupField<string>("动画", _animationChoices, 0);
            _animationPopup.style.width = 250;
            _animationPopup.style.marginRight = 8;
            _animationPopup.labelElement.style.minWidth = 30;
            _animationPopup.RegisterValueChangedCallback(evt =>
            {
                if (TypedData != null && evt.newValue != "(无)")
                {
                    TypedData.animationName = evt.newValue;
                    OnAnimationSelected(evt.newValue);
                    NotifyDataChanged();
                }
            });
            _animConfigRow.Add(_animationPopup);

            _animationDurationField = new TextField("帧数") { value = TypedData?.animationDuration ?? "10" };
            _animationDurationField.style.width = 100;
            _animationDurationField.style.marginRight = 8;
            _animationDurationField.labelElement.style.minWidth = 30;
            _animationDurationField.RegisterValueChangedCallback(evt =>
            {
                if (TypedData != null)
                {
                    TypedData.animationDuration = evt.newValue;
                    _timelineView?.UpdateDuration();
                    NotifyDataChanged();
                }
            });
            _animConfigRow.Add(_animationDurationField);

            _isAnimationLoopingToggle = new Toggle("循环") { value = TypedData?.isAnimationLooping ?? false };
            _isAnimationLoopingToggle.style.marginRight = 8;
            _isAnimationLoopingToggle.RegisterValueChangedCallback(evt =>
            {
                if (TypedData != null)
                {
                    TypedData.isAnimationLooping = evt.newValue;
                    if (_spriteRenderer != null && _spriteRenderer.IsInitialized && !string.IsNullOrEmpty(TypedData.animationName))
                    {
                        _spriteRenderer.SetAnimation(TypedData.animationName, evt.newValue);
                    }
                    NotifyDataChanged();
                }
            });
            _animConfigRow.Add(_isAnimationLoopingToggle);

            container.Add(_animConfigRow);

            // === 第三行：动画预览区域 ===
            _previewSection = new VisualElement
            {
                style =
                {
                    flexDirection = FlexDirection.Column,
                    alignItems = Align.Center
                }
            };

            _previewContainer = new IMGUIContainer(OnPreviewGUI)
            {
                style =
                {
                    width = 300,
                    height = 200,
                    backgroundColor = new Color(0.15f, 0.15f, 0.15f),
                    borderTopLeftRadius = 4,
                    borderTopRightRadius = 4,
                    borderBottomLeftRadius = 4,
                    borderBottomRightRadius = 4,
                    marginBottom = 4
                }
            };
            _previewSection.Add(_previewContainer);

            var controlRow = new VisualElement
            {
                style = { flexDirection = FlexDirection.Row, alignItems = Align.Center, justifyContent = Justify.Center }
            };

            _playPauseButton = new Button(OnPlayPauseClicked) { text = "> 播放" };
            _playPauseButton.style.width = 80;
            _playPauseButton.style.height = 22;
            controlRow.Add(_playPauseButton);

            _previewSection.Add(controlRow);

            _previewSection.style.display = DisplayStyle.None;
            container.Add(_previewSection);

            mainContainer.Add(container);

            if (TypedData?.animatorController != null)
            {
                OnAnimatorControllerChanged();
            }
        }

        #endregion

        #region 动画预览

        /// <summary>
        /// AnimatorController 变更时的处理
        /// </summary>
        private void OnAnimatorControllerChanged()
        {
            CleanupRenderer();

            var controller = TypedData?.animatorController;
            if (controller == null)
            {
                _previewSection.style.display = DisplayStyle.None;
                ResetAnimationChoices();
                return;
            }

            _spriteRenderer = new SpritePreviewRenderer();
            if (!_spriteRenderer.Initialize(controller))
            {
                CleanupRenderer();
                _previewSection.style.display = DisplayStyle.None;
                ResetAnimationChoices();
                return;
            }

            _previewSection.style.display = DisplayStyle.Flex;

            var names = _spriteRenderer.GetAnimationNames();
            _animationChoices.Clear();
            _animationChoices.Add("(无)");
            _animationChoices.AddRange(names);

            string currentAnim = TypedData?.animationName ?? "";
            if (!string.IsNullOrEmpty(currentAnim) && _animationChoices.Contains(currentAnim))
            {
                _animationPopup.SetValueWithoutNotify(currentAnim);
                OnAnimationSelected(currentAnim);
            }
            else
            {
                _animationPopup.SetValueWithoutNotify("(无)");
            }

            RegisterEditorUpdate();
        }

        /// <summary>
        /// 动画选择后的处理
        /// </summary>
        private void OnAnimationSelected(string animationName)
        {
            if (_spriteRenderer == null || !_spriteRenderer.IsInitialized) return;
            if (string.IsNullOrEmpty(animationName) || animationName == "(无)") return;

            bool loop = TypedData?.isAnimationLooping ?? false;
            _spriteRenderer.SetAnimation(animationName, loop);

            // 从 clip 的实际关键帧数量读取帧数，立刻写入
            int clipFrameCount = _spriteRenderer.GetClipFrameCount(animationName);
            if (clipFrameCount > 0 && TypedData != null)
            {
                TypedData.animationDuration = clipFrameCount.ToString();
                _animationDurationField.SetValueWithoutNotify(clipFrameCount.ToString());
                _timelineView?.UpdateDuration();
            }

            _timelineView?.SetPlaybackIndicatorVisible(true);
            _timelineView?.SetPlaybackFrame(0);

            UpdatePlayPauseButton();
            RepaintPreview();
        }

        /// <summary>
        /// 播放/暂停按钮点击
        /// </summary>
        private void OnPlayPauseClicked()
        {
            if (_spriteRenderer == null || !_spriteRenderer.IsInitialized) return;

            _spriteRenderer.TogglePlayPause();
            UpdatePlayPauseButton();
        }

        /// <summary>
        /// 更新播放/暂停按钮文本
        /// </summary>
        private void UpdatePlayPauseButton()
        {
            if (_playPauseButton == null) return;
            bool playing = _spriteRenderer?.IsPlaying ?? false;
            _playPauseButton.text = playing ? "|| 暂停" : "> 播放";
        }

        /// <summary>
        /// IMGUI预览渲染回调
        /// </summary>
        private void OnPreviewGUI()
        {
            if (_spriteRenderer == null || !_spriteRenderer.IsInitialized) return;

            var texture = _spriteRenderer.RenderResult;
            if (texture != null)
            {
                var rect = GUILayoutUtility.GetRect(300, 200);
                GUI.DrawTexture(rect, texture, ScaleMode.ScaleToFit);
            }

            if (_spriteRenderer.TotalFrames > 0)
            {
                var infoRect = new Rect(4, 180, 292, 18);
                var oldColor = GUI.color;
                GUI.color = new Color(1f, 1f, 1f, 0.7f);
                GUI.Label(infoRect, $"帧: {_spriteRenderer.CurrentFrame} / {_spriteRenderer.TotalFrames}",
                    EditorStyles.miniLabel);
                GUI.color = oldColor;
            }
        }

        /// <summary>
        /// EditorApplication.update 回调 - 驱动预览更新
        /// </summary>
        private void OnEditorUpdate()
        {
            if (_spriteRenderer == null || !_spriteRenderer.IsInitialized) return;

            _spriteRenderer.EditorUpdate();

            if (_spriteRenderer.IsPlaying && _timelineView != null)
            {
                _timelineView.SetPlaybackFrame(_spriteRenderer.CurrentFrame);
            }

            UpdatePlayPauseButton();
            RepaintPreview();
        }

        /// <summary>
        /// 注册 EditorUpdate
        /// </summary>
        private void RegisterEditorUpdate()
        {
            if (_editorUpdateRegistered) return;
            EditorApplication.update += OnEditorUpdate;
            _editorUpdateRegistered = true;
        }

        /// <summary>
        /// 取消注册 EditorUpdate
        /// </summary>
        private void UnregisterEditorUpdate()
        {
            if (!_editorUpdateRegistered) return;
            EditorApplication.update -= OnEditorUpdate;
            _editorUpdateRegistered = false;
        }

        /// <summary>
        /// 清理渲染器
        /// </summary>
        private void CleanupRenderer()
        {
            if (_spriteRenderer != null)
            {
                _spriteRenderer.Cleanup();
                _spriteRenderer = null;
            }
            UnregisterEditorUpdate();
            _timelineView?.SetPlaybackIndicatorVisible(false);
        }

        /// <summary>
        /// 重置动画下拉列表
        /// </summary>
        private void ResetAnimationChoices()
        {
            _animationChoices.Clear();
            _animationChoices.Add("(无)");
            _animationPopup?.SetValueWithoutNotify("(无)");
        }

        /// <summary>
        /// 播放指示器拖拽跳转回调
        /// </summary>
        private void OnPlaybackSeek(int frame)
        {
            if (_spriteRenderer == null || !_spriteRenderer.IsInitialized) return;
            _spriteRenderer.SeekToFrame(frame);
            RepaintPreview();
        }

        /// <summary>
        /// 强制重绘预览区域
        /// </summary>
        private EditorWindow _cachedEditorWindow;
        private void RepaintPreview()
        {
            _previewContainer?.MarkDirtyRepaint();

            if (_cachedEditorWindow == null)
            {
                var panel = _previewContainer?.panel;
                if (panel != null)
                {
                    foreach (var w in Resources.FindObjectsOfTypeAll<EditorWindow>())
                    {
                        if (w.rootVisualElement?.panel == panel)
                        {
                            _cachedEditorWindow = w;
                            break;
                        }
                    }
                }
            }
            _cachedEditorWindow?.Repaint();
        }

        /// <summary>
        /// 绑定播放指示器事件
        /// </summary>
        private void BindPlaybackIndicator()
        {
            if (_timelineView == null) return;

            var indicator = _timelineView.GetPlaybackIndicator();
            if (indicator != null)
            {
                indicator.OnSeekToFrame -= OnPlaybackSeek;
                indicator.OnSeekToFrame += OnPlaybackSeek;
            }
        }

        #endregion

        #region Timeline区域

        private void CreateTimelineSection()
        {
            _timelineContainer = new VisualElement
            {
                name = "TimelineSection",
                style =
                {
                    backgroundColor = new Color(56f / 255f, 56f / 255f, 56f / 255f),
                    borderTopLeftRadius = 8,
                    borderTopRightRadius = 8,
                    borderBottomLeftRadius = 8,
                    borderBottomRightRadius = 8,
                    paddingLeft = 8,
                    paddingRight = 8,
                    paddingTop = 8,
                    paddingBottom = 8,
                    marginTop = 8,
                    minWidth = 1004
                }
            };

            _timelineView = new TimelineView();
            _timelineView.style.display = _timelineSectionFolded ? DisplayStyle.None : DisplayStyle.Flex;
            _timelineView.OnDataChanged += NotifyDataChanged;
            _timelineView.OnAddButtonClicked += OnTimelineAddClicked;
            _timelineContainer.Add(_timelineView);

            mainContainer.Add(_timelineContainer);

            RefreshTimeline();
        }

        private void ToggleTimelineSection()
        {
            _timelineSectionFolded = !_timelineSectionFolded;
            if (_timelineView != null)
                _timelineView.style.display = _timelineSectionFolded ? DisplayStyle.None : DisplayStyle.Flex;
        }

        private void OnTimelineAddClicked()
        {
            var menu = new GenericMenu();
            menu.AddItem(new GUIContent("时间效果"), false, () =>
            {
                if (TypedData == null) return;
                if (TypedData.timeEffects == null)
                    TypedData.timeEffects = new List<TimeEffectData>();

                TypedData.timeEffects.Add(new TimeEffectData());

                if (_timelineSectionFolded)
                    ToggleTimelineSection();

                _timelineView?.AddNewTrack(false);
                RefreshPorts();
                NotifyDataChanged();
            });
            menu.AddItem(new GUIContent("时间Cue"), false, () =>
            {
                if (TypedData == null) return;
                if (TypedData.timeCues == null)
                    TypedData.timeCues = new List<TimeCueData>();

                TypedData.timeCues.Add(new TimeCueData());

                if (_timelineSectionFolded)
                    ToggleTimelineSection();

                _timelineView?.AddNewTrack(true);
                RefreshPorts();
                NotifyDataChanged();
            });
            menu.ShowAsContext();
        }

        private void RefreshTimeline()
        {
            if (_timelineView == null || TypedData == null) return;

            _timelineView.Initialize(TypedData, () =>
            {
                var port = TimelinePort.Create<Edge>(Orientation.Horizontal, Direction.Output, Port.Capacity.Multi, typeof(float));
                return port;
            });

            RefreshPorts();

            BindPlaybackIndicator();
        }

        #endregion

        #region 端口查找

        /// <summary>
        /// 根据端口标识符查找输出端口（支持普通端口和Timeline端口）
        /// </summary>
        public override Port FindOutputPortByIdentifier(string portIdentifier)
        {
            if (_timelineView != null)
            {
                var port = _timelineView.FindPortByIdentifier(portIdentifier);
                if (port != null) return port;
            }

            return base.FindOutputPortByIdentifier(portIdentifier);
        }

        #endregion

        #region 数据加载/保存

        public override void LoadData(NodeData data)
        {
            base.LoadData(data);
            SyncUIFromData();
        }

        public override void SyncUIFromData()
        {
            base.SyncUIFromData();
            if (TypedData == null) return;

            if (_animatorControllerField != null)
                _animatorControllerField.SetValueWithoutNotify(TypedData.animatorController);

            if (_animationDurationField != null)
                _animationDurationField.SetValueWithoutNotify(TypedData.animationDuration ?? "10");

            if (_isAnimationLoopingToggle != null)
                _isAnimationLoopingToggle.SetValueWithoutNotify(TypedData.isAnimationLooping);

            if (TypedData.animatorController != null)
            {
                OnAnimatorControllerChanged();
            }
            else
            {
                ResetAnimationChoices();
            }

            RefreshTimeline();
        }

        #endregion

        #region 生命周期

        ~AnimationNode()
        {
            CleanupRenderer();
        }

        #endregion
    }
}
