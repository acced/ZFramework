using System;
using System.Collections.Generic;
using TEngine;

namespace GameLogic
{
    /// <summary>
    /// 存档模块。每个存档槽对应 persistentDataPath/saves/slot_{id}.es3 文件，
    /// 槽位之间完全独立，删除/覆盖只操作单个文件。
    /// </summary>
    public class SaveModule : Singleton<SaveModule>
    {
        private const string SaveDir = "saves";
        private const string DataKey  = "gameData";

        /// <summary>自动存档专用槽位 ID（slot 0）</summary>
        public const int AUTO_SAVE_SLOT_ID = 0;

        private string GetFilePath(int slotId) => $"{SaveDir}/slot_{slotId}.es3";

        /// <summary>将数据保存到指定存档槽。</summary>
        public void SaveSlot(int slotId, GameSaveData data)
        {
            try
            {
                ES3.Save(DataKey, data, GetFilePath(slotId));
                Log.Info($"[SaveModule] 存档成功 -> 槽位 {slotId}");
            }
            catch (Exception e)
            {
                Log.Error($"[SaveModule] 存档失败 槽位 {slotId}: {e.Message}");
            }
        }

        /// <summary>从指定存档槽读取数据，槽位不存在时返回 null。</summary>
        public GameSaveData LoadSlot(int slotId)
        {
            try
            {
                if (!SlotExists(slotId))
                {
                    Log.Warning($"[SaveModule] 存档槽 {slotId} 不存在");
                    return null;
                }
                var data = ES3.Load<GameSaveData>(DataKey, GetFilePath(slotId));
                Log.Info($"[SaveModule] 读档成功 -> 槽位 {slotId} | 玩家:{data.playerName} 等级:{data.level} 时间:{data.saveTime}");
                return data;
            }
            catch (Exception e)
            {
                Log.Error($"[SaveModule] 读档失败 槽位 {slotId}: {e.Message}");
                return null;
            }
        }

        /// <summary>删除指定存档槽文件。</summary>
        public void DeleteSlot(int slotId)
        {
            try
            {
                if (!SlotExists(slotId))
                {
                    Log.Warning($"[SaveModule] 存档槽 {slotId} 不存在，无需删除");
                    return;
                }
                ES3.DeleteFile(GetFilePath(slotId));
                Log.Info($"[SaveModule] 删除存档槽 {slotId} 成功");
            }
            catch (Exception e)
            {
                Log.Error($"[SaveModule] 删除存档槽 {slotId} 失败: {e.Message}");
            }
        }

        /// <summary>检查指定存档槽文件是否存在。</summary>
        public bool SlotExists(int slotId)
        {
            return ES3.FileExists(GetFilePath(slotId));
        }

        /// <summary>获取所有已存在的存档槽 ID 列表。</summary>
        public List<int> GetAllSlotIds(int maxSlot = 10)
        {
            var result = new List<int>();
            for (int i = 1; i <= maxSlot; i++)
            {
                if (SlotExists(i))
                    result.Add(i);
            }
            return result;
        }
    }
}
