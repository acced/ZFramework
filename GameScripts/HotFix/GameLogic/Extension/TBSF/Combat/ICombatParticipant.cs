using TBSF.Core;

namespace TBSF.Combat
{
    /// <summary>
    /// 战斗参与者接口
    /// </summary>
    public interface ICombatParticipant
    {
        int UnitId { get; }
        int TeamId { get; }
        bool IsAlive { get; }
        HexCoordinates Position { get; }
        int CurrentHealth { get; }
        int MaxHealth { get; }
    }
}
