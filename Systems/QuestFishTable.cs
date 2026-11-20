using System.Collections.Generic;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;

namespace LLL.Systems
{
    /// <summary>
    /// 原版 41 只任务鱼的「环境」条件表。
    ///
    /// 【为什么必须自己维护这张表】
    /// 原版的任务鱼判定不是查表 —— 它散落在 Projectile.FishingCheck_RollItemDrop 的
    /// 每个生物群系掉落分支里，一共 43 个这样的判断：
    ///     if (attempt.uncommon && attempt.questFish == 某ID) { 掉落这只鱼 }
    /// 也就是说"这个环境对应哪只鱼"这件事，代码里没有可查询的数据结构。
    /// 想实现"任何时间都能钓到当前环境的任务鱼"，就只能自己带一张表。
    ///
    /// 数据来源：官方 wiki 的 Angler/Quests 表格（41 只鱼 + 高度 + 生物群系），
    /// 并对照程序集里的 43 个分支核对过鱼 ID 与数量。
    ///
    /// 【高度档位】直接用 FishingAttempt.heightLevel（原版自己按鱼漂坐标算好的）：
    ///     0 = 太空 / 高空（worldSurface 的上半部分）
    ///     1 = 地表
    ///     2 = 地下
    ///     3 = 洞穴
    ///     4 = 地狱
    /// ⚠️ 颠倒世界（remix）的高度编码与原版不同，这里按普通世界的口径处理。
    ///
    /// 【生物群系】用 Player.Zone*（原版判定也是看玩家所在地，不是鱼漂）。
    /// </summary>
    public static class QuestFishTable
    {
        /// <summary>一只任务鱼的全部条件。</summary>
        private readonly struct Entry
        {
            public readonly int Type;
            public readonly Biome Biome;
            public readonly int MinHeight;
            public readonly int MaxHeight;
            public readonly bool HardmodeOnly;

            public Entry(int type, Biome biome, int minHeight, int maxHeight, bool hardmodeOnly = false)
            {
                Type = type;
                Biome = biome;
                MinHeight = minHeight;
                MaxHeight = maxHeight;
                HardmodeOnly = hardmodeOnly;
            }
        }

        private enum Biome
        {
            /// <summary>纯净（森林）。</summary>
            Pure,
            Corruption,
            Crimson,
            Jungle,
            Snow,
            Hallow,
            Desert,
            /// <summary>海洋 / 海滩。</summary>
            Ocean,
            /// <summary>发光蘑菇地。</summary>
            Mushroom,
            /// <summary>蜂蜜（液体，不是群系）。</summary>
            Honey,
        }

        // 高度档位（对应 FishingAttempt.heightLevel）
        private const int Sky = 0;
        private const int Surface = 1;
        private const int Underground = 2;
        private const int Cavern = 3;
        private const int Underworld = 4;

