using TBSF.Core;
using TBSF.Grid;
using UnityEngine;

namespace TBSF.Visual
{
    /// <summary>
    /// 六边形格子视图 - 单个格子的渲染组件
    /// </summary>
    public class HexCellView : MonoBehaviour
    {
        [SerializeField] private MeshFilter _meshFilter;
        [SerializeField] private MeshRenderer _meshRenderer;
        [SerializeField] private MeshCollider _meshCollider;

        public HexCoordinates Coordinates { get; private set; }
        public HexCell CellData { get; private set; }

        private static Mesh _sharedHexMesh;

        public void Initialize(HexCell cell)
        {
            CellData = cell;
            Coordinates = cell.Coordinates;

            transform.position = cell.Coordinates.ToWorldPosition(cell.Height);

            if (_meshFilter == null) _meshFilter = GetComponent<MeshFilter>();
            if (_meshRenderer == null) _meshRenderer = GetComponent<MeshRenderer>();

            if (_meshFilter != null)
            {
                _meshFilter.sharedMesh = GetHexMesh();
                if (_meshCollider != null)
                    _meshCollider.sharedMesh = _meshFilter.sharedMesh;
            }
        }

        public void UpdateHeight(float height)
        {
            if (CellData != null)
            {
                CellData.Height = height;
                transform.position = Coordinates.ToWorldPosition(height);
            }
        }

        private static Mesh GetHexMesh()
        {
            if (_sharedHexMesh != null) return _sharedHexMesh;

            _sharedHexMesh = new Mesh { name = "HexCell" };

            var corners = HexMetrics.GetScaledCorners();
            var vertices = new Vector3[7];
            vertices[0] = Vector3.zero;
            for (int i = 0; i < 6; i++)
                vertices[i + 1] = corners[i];

            var triangles = new int[18];
            for (int i = 0; i < 6; i++)
            {
                triangles[i * 3] = 0;
                triangles[i * 3 + 1] = i + 1;
                triangles[i * 3 + 2] = (i < 5) ? i + 2 : 1;
            }

            var uvs = new Vector2[7];
            uvs[0] = new Vector2(0.5f, 0.5f);
            for (int i = 0; i < 6; i++)
            {
                float angle = i * 60f * Mathf.Deg2Rad;
                uvs[i + 1] = new Vector2(0.5f + Mathf.Cos(angle) * 0.5f, 0.5f + Mathf.Sin(angle) * 0.5f);
            }

            _sharedHexMesh.vertices = vertices;
            _sharedHexMesh.triangles = triangles;
            _sharedHexMesh.uv = uvs;
            _sharedHexMesh.RecalculateNormals();
            _sharedHexMesh.RecalculateBounds();

            return _sharedHexMesh;
        }
    }
}
