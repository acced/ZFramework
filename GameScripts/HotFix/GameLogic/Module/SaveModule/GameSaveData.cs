using System;

namespace GameLogic
{
    [Serializable]
    public class GameSaveData
    {
        public string playerName;
        /// <summary>等级</summary>
        public int level;
        /// <summary>当前所在场景/地图名称</summary>
        public string locationName;
        /// <summary>金币数量</summary>
        public long gold;
        /// <summary>累计游玩时长（秒）</summary>
        public float playTimeSeconds;
        /// <summary>存档时间（yyyy-MM-dd HH:mm:ss）</summary>
        public string saveTime;
        /// <summary>是否为自动存档</summary>
        public bool isAutoSave;

        /// <summary>格式化游玩时长为 HH:mm:ss 字符串</summary>
        public string FormatPlayTime()
        {
            var ts = TimeSpan.FromSeconds(playTimeSeconds);
            return ts.TotalHours >= 1
                ? $"{(int)ts.TotalHours:D2}:{ts.Minutes:D2}:{ts.Seconds:D2}"
                : $"{ts.Minutes:D2}:{ts.Seconds:D2}";
        }
    }
}
