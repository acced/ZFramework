using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;
using SkillEditor.Data;

namespace SkillEditor.Editor
{
    /// <summary>
    /// 序列帧预览渲染器 - 在编辑器中将 AnimatorController 驱动的序列帧动画渲染到 RenderTexture
    /// 通过 EditorCurveBinding 直接提取 AnimationClip 中的 Sprite 关键帧来实现精确预览
    /// </summary>
    public class SpritePreviewRenderer : IDisposable
    {
        private const int PREVIEW_LAYER = 30;
        private const int PREVIEW_CAMERA_CULLING_MASK = 1 << PREVIEW_LAYER;

        private GameObject _cameraObject;
        private Camera _camera;
        private GameObject _previewObject;
        private SpriteRenderer _spriteRenderer;
        private RenderTexture _renderTexture;

        private bool _isInitialized;
        private bool _isPlaying;
        private int _currentFrame;
        private int _totalFrames;
        private float _animationDuration;
        private float _clipFrameRate;
        private float _playbackTime;
        private double _lastEditorTime;
        private List<string> _animationNames = new List<string>();
        private Dictionary<string, AnimationClip> _clipMap = new Dictionary<string, AnimationClip>();
        private AnimationClip _currentClip;
        private bool _currentLoop;

        // 当前 clip 的所有 Sprite 关键帧（按时间排序）
        private List<SpriteKeyframe> _spriteKeyframes = new List<SpriteKeyframe>();

        private struct SpriteKeyframe
        {
            public float Time;
            public Sprite Sprite;
        }

        private int _width;
        private int _height;

        public int CurrentFrame => _currentFrame;
        public int TotalFrames => _totalFrames;
        public bool IsPlaying => _isPlaying;
        public Texture RenderResult => _renderTexture;
        public bool IsInitialized => _isInitialized;

        /// <summary>
        /// 初始化预览环境
        /// </summary>
        public bool Initialize(RuntimeAnimatorController animatorController, int width = 300, int height = 200)
        {
            Cleanup();

            if (animatorController == null) return false;

            var clips = animatorController.animationClips;
            if (clips == null || clips.Length == 0) return false;

            _width = width;
            _height = height;

            try
            {
                _previewObject = new GameObject("SpritePreviewObject");
                _previewObject.hideFlags = HideFlags.HideAndDontSave;
                _previewObject.layer = PREVIEW_LAYER;

                _spriteRenderer = _previewObject.AddComponent<SpriteRenderer>();
                _spriteRenderer.enabled = false;

                _animationNames.Clear();
                _clipMap.Clear();
                foreach (var clip in clips)
                {
                    if (clip == null) continue;
                    if (!_clipMap.ContainsKey(clip.name))
                    {
                        _animationNames.Add(clip.name);
                        _clipMap[clip.name] = clip;
                    }
                }

                _cameraObject = new GameObject("SpritePreviewCamera");
                _cameraObject.hideFlags = HideFlags.HideAndDontSave;

                _camera = _cameraObject.AddComponent<Camera>();
                _camera.orthographic = true;
                _camera.cullingMask = PREVIEW_CAMERA_CULLING_MASK;
                _camera.nearClipPlane = 0.01f;
                _camera.farClipPlane = 1000f;
                _camera.backgroundColor = new Color(0.15f, 0.15f, 0.15f, 1f);
                _camera.clearFlags = CameraClearFlags.SolidColor;
                _camera.enabled = false;

                _renderTexture = new RenderTexture(_width, _height, 16, RenderTextureFormat.ARGB32);
                _renderTexture.Create();

                _isInitialized = true;
                _lastEditorTime = EditorApplication.timeSinceStartup;

                // 用第一个 clip 的第一帧初始化预览
                if (clips.Length > 0 && clips[0] != null)
                {
                    var keyframes = ExtractSpriteKeyframes(clips[0]);
                    if (keyframes.Count > 0)
                    {
                        _spriteRenderer.sprite = keyframes[0].Sprite;
                        _spriteRenderer.enabled = true;
                        AdjustCamera();
                        _spriteRenderer.enabled = false;
                    }
                }

                RenderFrame();
                return true;
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[SpritePreviewRenderer] 初始化失败: {e.Message}");
                Cleanup();
                return false;
            }
        }

