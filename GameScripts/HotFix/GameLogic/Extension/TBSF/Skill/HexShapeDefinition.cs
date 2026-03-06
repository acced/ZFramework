using System;

namespace TBSF.Skill
{
    /// <summary>
    /// 六边形技能形状类型
    /// </summary>
    public enum HexShapeType
    {
        SingleCell,
        Area,
        Ring,
        Line,
        Sector,
        Custom
    }

    /// <summary>
    /// 六边形技能形状定义 - 描述技能的范围形状参数
    /// </summary>
    [Serializable]
    public class HexShapeDefinition
    {
        public HexShapeType ShapeType { get; set; }
        public int Radius { get; set; }
        public int LineLength { get; set; }
        public int SectorSpread { get; set; }
        public int CastRange { get; set; }
        public bool IncludeSelf { get; set; }
        public bool CheckHeight { get; set; }
        public float MaxHeightDiff { get; set; } = 2f;

        public static HexShapeDefinition SingleTarget(int castRange)
        {
            return new HexShapeDefinition
            {
                ShapeType = HexShapeType.SingleCell,
                CastRange = castRange
            };
        }

        public static HexShapeDefinition AreaOfEffect(int radius, int castRange, bool includeSelf = false)
        {
            return new HexShapeDefinition
            {
                ShapeType = HexShapeType.Area,
                Radius = radius,
                CastRange = castRange,
                IncludeSelf = includeSelf
            };
        }

        public static HexShapeDefinition LineAttack(int lineLength, int castRange = 0)
        {
            return new HexShapeDefinition
            {
                ShapeType = HexShapeType.Line,
                LineLength = lineLength,
                CastRange = castRange
            };
        }

        public static HexShapeDefinition RingEffect(int radius, int castRange)
        {
            return new HexShapeDefinition
            {
                ShapeType = HexShapeType.Ring,
                Radius = radius,
                CastRange = castRange
            };
        }
    }
}
