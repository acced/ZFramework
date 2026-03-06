using UnityEngine;

namespace GameLogic
{
    /// <summary>
    /// 示例数据类
    /// </summary>
    public class DemoData
    {
        /// <summary>
        /// 唯一ID
        /// </summary>
        public int Id;
        
        /// <summary>
        /// 名称
        /// </summary>
        public string Name;
        
        /// <summary>
        /// 描述
        /// </summary>
        public string Desc;
        
        /// <summary>
        /// 图标索引
        /// </summary>
        public int IconIndex;
        
        /// <summary>
        /// 创建时间戳
        /// </summary>
        public float CreateTime;
        
        private static int _idCounter = 0;
        
        /// <summary>
        /// 创建新的数据
        /// </summary>
        public static DemoData Create(string name = null)
        {
            _idCounter++;
            return new DemoData
            {
                Id = _idCounter,
                Name = name ?? $"Item {_idCounter}",
                Desc = $"这是第 {_idCounter} 条数据",
                IconIndex = Random.Range(0, 50),
                CreateTime = Time.time
            };
        }
        
        /// <summary>
        /// 批量创建数据
        /// </summary>
        public static System.Collections.Generic.List<DemoData> CreateBatch(int count)
        {
            var list = new System.Collections.Generic.List<DemoData>();
            for (int i = 0; i < count; i++)
            {
                list.Add(Create());
            }
            return list;
        }
        
        /// <summary>
        /// 重置ID计数器
        /// </summary>
        public static void ResetIdCounter()
        {
            _idCounter = 0;
        }
    }
}
