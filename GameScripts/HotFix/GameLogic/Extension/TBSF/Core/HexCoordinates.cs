using System;
using System.Collections.Generic;
using UnityEngine;

namespace TBSF.Core
{
    /// <summary>
    /// 六边形 axial 坐标 (q, r), 内部支持 cube 坐标 (q, r, s) 其中 s = -q - r
    /// flat-top 布局
    /// </summary>
    [Serializable]
    public struct HexCoordinates : IEquatable<HexCoordinates>
    {
        [SerializeField] private int q;
        [SerializeField] private int r;

        public int Q => q;
        public int R => r;
        public int S => -q - r;

        public HexCoordinates(int q, int r)
        {
            this.q = q;
            this.r = r;
        }

        public static HexCoordinates Zero => new HexCoordinates(0, 0);

        // ============ 邻居 ============

        public HexCoordinates GetNeighbor(HexDirection direction)
        {
            int d = (int)direction;
            return new HexCoordinates(
                q + HexMetrics.AxialDirections[d, 0],
                r + HexMetrics.AxialDirections[d, 1]
            );
        }

        public HexCoordinates[] GetAllNeighbors()
        {
            var neighbors = new HexCoordinates[6];
            GetAllNeighborsNonAlloc(neighbors);
            return neighbors;
        }

        /// <summary>
        /// 零分配版本：将6个邻居写入调用方提供的 buffer（长度必须 >= 6）
        /// </summary>
        public void GetAllNeighborsNonAlloc(HexCoordinates[] buffer)
        {
            for (int i = 0; i < 6; i++)
            {
                buffer[i] = new HexCoordinates(
                    q + HexMetrics.AxialDirections[i, 0],
                    r + HexMetrics.AxialDirections[i, 1]
                );
            }
        }

        /// <summary>
        /// 零分配 struct 枚举器，可直接 foreach 使用
        /// </summary>
        public NeighborEnumerator GetNeighbors() => new NeighborEnumerator(this);

        public struct NeighborEnumerator
        {
            private readonly int _q, _r;
            private int _index;

            public NeighborEnumerator(HexCoordinates center)
            {
                _q = center.Q;
                _r = center.R;
                _index = -1;
            }

            public HexCoordinates Current => new HexCoordinates(
                _q + HexMetrics.AxialDirections[_index, 0],
                _r + HexMetrics.AxialDirections[_index, 1]);

            public bool MoveNext() => ++_index < 6;
            public NeighborEnumerator GetEnumerator() => this;
        }

        // ============ 距离 ============

        public static int Distance(HexCoordinates a, HexCoordinates b)
        {
            int dq = a.Q - b.Q;
            int dr = a.R - b.R;
            int ds = a.S - b.S;
            return (Math.Abs(dq) + Math.Abs(dr) + Math.Abs(ds)) / 2;
        }

        public int DistanceTo(HexCoordinates other)
        {
            return Distance(this, other);
        }

        // ============ 范围查询 ============

        /// <summary>
        /// 获取指定半径的环形坐标 (距离恰好 = radius)
        /// </summary>
        public List<HexCoordinates> GetRing(int radius)
        {
            var results = new List<HexCoordinates>();
            if (radius <= 0)
            {
                results.Add(this);
                return results;
            }

            var current = new HexCoordinates(q - radius, r + radius); // SW 方向起点
            for (int dir = 0; dir < 6; dir++)
            {
                for (int step = 0; step < radius; step++)
                {
                    results.Add(current);
                    current = current.GetNeighbor((HexDirection)dir);
                }
            }
            return results;
        }

        /// <summary>
        /// 获取指定半径的实心圆范围 (距离 <= radius)
        /// </summary>
        public List<HexCoordinates> GetArea(int radius)
        {
            var results = new List<HexCoordinates>();
            for (int dq = -radius; dq <= radius; dq++)
            {
                int rMin = Math.Max(-radius, -dq - radius);
                int rMax = Math.Min(radius, -dq + radius);
                for (int dr = rMin; dr <= rMax; dr++)
                {
                    results.Add(new HexCoordinates(q + dq, r + dr));
                }
            }
            return results;
        }