        /// <summary>
        /// 从 AnimationClip 中提取所有 SpriteRenderer.sprite 关键帧
        /// </summary>
        private List<SpriteKeyframe> ExtractSpriteKeyframes(AnimationClip clip)
        {
            var result = new List<SpriteKeyframe>();
            if (clip == null) return result;

            var bindings = AnimationUtility.GetObjectReferenceCurveBindings(clip);
            foreach (var binding in bindings)
            {
                // SpriteRenderer 的 sprite 属性绑定路径为空（根对象）或组件路径，propertyName 为 m_Sprite
                if (binding.type == typeof(SpriteRenderer) && binding.propertyName == "m_Sprite")
                {
                    var keyframes = AnimationUtility.GetObjectReferenceCurve(clip, binding);
                    if (keyframes != null)
                    {
                        foreach (var kf in keyframes)
                        {
                            if (kf.value is Sprite sprite)
                            {
                                result.Add(new SpriteKeyframe { Time = kf.time, Sprite = sprite });
                            }
                        }
                    }
                }
            }

            result.Sort((a, b) => a.Time.CompareTo(b.Time));
            return result;
        }

        /// <summary>
        /// 根据时间获取对应的 Sprite（向前查找最近的关键帧）
        /// </summary>
        private Sprite GetSpriteAtTime(float time)
        {
            if (_spriteKeyframes.Count == 0) return null;

            Sprite result = _spriteKeyframes[0].Sprite;
            for (int i = 0; i < _spriteKeyframes.Count; i++)
            {
                if (_spriteKeyframes[i].Time <= time)
                    result = _spriteKeyframes[i].Sprite;
                else
                    break;
            }
            return result;
        }

        /// <summary>
        /// 将指定时间对应的 Sprite 应用到 SpriteRenderer 并调整相机
        /// </summary>
        private void ApplySpriteAtTime(float time)
        {
            var sprite = GetSpriteAtTime(time);
            if (sprite != null)
            {
                _spriteRenderer.sprite = sprite;
                _spriteRenderer.enabled = true;
                AdjustCamera();
            }
        }

        private void AdjustCamera()
        {
            if (_camera == null || _spriteRenderer == null || _spriteRenderer.sprite == null) return;

            Bounds bounds = _spriteRenderer.bounds;
            if (bounds.size == Vector3.zero)
            {
                _camera.orthographicSize = 1f;
                _camera.transform.position = new Vector3(0, 0, -10f);
            }
            else
            {
                _camera.orthographicSize = Mathf.Max(bounds.size.y, bounds.size.x * _height / _width) * 0.55f;
                _camera.transform.position = bounds.center + new Vector3(0, 0, -10f);
            }
            _camera.transform.rotation = Quaternion.identity;
        }

        /// <summary>
        /// 设置动画
        /// </summary>
        public void SetAnimation(string animationName, bool loop)
        {
            if (!_isInitialized) return;
            if (!_clipMap.TryGetValue(animationName, out var clip)) return;

            _currentClip = clip;
            _currentLoop = loop;
            _animationDuration = clip.length;
            _clipFrameRate = clip.frameRate > 0 ? clip.frameRate : SkillConstants.DEFAULT_FPS;

            _spriteKeyframes = ExtractSpriteKeyframes(clip);
            if (_spriteKeyframes.Count > 1)
            {
                _totalFrames = _spriteKeyframes.Count - 1;
            }
            else
            {
                _totalFrames = Mathf.Max(1, Mathf.RoundToInt(_animationDuration * _clipFrameRate));
            }

            _currentFrame = 0;
            _playbackTime = 0f;
            _isPlaying = false;

            SeekToFrame(0);
        }

