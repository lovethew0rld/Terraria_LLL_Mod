using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace LLL.Systems
{
    /// <summary>
    /// 修饰语（前缀）信息中心：告诉我们在**当前加载环境下**到底有哪些修饰语，并提供随机/合法性判定。
    ///
    /// 【能读到什么】
    /// tModLoader 的前缀注册表在**模组加载阶段**就已建好，不需要进世界：
    ///     PrefixLoader.PrefixCount       —— 原版 + 所有模组注册的修饰语总数
    ///     PrefixLoader.GetPrefix(id)     —— 模组修饰语实例（原版返回 null）
    ///     Lang.prefix[id]                —— 原版修饰语的当前语言名字
    /// 进世界时会打一行日志告诉你读到了多少个，方便一眼确认模组前缀有没有被读到。
    ///
    /// 【随机规则】
    /// 从该物品所有**合法**修饰语里等概率取一个；另有 25% 概率返回 0（轮空，不加修饰语）。
    /// 合法判定分两条路：原版用 Item.CanApplyPrefix，模组修饰语用 ModPrefix.CanRoll。
    /// </summary>
    public class PrefixRegistry : ModSystem
    {
        /// <summary>“随机”模式下轮空（不加任何修饰语）的概率，单位 %。</summary>
        public const int NoPrefixChancePercent = 25;

        /// <summary>物品类型 → 该类型的合法修饰语池（缓存，避免每次掉落都遍历上百个修饰语）。</summary>
        private static readonly Dictionary<int, int[]> _poolCache = new Dictionary<int, int[]>();

        /// <summary>一进世界就打印读到的修饰语数量。</summary>
        public override void OnWorldLoad()
        {
            Mod.Logger.Info($"[额外战利品] 已读取到 {PrefixLoader.PrefixCount - 1} 个修饰语（原版 + 所有模组），随机时会从这件物品的合法修饰语里取。");
        }

        /// <summary>模组卸载时清缓存，避免重载后残留旧 ID。</summary>
        public override void Unload()
        {
            _poolCache.Clear();
        }

        /// <summary>
        /// 这个修饰语能不能用在这件物品上：
        /// 原版修饰语走 Item.CanApplyPrefix，其它模组添加的走 ModPrefix.CanRoll。
        /// </summary>
        public static bool IsValid(Item item, int prefix)
        {
            if (item == null || prefix <= 0 || prefix >= PrefixLoader.PrefixCount)
                return false;

            ModPrefix modPrefix = PrefixLoader.GetPrefix(prefix);
            if (modPrefix != null)
                return modPrefix.CanRoll(item);

            return item.CanApplyPrefix(prefix);
        }

        /// <summary>
        /// 随机取一个修饰语；有 <see cref="NoPrefixChancePercent"/>% 概率返回 0 表示轮空（不加修饰语）。
        /// </summary>
        public static int Roll(Item item)
        {
            if (Main.rand.Next(100) < NoPrefixChancePercent)
                return 0;

            int[] pool = GetPool(item);
            if (pool.Length == 0)
                return 0;

            return pool[Main.rand.Next(pool.Length)];
        }

        private static int[] GetPool(Item item)
        {
            if (_poolCache.TryGetValue(item.type, out int[] cached))
                return cached;

            var pool = new List<int>();
            for (int prefix = 1; prefix < PrefixLoader.PrefixCount; prefix++)
            {
                if (IsValid(item, prefix))
                    pool.Add(prefix);
            }

            int[] result = pool.ToArray();
            _poolCache[item.type] = result;
            return result;
        }
    }
}
