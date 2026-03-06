using UnityEngine;

namespace TBSF.Core
{
    /// <summary>
    /// 六边形度量常量 (flat-top 布局)
    /// 外径 = OuterRadius, 内径 = InnerRadius
    /// flat-top: 宽 = 2 * OuterRadius, 高 = sqrt(3) * OuterRadius
    /// 修改 OuterRadius/HeightStep 后 Corners 会自动重算
    /// </summary>
    public static class HexMetrics
    {
        private const float Sqrt3Over2 = 0.866025404f;

        private static float _outerRadius = 1f;
        private static float _heightStep = 0.5f;
        private static bool _cornersDirty = true;
        private static Vector3[] _corners;
        private static Vector3[] _scaledCorners;

        public static float OuterRadius
        {
            get => _outerRadius;
            set
            {
                if (Mathf.Approximately(_outerRadius, value)) return;
                _outerRadius = value;
                _cornersDirty = true;
            }
        }

        public static float HeightStep
        {
            get => _heightStep;
            set => _heightStep = value;
        }

        public static float InnerRadius => _outerRadius * Sqrt3Over2;
        public static float Width => _outerRadius * 2f;
        public static float Height => InnerRadius * 2f;
        public static float HorizontalSpacing => Width * 0.75f;
        public static float VerticalSpacing => Height;

        /// <summary>
        /// 六边形6个顶点偏移 (flat-top, 从右侧顶点开始顺时针, 第7个 = 第0个闭合)
        /// 修改 OuterRadius 后自动重算
        /// </summary>
        public static Vector3[] Corners
        {
            get
            {
                if (_cornersDirty || _corners == null)
                    RecalculateCorners();
                return _corners;
            }
        }

        /// <summary>
        /// axial 坐标偏移 (flat-top) 按 HexDirection 顺序: E, NE, NW, W, SW, SE
        /// </summary>
        public static readonly int[,] AxialDirections =
        {
            { +1,  0 }, // E
            { +1, -1 }, // NE
            {  0, -1 }, // NW
            { -1,  0 }, // W
            { -1, +1 }, // SW
            {  0, +1 }  // SE
        };

        /// <summary>
        /// 获取缩放后的顶点（缓存版本，修改 OuterRadius 后自动更新）
        /// </summary>
        public static Vector3[] GetScaledCorners()
        {
            if (_cornersDirty || _scaledCorners == null)
                RecalculateCorners();
            return _scaledCorners;
        }

        private static void RecalculateCorners()
        {
            float r = _outerRadius;
            _corners = new[]
            {
                new Vector3(1f, 0f, 0f) * r,
                new Vector3(0.5f, 0f, Sqrt3Over2) * r,
                new Vector3(-0.5f, 0f, Sqrt3Over2) * r,
                new Vector3(-1f, 0f, 0f) * r,
                new Vector3(-0.5f, 0f, -Sqrt3Over2) * r,
                new Vector3(0.5f, 0f, -Sqrt3Over2) * r,
                new Vector3(1f, 0f, 0f) * r
            };

            _scaledCorners = new Vector3[7];
            for (int i = 0; i < 7; i++)
            {
                float angle = 60f * i * Mathf.Deg2Rad;
                _scaledCorners[i] = new Vector3(
                    Mathf.Cos(angle) * r,
                    0f,
                    Mathf.Sin(angle) * r
                );
            }

            _cornersDirty = false;
        }
    }
}
