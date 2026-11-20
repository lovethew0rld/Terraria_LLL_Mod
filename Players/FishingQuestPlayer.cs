using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using LLL.Systems;

namespace LLL.Players
{
    /// <summary>
    /// 任务鱼相关的两条钓鱼调整。
    ///
    /// 【规则 A：任意水域 → 今天的任务鱼】
    ///   不再管你在哪个生物群系、哪个深度，只要钓鱼成功，就有机会钓到哥布林今天要的那条鱼。
    ///
    /// 【规则 B：任意时间 → 当前环境的任务鱼】
    ///   不再管今天哥布林要哪条，只要你在某个环境里，就有机会钓到"这个环境本来该有的"任务鱼
    ///   （比如在丛林里能钓到泥鱼，哪怕今天要的是沙漠的鱼）。
    ///
    /// 【怎么掷骰子】
    ///   完全沿用原版：只看 attempt.uncommon（"不常见"档）。
    ///   原版的任务鱼判定就藏在这一档里（if (attempt.uncommon && attempt.questFish == 某ID)），
    ///   所以这里的命中率天然随渔力浮动，和原版手感一致。
    ///
    /// 【两条规则同时开启时怎么选鱼】
    ///   骰子中了以后：
    ///     50% → 今天的任务鱼
    ///     50% → 当前环境匹配的其他任务鱼（等概率随机一条）
    ///   如果当前环境只有"今天这条"（排除后池子为空），则直接落到今天这条。
    ///
    /// 【不顶掉什么】
    ///   传说 / 极稀有 / 稀有 档的渔获不碰；宝匣不碰；原版已经给出的任务鱼也不碰。
    ///
    /// 【原版的三条限制全部放开】
    ///   已经持有该鱼、世界里没有哥布林、今天已经交过任务 —— 都不再阻止任务鱼出现。
    /// </summary>
    public class FishingQuestPlayer : ModPlayer
    {
        public override void CatchFish(FishingAttempt attempt, ref int itemDrop, ref int npcSpawn,
            ref AdvancedPopupRequest sonar, ref Vector2 sonarPosition)
        {
            // 只处理本地玩家（和模组其它功能保持一致，不考虑多人同步）
            if (Player.whoAmI != Main.myPlayer) return;

            LLLServerConfig config = ModContent.GetInstance<LLLServerConfig>();
            if (!config.EnableFishingQuestTweak) return;

            bool anyWater = config.QuestFishAnyWater;
            bool anyDay = config.QuestFishAnyDay;
            if (!anyWater && !anyDay) return;

            // 这一竿要有实际渔获，且不是"生成 NPC"（npcSpawn > 0 时 itemDrop 会被置 -1）
            if (npcSpawn > 0 || itemDrop <= 0) return;

            // 岩浆渔获另有体系，不参与
            if (attempt.inLava) return;

            // ─── 原版骰子：只在"不常见"档，且高稀有度没有先命中 ───
            if (!attempt.uncommon) return;
            if (attempt.rare || attempt.veryrare || attempt.legendary) return;

            // 宝匣不顶掉
            if (ItemID.Sets.IsFishingCrate[itemDrop]) return;

            // 原版已经给出任务鱼（今天那条 + 环境匹配），保持原样
            if (QuestFishTable.IsQuestFish(itemDrop)) return;

            int today = GetTodayQuestFish();
            if (today <= 0) return;

            int chosen = ChooseTarget(attempt, today, anyWater, anyDay, config.QuestFishIgnoreHardmode);
            if (chosen <= 0 || chosen == itemDrop) return;

            itemDrop = chosen;
        }

        /// <summary>
        /// 决定这一竿换成哪条任务鱼；返回 0 表示"这次不换"。
        /// </summary>
        private int ChooseTarget(FishingAttempt attempt, int today, bool anyWater, bool anyDay, bool ignoreHardmode)
        {
            // 只有规则 A：任何水域都出"今天那条"
            if (anyWater && !anyDay)
                return today;

            List<int> pool = QuestFishTable.GetCandidates(Player, attempt, ignoreHardmode);

            // 只有规则 B：只从当前环境的候选里等概率抽（包含今天的鱼，若它也符合环境）
            if (!anyWater)
            {
                if (pool.Count == 0) return 0;
                return pool[Main.rand.Next(pool.Count)];
            }

            // ─── 两条都开：50% 今天那条 / 50% 环境里的其它鱼 ───
            pool.Remove(today);

            // 环境里没有"其它"任务鱼时，整份概率都给今天这条
            if (pool.Count == 0) return today;

            if (Main.rand.Next(2) == 0) return today;
            return pool[Main.rand.Next(pool.Count)];
        }

        /// <summary>
        /// 哥布林今天要的鱼（`Main.anglerQuest` 是索引，`Main.anglerQuestItemNetIDs` 才是物品 ID）。
        /// </summary>
        private static int GetTodayQuestFish()
        {
            int[] table = Main.anglerQuestItemNetIDs;
            if (table == null) return 0;

            int index = Main.anglerQuest;
            if (index < 0 || index >= table.Length) return 0;

            return table[index];
        }
    }
}
