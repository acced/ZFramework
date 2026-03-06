using System.Collections.Generic;
using TBSF.Core;
using TBSF.Grid;
using UnityEngine;

namespace TBSF.Visual
{
    /// <summary>
    /// 六边形网格视图 - 管理所有格子视图的创建和销毁
    /// </summary>
    public class HexGridView : MonoBehaviour
    {
        [SerializeField] private GameObject _cellPrefab;
        [SerializeField] private Transform _cellContainer;

        private readonly Dictionary<HexCoordinates, HexCellView> _cellViews =
            new Dictionary<HexCoordinates, HexCellView>();

        private HexGrid _grid;

        public IReadOnlyDictionary<HexCoordinates, HexCellView> CellViews => _cellViews;

        public void Initialize(HexGrid grid)
        {
            _grid = grid;
            ClearViews();
            CreateViews();
        }

        private void CreateViews()
        {
            if (_grid == null || _cellPrefab == null) return;

            var parent = _cellContainer != null ? _cellContainer : transform;

            foreach (var cell in _grid.AllCells)
            {
                var obj = Instantiate(_cellPrefab, parent);
                var view = obj.GetComponent<HexCellView>();
                if (view == null) view = obj.AddComponent<HexCellView>();

                view.Initialize(cell);
                obj.name = $"Cell_{cell.Coordinates}";
                _cellViews[cell.Coordinates] = view;
            }
        }

        public HexCellView GetCellView(HexCoordinates coord)
        {
            _cellViews.TryGetValue(coord, out var view);
            return view;
        }

        public void ClearViews()
        {
            foreach (var kvp in _cellViews)
            {
                if (kvp.Value != null)
                    Destroy(kvp.Value.gameObject);
            }
            _cellViews.Clear();
        }

        /// <summary>
        /// 从世界坐标获取格子视图
        /// </summary>
        public HexCellView GetCellViewFromWorldPosition(Vector3 worldPos)
        {
            var coord = HexCoordinates.FromWorldPosition(worldPos);
            return GetCellView(coord);
        }

        private void OnDestroy()
        {
            ClearViews();
        }
    }
}
