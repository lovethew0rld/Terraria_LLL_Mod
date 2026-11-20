using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using LLL;
using LLL.Systems;

namespace LLL.Items
{
    /// <summary>
    /// 战利品倍率全局类。
    /// 当 NPC 被击杀掉落物品时，根据服务器配置的 LootMultiplier 值，
    /// 将物品数量翻倍（或乘以指定倍数）。
    /// 由 LLLServerConfig.LootMultiplier 控制。
    ///
    /// 【额外战利品的修饰语（前缀）】
    /// 只处理“额外生成的那几份”，原本掉落的那一份完全不动。
    /// 每个分类（近战与镐/斧/锤、远程、魔法、召唤、饰品）在配置里各有一个三档模式：
    ///     全部随机            —— 每个额外副本随机取一个修饰语（可负面；25% 概率轮空不加）
    ///     全部指定            —— 每个额外副本都用本分类的指定修饰语
    ///     首件指定其余随机    —— 额外副本里第 1 件用指定修饰语，其余按“全部随机”的规则
    /// 指定修饰语：近战与镐/斧/锤 = 传奇，远程 = 虚幻，魔法 = 神话，召唤 = 无情，饰品 = 护佑。
    /// 指定修饰语万一用不了（例如长矛拿不到传奇）会退化成随机。
    /// 只有能附魔的物品会参与：武器、带伤害的镐/斧/锤、饰品；鱼竿与时装不附魔。
    /// </summary>
    public class LootMultiplier : GlobalItem
    {
        /// <summary>
        /// 防止递归重复触发的标志。
        /// 因为 OnSpawn 中调用 Item.NewItem 会再次触发 OnSpawn，需防止无限循环。
        /// </summary>
        private static bool isSpawningDouble = false;

        /// <summary>
        /// 物品生成时的钩子（包括从宝箱、掉落、合成等来源）。
        /// 这里只处理 NPC 掉落来源的战利品。
        /// </summary>
        public override void OnSpawn(Item item, IEntitySource source)
        {
            // 若当前正在执行翻倍操作，直接返回以防递归
            if (isSpawningDouble) return;

            // 只处理 NPC 掉落来源
            if (source is not EntitySource_Loot lootSource || lootSource.Entity is not NPC npc) return;

            var config = ModContent.GetInstance<LLLServerConfig>();
            int lootFactor = config.LootMultiplier;

            // 倍率 <=1 或物品是硬币（钱币）则跳过
            if (lootFactor <= 1) return;
            if (item.IsACoin) return;

            // ==========================================
            // 不可堆叠物品（如武器、配饰）：逐个生成额外副本
            // 可堆叠物品（如药水、材料）：直接增加 stack 数量
            // ==========================================
            if (item.maxStack <= 1)
            {
                try
                {
                    isSpawningDouble = true; // 设置标志防止递归

                    // 生成 (lootFactor - 1) 个额外副本
                    for (int i = 0; i < lootFactor - 1; i++)
                    {
                        int extraIndex = Item.NewItem(source, npc.getRect(), item.type, item.stack);

                        // 只给“额外那几份”加修饰语；i 表示这是第几份额外副本（0 起，混合模式只看第 0 份）。
                        // 必须等 NewItem 返回之后再改：原版在 NewItem 内部会先做一次 Prefix(0) 初始化。
                        if (extraIndex >= 0 && extraIndex < Main.maxItems)
                            ApplyPrefixToExtraLoot(Main.item[extraIndex], i);
                    }
                }
                finally
                {
                    isSpawningDouble = false; // 确保标志重置
                }
            }
            else
            {
                // 可堆叠物品：直接乘以倍率
                item.stack *= lootFactor;
            }
        }

        // ═══════════════════════════════════════════════════════════════
        // 额外战利品的修饰语（前缀）
        // ═══════════════════════════════════════════════════════════════

        /// <summary>给“额外复制出来的那一份”战利品加修饰语。</summary>
        /// <param name="item">刚生成的额外副本。</param>
        /// <param name="extraOrder">这是第几份额外副本（0 起）。</param>
        private static void ApplyPrefixToExtraLoot(Item item, int extraOrder)
        {
            if (item == null || item.IsAir) return;

            LLLServerConfig config = ModContent.GetInstance<LLLServerConfig>();
            if (!config.PrefixExtraLoot) return;
            if (!CanHaveModifier(item)) return;

            ExtraLootPrefixMode mode = GetMode(item, config);

            // “全部指定” → 每份都用指定；“首件指定其余随机” → 只有第 0 份用指定
            bool useSpecified = mode == ExtraLootPrefixMode.AlwaysSpecified
                || (mode == ExtraLootPrefixMode.FirstSpecifiedRestRandom && extraOrder == 0);

            int prefix;
            if (useSpecified)
            {
                int specified = GetSpecifiedPrefix(item);

                // 这件物品拿不到指定修饰语（例如长矛拿不到传奇）→ 退化成随机
                prefix = PrefixRegistry.IsValid(item, specified)
                    ? specified
                    : PrefixRegistry.Roll(item);
            }
            else
            {
                // 全部随机：可能正面、可能负面、也可能轮空（返回 0，不加修饰语）
                prefix = PrefixRegistry.Roll(item);
            }

            if (prefix > 0)
                item.Prefix(prefix);
        }

        /// <summary>能附魔的物品：武器、带伤害的镐/斧/锤、饰品（鱼竿与时装不附魔）。</summary>
        private static bool CanHaveModifier(Item item)
        {
            if (item.vanity) return false;                                      // 时装 / 时装配饰
            if (item.accessory) return true;                                    // 饰品
            if (item.damage > 0) return true;                                   // 有伤害的武器与工具
            if (item.pick > 0 || item.axe > 0 || item.hammer > 0) return true;  // 万一有零伤害的工具
            return false;
        }

        /// <summary>取该物品所属分类在配置里选的模式。</summary>
        private static ExtraLootPrefixMode GetMode(Item item, LLLServerConfig config)
        {
            if (item.accessory)
                return config.ExtraLootAccessoryPrefix;

            // 鞭属于 SummonMeleeSpeed，和召唤物一起归到召唤
            if (item.CountsAsClass(DamageClass.Summon) || item.CountsAsClass(DamageClass.SummonMeleeSpeed))
                return config.ExtraLootSummonPrefix;

            if (item.CountsAsClass(DamageClass.Magic))
                return config.ExtraLootMagicPrefix;

            if (item.CountsAsClass(DamageClass.Ranged))
                return config.ExtraLootRangedPrefix;

            // 近战武器、镐/斧/锤、以及无法判断类型的兜底
            return config.ExtraLootMeleePrefix;
        }

        /// <summary>
        /// 本分类的“指定修饰语”：
        /// 近战与镐/斧/锤 = 传奇，远程 = 虚幻，魔法 = 神话，召唤 = 无情，饰品 = 护佑。
        /// </summary>
        private static int GetSpecifiedPrefix(Item item)
        {
            if (item.accessory)
                return PrefixID.Warding;      // 护佑

            if (item.CountsAsClass(DamageClass.Summon) || item.CountsAsClass(DamageClass.SummonMeleeSpeed))
                return PrefixID.Ruthless;     // 无情

            if (item.CountsAsClass(DamageClass.Magic))
                return PrefixID.Mythical;     // 神话

            if (item.CountsAsClass(DamageClass.Ranged))
                return PrefixID.Unreal;       // 虚幻

            return PrefixID.Legendary;        // 传奇
        }
    }
}
