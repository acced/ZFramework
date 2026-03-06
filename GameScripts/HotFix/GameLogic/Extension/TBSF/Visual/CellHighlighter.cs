using System.Collections.Generic;
using TBSF.Core;
using UnityEngine;

namespace TBSF.Visual
{
    /// <summary>
    /// 高亮类型
    /// </summary>
    public enum HighlightType
    {
        None,
        MoveRange,
        AttackRange,
        SkillPreview,
        SkillEffect,
        Path,
        Selected,
        Danger
    }

    /// <summary>
    /// 格子高亮管理器 - 统一管理所有格子的高亮显示
    /// 使用对象池和 MaterialPropertyBlock 复用来避免 GC
    /// </summary>
    public sealed class CellHighlighter : MonoBehaviour
    {
        [Header("高亮颜色")]
        [SerializeField] private Color _moveRangeColor = new Color(0.2f, 0.6f, 1f, 0.4f);
        [SerializeField] private Color _attackRangeColor = new Color(1f, 0.3f, 0.3f, 0.4f);
        [SerializeField] private Color _skillPreviewColor = new Color(1f, 1f, 0.2f, 0.4f);
        [SerializeField] private Color _skillEffectColor = new Color(1f, 0.5f, 0f, 0.5f);
        [SerializeField] private Color _pathColor = new Color(0.2f, 1f, 0.4f, 0.6f);
        [SerializeField] private Color _selectedColor = new Color(1f, 1f, 1f, 0.5f);
        [SerializeField] private Color _dangerColor = new Color(0.8f, 0f, 0f, 0.5f);

        private static readonly int s_ColorId = Shader.PropertyToID("_Color");

        private readonly Dictionary<HexCoordinates, HighlightType> _highlights =
            new Dictionary<HexCoordinates, HighlightType>();

        private readonly Dictionary<HexCoordinates, GameObject> _highlightObjects =
            new Dictionary<HexCoordinates, GameObject>();

        private readonly Dictionary<HighlightType, HashSet<HexCoordinates>> _typeIndex =
            new Dictionary<HighlightType, HashSet<HexCoordinates>>();

        private readonly Queue<GameObject> _pool = new Queue<GameObject>();
        private readonly List<HexCoordinates> _clearBuffer = new List<HexCoordinates>();

        private MaterialPropertyBlock _mpb;

        [SerializeField] private GameObject _highlightPrefab;

        public IReadOnlyDictionary<HexCoordinates, HighlightType> ActiveHighlights => _highlights;

        private void Awake()
        {
            _mpb = new MaterialPropertyBlock();
        }

        public void SetHighlight(HexCoordinates coord, HighlightType type)
        {
            if (_highlights.TryGetValue(coord, out var oldType) && oldType != type)
                RemoveFromTypeIndex(coord, oldType);

            _highlights[coord] = type;
            AddToTypeIndex(coord, type);
            UpdateHighlightVisual(coord, type);
        }

        public void SetHighlights(IEnumerable<HexCoordinates> coords, HighlightType type)
        {
            foreach (var coord in coords)
                SetHighlight(coord, type);
        }

        public void ClearHighlight(HexCoordinates coord)
        {
            if (_highlights.TryGetValue(coord, out var type))
            {
                RemoveFromTypeIndex(coord, type);
                _highlights.Remove(coord);
            }
            RemoveHighlightVisual(coord);
        }

        public void ClearHighlightType(HighlightType type)
        {
            if (!_typeIndex.TryGetValue(type, out var set) || set.Count == 0)
                return;

            _clearBuffer.Clear();
            _clearBuffer.AddRange(set);
            foreach (var coord in _clearBuffer)
                ClearHighlight(coord);
        }

        public void ClearAll()
        {
            foreach (var kvp in _highlightObjects)
            {
                if (kvp.Value != null)
                    ReturnToPool(kvp.Value);
            }
            _highlightObjects.Clear();
            _highlights.Clear();
            foreach (var set in _typeIndex.Values)
                set.Clear();
        }

        public Color GetHighlightColor(HighlightType type)
        {
            switch (type)
            {
                case HighlightType.MoveRange: return _moveRangeColor;
                case HighlightType.AttackRange: return _attackRangeColor;
                case HighlightType.SkillPreview: return _skillPreviewColor;
                case HighlightType.SkillEffect: return _skillEffectColor;
                case HighlightType.Path: return _pathColor;
                case HighlightType.Selected: return _selectedColor;
                case HighlightType.Danger: return _dangerColor;
                default: return Color.clear;
            }
        }

        private void UpdateHighlightVisual(HexCoordinates coord, HighlightType type)
        {
            if (!_highlightObjects.TryGetValue(coord, out var obj) || obj == null)
            {
                obj = GetFromPool();
                if (obj == null) return;
                _highlightObjects[coord] = obj;
            }

            obj.transform.position = coord.ToWorldPosition();
            obj.SetActive(type != HighlightType.None);

            var renderer = obj.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.GetPropertyBlock(_mpb);
                _mpb.SetColor(s_ColorId, GetHighlightColor(type));
                renderer.SetPropertyBlock(_mpb);
            }
        }

        private void RemoveHighlightVisual(HexCoordinates coord)
        {
            if (_highlightObjects.TryGetValue(coord, out var obj))
            {
                if (obj != null) ReturnToPool(obj);
                _highlightObjects.Remove(coord);
            }
        }

        private GameObject GetFromPool()
        {
            while (_pool.Count > 0)
            {
                var obj = _pool.Dequeue();
                if (obj != null)
                {
                    obj.SetActive(true);
                    return obj;
                }
            }
            return _highlightPrefab != null ? Instantiate(_highlightPrefab, transform) : null;
        }

        private void ReturnToPool(GameObject obj)
        {
            obj.SetActive(false);
            _pool.Enqueue(obj);
        }

        private void AddToTypeIndex(HexCoordinates coord, HighlightType type)
        {
            if (!_typeIndex.TryGetValue(type, out var set))
            {
                set = new HashSet<HexCoordinates>();
                _typeIndex[type] = set;
            }
            set.Add(coord);
        }

        private void RemoveFromTypeIndex(HexCoordinates coord, HighlightType type)
        {
            if (_typeIndex.TryGetValue(type, out var set))
                set.Remove(coord);
        }

        private void OnDestroy()
        {
            ClearAll();
            while (_pool.Count > 0)
            {
                var obj = _pool.Dequeue();
                if (obj != null) Destroy(obj);
            }
        }
    }
}
