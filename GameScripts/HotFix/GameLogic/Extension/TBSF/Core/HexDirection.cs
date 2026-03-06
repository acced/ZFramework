using System;

namespace TBSF.Core
{
    /// <summary>
    /// 六边形方向 (flat-top 布局, 6个方向)
    /// </summary>
    public enum HexDirection
    {
        E  = 0,
        NE = 1,
        NW = 2,
        W  = 3,
        SW = 4,
        SE = 5
    }

    public static class HexDirectionExtensions
    {
        public static HexDirection Opposite(this HexDirection dir)
        {
            return (HexDirection)(((int)dir + 3) % 6);
        }

        public static HexDirection Next(this HexDirection dir)
        {
            return (HexDirection)(((int)dir + 1) % 6);
        }

        public static HexDirection Previous(this HexDirection dir)
        {
            return (HexDirection)(((int)dir + 5) % 6);
        }

        public static HexDirection RotateClockwise(this HexDirection dir, int steps)
        {
            return (HexDirection)(((int)dir + steps % 6 + 6) % 6);
        }
    }
}