        /// <summary>
        /// 获取从 this 到 target 的直线格子
        /// </summary>
        public List<HexCoordinates> GetLine(HexCoordinates target)
        {
            int dist = Distance(this, target);
            var results = new List<HexCoordinates>(dist + 1);

            if (dist == 0)
            {
                results.Add(this);
                return results;
            }

            for (int i = 0; i <= dist; i++)
            {
                float t = (float)i / dist;
                float lerpQ = Q + (target.Q - Q) * t;
                float lerpR = R + (target.R - R) * t;
                results.Add(CubeRound(lerpQ, lerpR, -lerpQ - lerpR));
            }
            return results;
        }

        // ============ 世界坐标转换 (flat-top) ============

        public Vector3 ToWorldPosition(float height = 0f)
        {
            float x = q * HexMetrics.HorizontalSpacing;
            float z = r * HexMetrics.VerticalSpacing + q * 0.5f * HexMetrics.VerticalSpacing;
            float y = height * HexMetrics.HeightStep;
            return new Vector3(x, y, z);
        }

        public static HexCoordinates FromWorldPosition(Vector3 position)
        {
            float fq = position.x / HexMetrics.HorizontalSpacing;
            float fr = (position.z - fq * 0.5f * HexMetrics.VerticalSpacing) / HexMetrics.VerticalSpacing;
            return CubeRound(fq, fr, -fq - fr);
        }

        // ============ 方向 ============

        /// <summary>
        /// 获取从 from 到 to 的大致方向
        /// </summary>
        public static HexDirection GetDirection(HexCoordinates from, HexCoordinates to)
        {
            int dq = to.Q - from.Q;
            int dr = to.R - from.R;
            int ds = to.S - from.S;

            int absQ = Math.Abs(dq);
            int absR = Math.Abs(dr);
            int absS = Math.Abs(ds);

            if (absQ >= absR && absQ >= absS)
                return dq > 0 ? HexDirection.E : HexDirection.W;
            if (absR >= absS)
                return dr > 0 ? HexDirection.SE : HexDirection.NE; // Fixed for flat-top
            return ds > 0 ? HexDirection.SW : HexDirection.NW;
        }

        // ============ 内部工具 ============

        private static HexCoordinates CubeRound(float fq, float fr, float fs)
        {
            int rq = Mathf.RoundToInt(fq);
            int rr = Mathf.RoundToInt(fr);
            int rs = Mathf.RoundToInt(fs);

            float dq = Math.Abs(rq - fq);
            float dr = Math.Abs(rr - fr);
            float ds = Math.Abs(rs - fs);

            if (dq > dr && dq > ds)
                rq = -rr - rs;
            else if (dr > ds)
                rr = -rq - rs;

            return new HexCoordinates(rq, rr);
        }

        // ============ 运算符 ============

        public static HexCoordinates operator +(HexCoordinates a, HexCoordinates b)
        {
            return new HexCoordinates(a.q + b.q, a.r + b.r);
        }

        public static HexCoordinates operator -(HexCoordinates a, HexCoordinates b)
        {
            return new HexCoordinates(a.q - b.q, a.r - b.r);
        }

        public static bool operator ==(HexCoordinates a, HexCoordinates b)
        {
            return a.q == b.q && a.r == b.r;
        }

        public static bool operator !=(HexCoordinates a, HexCoordinates b)
        {
            return !(a == b);
        }

        public bool Equals(HexCoordinates other) => q == other.q && r == other.r;
        public override bool Equals(object obj) => obj is HexCoordinates other && Equals(other);
        public override int GetHashCode() => (q * 397) ^ r;
        public override string ToString() => $"({q}, {r})";
    }
}