        /// <summary>
        /// 41 只原版任务鱼。顺序按 wiki 表格，方便日后对照。
        /// </summary>
        private static readonly Entry[] Entries =
        {
            //         物品 ID                                生物群系         高度下限     高度上限     肉后限定
            new Entry(ItemID.AmanitaFungifin,      Biome.Mushroom,   Sky,         Underworld),
            new Entry(ItemID.Angelfish,            Biome.Pure,       Sky,         Sky),
            new Entry(ItemID.Batfish,              Biome.Pure,       Underground, Underworld),
            new Entry(ItemID.BloodyManowar,        Biome.Crimson,    Sky,         Underworld),
            new Entry(ItemID.Bonefish,             Biome.Pure,       Underground, Underworld),
            new Entry(ItemID.BumblebeeTuna,        Biome.Honey,      Sky,         Underworld),
            new Entry(ItemID.Bunnyfish,            Biome.Pure,       Surface,     Surface),
            new Entry(ItemID.CapnTunabeard,        Biome.Ocean,      Sky,         Surface,   true),
            new Entry(ItemID.Catfish,              Biome.Jungle,     Surface,     Surface),
            new Entry(ItemID.Cloudfish,            Biome.Pure,       Sky,         Sky),
            new Entry(ItemID.Clownfish,            Biome.Ocean,      Sky,         Surface),
            new Entry(ItemID.Cursedfish,           Biome.Corruption, Sky,         Underworld, true),
            new Entry(ItemID.DemonicHellfish,      Biome.Pure,       Cavern,      Underworld),
            new Entry(ItemID.Derpfish,             Biome.Jungle,     Surface,     Surface,   true),
            new Entry(ItemID.Dirtfish,             Biome.Pure,       Surface,     Underground),
            new Entry(ItemID.DynamiteFish,         Biome.Pure,       Surface,     Surface),
            new Entry(ItemID.EaterofPlankton,      Biome.Corruption, Sky,         Underworld),
            new Entry(ItemID.FallenStarfish,       Biome.Pure,       Sky,         Surface),
            new Entry(ItemID.Fishotron,            Biome.Pure,       Cavern,      Underworld),
            new Entry(ItemID.Fishron,              Biome.Snow,       Underground, Underworld, true),
            new Entry(ItemID.GuideVoodooFish,      Biome.Pure,       Cavern,      Underworld),
            new Entry(ItemID.Harpyfish,            Biome.Pure,       Sky,         Surface),
            new Entry(ItemID.Hungerfish,           Biome.Pure,       Cavern,      Underworld, true),
            new Entry(ItemID.Ichorfish,            Biome.Crimson,    Sky,         Underworld, true),
            new Entry(ItemID.InfectedScabbardfish, Biome.Corruption, Sky,         Underworld),
            new Entry(ItemID.Jewelfish,            Biome.Pure,       Underground, Underworld),
            new Entry(ItemID.MirageFish,           Biome.Hallow,     Underground, Cavern,    true),
            new Entry(ItemID.Mudfish,              Biome.Jungle,     Sky,         Underworld),
            new Entry(ItemID.MutantFlinxfin,       Biome.Snow,       Underground, Underworld),
            new Entry(ItemID.Pengfish,             Biome.Snow,       Sky,         Surface),
            new Entry(ItemID.Pixiefish,            Biome.Hallow,     Sky,         Surface,   true),
            new Entry(ItemID.ScarabFish,           Biome.Desert,     Sky,         Underworld),
            new Entry(ItemID.ScorpioFish,          Biome.Desert,     Sky,         Underworld),
            new Entry(ItemID.Slimefish,            Biome.Pure,       Surface,     Surface),
            new Entry(ItemID.Spiderfish,           Biome.Pure,       Underground, Underworld),
            new Entry(ItemID.TheFishofCthulu,      Biome.Pure,       Sky,         Surface),
            new Entry(ItemID.TropicalBarracuda,    Biome.Jungle,     Surface,     Surface),
            new Entry(ItemID.TundraTrout,          Biome.Snow,       Surface,     Surface),
            new Entry(ItemID.UnicornFish,          Biome.Hallow,     Sky,         Underworld, true),
            new Entry(ItemID.Wyverntail,           Biome.Pure,       Sky,         Sky,       true),
            new Entry(ItemID.ZombieFish,           Biome.Pure,       Surface,     Surface),
        };

        /// <summary>任务鱼 ID 集合，用来判断"钓到的这个是不是任务鱼"。</summary>
        private static readonly HashSet<int> QuestFishIds = BuildQuestFishIds();

        private static HashSet<int> BuildQuestFishIds()
        {
            HashSet<int> set = new HashSet<int>();
            foreach (Entry entry in Entries)
                set.Add(entry.Type);

            // 兜底：原版的"当天任务鱼 ID 表"里出现的，也算任务鱼
            if (Main.anglerQuestItemNetIDs != null)
            {
                foreach (int id in Main.anglerQuestItemNetIDs)
                    set.Add(id);
            }

            return set;
        }

        /// <summary>这条物品是不是任务鱼。</summary>
        public static bool IsQuestFish(int itemType) => QuestFishIds.Contains(itemType);

        /// <summary>
        /// 列出"当前环境"能钓到的所有任务鱼。
        /// </summary>
        /// <param name="player">用来判定生物群系（和原版一样看玩家位置）。</param>
        /// <param name="attempt">提供高度档位与蜂蜜判定。</param>
        /// <param name="allowHardmodeFish">false 时，肉前会排除肉后专属的任务鱼。</param>
        public static List<int> GetCandidates(Player player, FishingAttempt attempt, bool allowHardmodeFish)
        {
            List<int> result = new List<int>(8);
            if (player == null) return result;

            int height = attempt.heightLevel;
            bool hardmode = Main.hardMode;

            foreach (Entry entry in Entries)
            {
                if (entry.HardmodeOnly && !hardmode && !allowHardmodeFish) continue;
                if (height < entry.MinHeight || height > entry.MaxHeight) continue;
                if (!BiomeMatches(entry.Biome, player, attempt)) continue;

                result.Add(entry.Type);
            }

            return result;
        }

        /// <summary>生物群系是否匹配（口径与 wiki 表格一致）。</summary>
        private static bool BiomeMatches(Biome biome, Player player, FishingAttempt attempt)
        {
            switch (biome)
            {
                case Biome.Pure: return player.ZonePurity;
                case Biome.Corruption: return player.ZoneCorrupt;
                case Biome.Crimson: return player.ZoneCrimson;
                case Biome.Jungle: return player.ZoneJungle;
                case Biome.Snow: return player.ZoneSnow;
                case Biome.Hallow: return player.ZoneHallow;
                case Biome.Desert: return player.ZoneDesert;
                case Biome.Ocean: return player.ZoneBeach;
                case Biome.Mushroom: return player.ZoneGlowshroom;
                case Biome.Honey: return attempt.inHoney;
                default: return false;
            }
        }
    }
}