        /// <summary>
        /// 获取 clip 基于自身帧率的总帧数（供外部读取写入帧数字段）
        /// </summary>
        public int GetClipFrameCount(string animationName)
        {
            if (!_clipMap.TryGetValue(animationName, out var clip)) return 0;

            var keyframes = ExtractSpriteKeyframes(clip);
            if (keyframes.Count > 1)
                return keyframes.Count;

            float fr = clip.frameRate > 0 ? clip.frameRate : SkillConstants.DEFAULT_FPS;
            return Mathf.Max(1, Mathf.RoundToInt(clip.length * fr));
        }

        /// <summary>
        /// 跳转到指定帧
        /// </summary>
        public void SeekToFrame(int frame)
        {
            if (!_isInitialized || _currentClip == null || _spriteKeyframes.Count == 0) return;

            _currentFrame = Mathf.Clamp(frame, 0, _totalFrames);
            _playbackTime = _totalFrames > 0 ? (_animationDuration * _currentFrame) / _totalFrames : 0f;

            ApplySpriteAtTime(_playbackTime);
            RenderFrame();
        }

        /// <summary>
        /// 切换播放/暂停
        /// </summary>
        public void TogglePlayPause()
        {
            if (!_isInitialized) return;

            _isPlaying = !_isPlaying;
            _lastEditorTime = EditorApplication.timeSinceStartup;
        }

        /// <summary>
        /// 编辑器更新（由 EditorApplication.update 驱动）
        /// </summary>
        public void EditorUpdate()
        {
            if (!_isInitialized || !_isPlaying || _currentClip == null || _spriteKeyframes.Count == 0) return;

            double currentTime = EditorApplication.timeSinceStartup;
            float deltaTime = (float)(currentTime - _lastEditorTime);
            _lastEditorTime = currentTime;
            deltaTime = Mathf.Min(deltaTime, 0.1f);

            _playbackTime += deltaTime;

            if (_currentLoop)
            {
                if (_animationDuration > 0f)
                    _playbackTime %= _animationDuration;
            }
            else
            {
                if (_playbackTime >= _animationDuration)
                {
                    _playbackTime = _animationDuration;
                    _isPlaying = false;
                }
            }

            if (_animationDuration > 0f && _totalFrames > 0)
                _currentFrame = Mathf.Clamp(Mathf.RoundToInt((_playbackTime / _animationDuration) * _totalFrames), 0, _totalFrames);
            else
                _currentFrame = 0;

            ApplySpriteAtTime(_playbackTime);
            RenderFrame();
        }

        /// <summary>
        /// 渲染当前帧到 RenderTexture
        /// </summary>
        public void RenderFrame()
        {
            if (!_isInitialized || _camera == null || _renderTexture == null || _spriteRenderer == null) return;

            _spriteRenderer.enabled = true;

            var prevRT = RenderTexture.active;
            _camera.targetTexture = _renderTexture;
            _camera.Render();
            _camera.targetTexture = null;
            RenderTexture.active = prevRT;

            _spriteRenderer.enabled = false;
        }

        /// <summary>
        /// 获取动画名称列表
        /// </summary>
        public List<string> GetAnimationNames()
        {
            return _animationNames;
        }

        /// <summary>
        /// 清理预览资源
        /// </summary>
        public void Cleanup()
        {
            _isPlaying = false;
            _isInitialized = false;
            _currentFrame = 0;
            _totalFrames = 0;
            _animationDuration = 0;
            _clipFrameRate = 0;
            _playbackTime = 0f;
            _animationNames.Clear();
            _clipMap.Clear();
            _spriteKeyframes.Clear();
            _currentClip = null;

            _spriteRenderer = null;
            _camera = null;

            if (_previewObject != null)
            {
                UnityEngine.Object.DestroyImmediate(_previewObject);
                _previewObject = null;
            }

            if (_cameraObject != null)
            {
                UnityEngine.Object.DestroyImmediate(_cameraObject);
                _cameraObject = null;
            }

            if (_renderTexture != null)
            {
                _renderTexture.Release();
                UnityEngine.Object.DestroyImmediate(_renderTexture);
                _renderTexture = null;
            }
        }

        public void Dispose()
        {
            Cleanup();
        }
    }
}
