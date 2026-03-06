using System.Collections.Generic;
using TBSF.Grid;
using UnityEngine;

namespace TBSF.Visual
{
    /// <summary>
    /// 路径线渲染器 - 在格子中心之间绘制路径线
    /// </summary>
    [RequireComponent(typeof(LineRenderer))]
    public class PathLineRenderer : MonoBehaviour
    {
        [SerializeField] private float _heightOffset = 0.1f;
        [SerializeField] private float _lineWidth = 0.08f;

        private LineRenderer _lineRenderer;

        private void Awake()
        {
            _lineRenderer = GetComponent<LineRenderer>();
            _lineRenderer.startWidth = _lineWidth;
            _lineRenderer.endWidth = _lineWidth;
            _lineRenderer.positionCount = 0;
        }

        public void ShowPath(List<HexCell> path)
        {
            if (path == null || path.Count == 0)
            {
                Hide();
                return;
            }

            _lineRenderer.positionCount = path.Count;
            for (int i = 0; i < path.Count; i++)
            {
                var worldPos = path[i].Coordinates.ToWorldPosition(path[i].Height);
                worldPos.y += _heightOffset;
                _lineRenderer.SetPosition(i, worldPos);
            }
        }

        public void Hide()
        {
            _lineRenderer.positionCount = 0;
        }
    }
}
