using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace LLL.Systems
{
    /// <summary>
    /// 召唤物用量统计：算“某个召唤物/哨兵当前有几只”，以及对应的栏位上限。
    ///
    /// 两种查询方式：
    ///   · 按弹幕类型（CountByProjectile）—— 物品说明里用，知道这件武器召唤的是什么
    ///   · 按 buff（CountByBuff）—— buff 描述里用，只知道是哪个 buff
    ///
    /// ⚠️ 注意：这里**不能**用 Projectile.buffType 去反查 buff（那个成员在游戏内构建里不存在）。
    /// 所以按 buff 查询时，先用“物品样本表”建一张 buff → 召唤物弹幕 的映射表（只建一次，缓存起来）。
    ///
    /// 只统计**属于该玩家**的召唤物/哨兵，所以多人游戏里也不会算到别人的。
    /// </summary>
    public static class MinionUsage
    {
        /// <summary>一种召唤物来源：弹幕类型 + 它是不是哨兵。</summary>
        private struct MinionSource
        {
            public int ProjectileType;
            public bool Sentry;
        }

        /// <summary>buff → 该 buff 对应的召唤物/哨兵弹幕（懒加载缓存）。</summary>
        private static Dictionary<int, List<MinionSource>> _buffMap;

        // ═══════════════════════════════════════════════════════════════
        // 对外：计数
        // ═══════════════════════════════════════════════════════════════

        /// <summary>统计玩家当前拥有的、指定弹幕类型的召唤物/哨兵数量。</summary>
        public static int CountByProjectile(Player player, int projectileType, bool sentry)
        {
            if (player == null || projectileType <= 0) return 0;

            int count = 0;
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile p = Main.projectile[i];
                if (p == null || !p.active) continue;
                if (p.owner != player.whoAmI) continue;
                if (p.type != projectileType) continue;
                if (sentry ? !p.sentry : !p.minion) continue;

                count++;
            }
            return count;
        }

        /// <summary>
        /// 统计玩家当前拥有的、指定 buff 的召唤物/哨兵数量。
        /// 顺带通过 sentry 返回它是不是哨兵（决定用哨兵栏还是召唤栏做分母）。
        /// </summary>
        public static int CountByBuff(Player player, int buffType, out bool sentry)
        {
            sentry = false;
            if (player == null || buffType <= 0) return 0;

            EnsureBuffMap();
            if (!_buffMap.TryGetValue(buffType, out List<MinionSource> sources)) return 0;

            int count = 0;
            foreach (MinionSource source in sources)
            {
                int n = CountByProjectile(player, source.ProjectileType, source.Sentry);
                if (n <= 0) continue;

                if (source.Sentry) sentry = true;
                count += n;
            }
            return count;
        }

        /// <summary>栏位上限：哨兵用哨兵栏，召唤物用召唤栏。</summary>
        public static int GetSlotCapacity(Player player, bool sentry)
        {
            return sentry ? player.maxTurrets : player.maxMinions;
        }

        /// <summary>
        /// 已经被占用的栏位数。
        /// 召唤物按 Projectile.minionSlots 累加（有些召唤物不止占 1 格，多节召唤物的“节”占 0 格，天然不会重复算）；
        /// 哨兵每只占 1 格。
        /// </summary>
        public static float GetUsedSlots(Player player, bool sentry)
        {
            if (player == null) return 0f;

            float used = 0f;
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile p = Main.projectile[i];
                if (p == null || !p.active) continue;
                if (p.owner != player.whoAmI) continue;

                if (sentry)
                {
                    if (p.sentry) used += 1f;
                }
                else if (p.minion)
                {
                    used += p.minionSlots;
                }
            }
            return used;
        }

        /// <summary>
        /// 还剩几个空栏位（向下取整；可能出现半格情况，那半格不算“一个空位”）。
        /// 这是全局数字：所有召唤物共用同一批栏位，所以两把法杖看到的剩余空位是同一个。
        /// </summary>
        public static int GetFreeSlots(Player player, bool sentry)
        {
            float remaining = GetSlotCapacity(player, sentry) - GetUsedSlots(player, sentry);
            return remaining > 0f ? (int)System.Math.Floor(remaining) : 0;
        }

        /// <summary>
        /// 这件武器是不是召唤法杖 / 哨兵法杖？
        /// 判据：哨兵法杖看 item.sentry；召唤法杖看它的 shoot 是不是“可献祭的召唤物弹幕”。
        /// 鞭子、普通武器都不会命中。
        /// </summary>
        public static bool IsSummonStaff(Item item, out bool sentry)
        {
            sentry = false;
            if (item == null || item.IsAir || item.shoot <= 0) return false;

            if (item.sentry)
            {
                sentry = true;
                return true;
            }

            if (item.buffType <= 0) return false;
            return IsMinionProjectile(item.shoot);
        }

        // ═══════════════════════════════════════════════════════════════
        // 内部
        // ═══════════════════════════════════════════════════════════════

        private static bool IsMinionProjectile(int projectileType)
        {
            if (projectileType <= 0) return false;
            if (projectileType >= ProjectileID.Sets.MinionSacrificable.Length) return false;
            return ProjectileID.Sets.MinionSacrificable[projectileType];
        }

        /// <summary>建立 buff → 召唤物 的映射（用所有物品的样本表，模组物品也包含在内）。</summary>
        private static void EnsureBuffMap()
        {
            if (_buffMap != null) return;

            _buffMap = new Dictionary<int, List<MinionSource>>();

            foreach (Item sample in ContentSamples.ItemsByType.Values)
            {
                if (sample == null || sample.IsAir) continue;
                if (sample.buffType <= 0 || sample.shoot <= 0) continue;

                bool sentry = sample.sentry;
                if (!sentry && !IsMinionProjectile(sample.shoot)) continue;

                if (!_buffMap.TryGetValue(sample.buffType, out List<MinionSource> list))
                {
                    list = new List<MinionSource>();
                    _buffMap[sample.buffType] = list;
                }

                var entry = new MinionSource { ProjectileType = sample.shoot, Sentry = sentry };
                if (!list.Contains(entry))
                    list.Add(entry);
            }
        }

        /// <summary>卸载/重载时清缓存（内容变了，映射表要重建）。</summary>
        public static void ClearCache()
        {
            _buffMap = null;
        }
    }

    /// <summary>负责在模组卸载时清掉 MinionUsage 的缓存。</summary>
    public class MinionUsageCacheSystem : ModSystem
    {
        public override void Unload()
        {
            MinionUsage.ClearCache();
        }
    }
}
