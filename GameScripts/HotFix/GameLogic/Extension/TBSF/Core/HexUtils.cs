using System.Collections.Generic;
using UnityEngine;

namespace TBSF.Core
{
    public static class HexUtils
    {
        /// <summary>
        /// 将 cube 坐标旋转60度 (顺时针)
        /// </summary>
        public static HexCoordinates RotateCW(HexCoordinates coord)
        {
            return new HexCoordinates(-coord.S, -coord.Q);
        }

        /// <summary>
        /// 将 cube 坐标旋转60度 (逆时针)
        /// </summary>
        public static HexCoordinates RotateCCW(HexCoordinates coord)
        {
            return new HexCoordinates(-coord.R, -coord.S);
        }

        /// <summary>
        /// 围绕 center 旋转 steps 个60度 (正值=顺时针)
        /// </summary>
        public static HexCoordinates RotateAround(HexCoordinates coord, HexCoordinates center, int steps)
        {
            var offset = coord - center;
            steps = ((steps % 6) + 6) % 6;
            for (int i = 0; i < steps; i++)
                offset = RotateCW(offset);
            return center + offset;
        }

        /// <summary>
        /// 沿 HexDirection 方向偏移 distance 格
        /// </summary>
        public static HexCoordinates Step(HexCoordinates origin, HexDirection dir, int distance)
        {
            int d = (int)dir;
            return new HexCoordinates(
                origin.Q + HexMetrics.AxialDirections[d, 0] * distance,
                origin.R + HexMetrics.AxialDirections[d, 1] * distance
            );
        }

        /// <summary>
        /// 获取扇形范围内的格子 (从 origin 出发, facing 方向, 张角 sectorDirs 个方向, 半径 radius)
        /// </summary>
        public static List<HexCoordinates> GetSector(
            HexCoordinates origin, HexDirection facing, int halfSpread, int radius)
        {
            var area = origin.GetArea(radius);
            var result = new List<HexCoordinates>();

            foreach (var coord in area)
            {
                if (coord == origin) continue;
                var dir = HexCoordinates.GetDirection(origin, coord);
                int diff = ((int)dir - (int)facing + 6) % 6;
                if (diff > 3) diff = 6 - diff;
                if (diff <= halfSpread)
                    result.Add(coord);
            }
            return result;
        }

        /// <summary>
        /// 计算两个格子之间的高度差
        /// </summary>
        public static float HeightDifference(float heightA, float heightB)
        {
            return Mathf.Abs(heightA - heightB);
        }

        /// <summary>
        /// 将世界方向向量映射到最近的 HexDirection
        /// </summary>
        public static HexDirection WorldDirectionToHex(Vector3 worldDir)
        {
            worldDir.y = 0;
            if (worldDir.sqrMagnitude < 0.001f)
                return HexDirection.E;

            float angle = Mathf.Atan2(worldDir.z, worldDir.x) * Mathf.Rad2Deg;
            if (angle < 0) angle += 360f;

            // flat-top: E=0°, NE=60°, NW=120°, W=180°, SW=240°, SE=300°
            int index = Mathf.RoundToInt(angle / 60f) % 6;
            return (HexDirection)index;
        }

        /// <summary>
        /// 将 HexDirection 转换为世界方向向量
        /// </summary>
        public static Vector3 HexDirectionToWorld(HexDirection dir)
        {
            float angle = (int)dir * 60f * Mathf.Deg2Rad;
            return new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));
        }
    }
}
