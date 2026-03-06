using TBSF.Core;
using UnityEngine;

namespace TBSF.Grid
{
    /// <summary>
    /// 地图生成器 - 支持多种生成策略
    /// </summary>
    public static class HexGridGenerator
    {
        /// <summary>
        /// 生成矩形网格 (offset 布局)
        /// </summary>
        public static HexGrid GenerateRectangle(int width, int height)
        {
            var grid = new HexGrid();
            for (int r = 0; r < height; r++)
            {
                int rOffset = r / 2;
                for (int q = -rOffset; q < width - rOffset; q++)
                {
                    var coord = new HexCoordinates(q, r);
                    grid.AddCell(new HexCell(coord));
                }
            }
            return grid;
        }

        /// <summary>
        /// 生成六边形形状的网格 (半径 = mapRadius)
        /// </summary>
        public static HexGrid GenerateHexagonal(int mapRadius)
        {
            var grid = new HexGrid();
            var coords = HexCoordinates.Zero.GetArea(mapRadius);
            foreach (var c in coords)
            {
                grid.AddCell(new HexCell(c));
            }
            return grid;
        }

        /// <summary>
        /// 生成带随机高度和地形的网格
        /// </summary>
        public static HexGrid GenerateWithTerrain(int mapRadius, int seed = 0)
        {
            var grid = new HexGrid();
            var coords = HexCoordinates.Zero.GetArea(mapRadius);

            var rng = seed == 0 ? new System.Random() : new System.Random(seed);
            float scale = 0.15f;
            float offsetX = (float)rng.NextDouble() * 1000f;
            float offsetZ = (float)rng.NextDouble() * 1000f;

            foreach (var c in coords)
            {
                var worldPos = c.ToWorldPosition();
                float noise = Mathf.PerlinNoise(
                    worldPos.x * scale + offsetX,
                    worldPos.z * scale + offsetZ
                );

                float cellHeight = Mathf.Round(noise * 6f) * 0.5f;
                HexTerrainType terrain = SampleTerrain(noise);

                grid.AddCell(new HexCell(c, cellHeight, terrain));
            }

            return grid;
        }

        private static HexTerrainType SampleTerrain(float noise)
        {
            if (noise < 0.2f) return HexTerrainType.Water;
            if (noise < 0.4f) return HexTerrainType.Sand;
            if (noise < 0.6f) return HexTerrainType.Plain;
            if (noise < 0.75f) return HexTerrainType.Forest;
            if (noise < 0.9f) return HexTerrainType.Swamp;
            return HexTerrainType.Mountain;
        }
    }
}
