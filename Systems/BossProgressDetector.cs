using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using LLL.Players;

namespace LLL.Systems
{
    /// <summary>
    /// Boss 击杀检测器：只负责“发现玩家打死了某个不同的 Boss”，然后交给人物存档里的 CorePlayer 记录。
    ///
    /// 【为什么用两条腿】
    ///   ① GlobalNPC.OnKill —— 监听所有 NPC 死亡，用 npc.boss 筛出 Boss（模组 Boss 也算），
    ///      用 npc.type 去重。这是最直接的做法，绝大多数 Boss 都靠它。
    ///   ② 原版“已击败”标志轮询 —— 世吞/毁灭者这类**分段 Boss** 在死亡瞬间很难判断“谁是主体”，
    ///      所以再用游戏自己写的进度记录兜底：进世界时拍一张快照，之后只要有标志从 false 变 true，
    ///      就说明“这次游戏里刚打死了这只 Boss”。
    ///
    /// 两条腿都往同一个 HashSet 里塞 npc.type，自动去重，所以不会重复计数。
    /// 快照机制同时天然满足“不追究历史进度”：进这个世界之前就打过的 Boss 不算。
    /// </summary>
    public class BossProgressDetector : ModSystem
    {
        /// <summary>进世界那一刻，原版“已击败”标志的快照（只统计之后新翻转的）。</summary>
        private static readonly HashSet<int> _vanillaSnapshot = new HashSet<int>();

        /// <summary>认识的“原版 Boss”类型编号。分段 Boss 只登记头部即可（身体/尾巴会解析到头部）。</summary>
        private static readonly HashSet<int> VanillaBossTypes = new HashSet<int>
        {
            NPCID.KingSlime,
            NPCID.EyeofCthulhu,
            NPCID.EaterofWorldsHead,
            NPCID.BrainofCthulhu,
            NPCID.QueenBee,
            NPCID.SkeletronHead,
            NPCID.Deerclops,
            NPCID.WallofFlesh,
            NPCID.QueenSlimeBoss,
            NPCID.TheDestroyer,
            NPCID.Retinazer,
            NPCID.Spazmatism,
            NPCID.SkeletronPrime,
            NPCID.Plantera,
            NPCID.Golem,
            NPCID.DukeFishron,
            NPCID.HallowBoss,      // 光之女皇
            NPCID.CultistBoss,     // 拜月教邪教徒
            NPCID.MoonLordHead,
        };

        // ═══════════════════════════════════════════════════════════════
        // 生命周期
        // ═══════════════════════════════════════════════════════════════

        public override void ClearWorld()
        {
            _vanillaSnapshot.Clear();
        }

        public override void OnWorldLoad()
        {
            _vanillaSnapshot.Clear();
            foreach (int type in GetDefeatedVanillaBossTypes())
                _vanillaSnapshot.Add(type);
        }

        /// <summary>腿②：轮询原版进度标志，只认“本次进世界之后新翻转”的。</summary>
        public override void PostUpdateEverything()
        {
            if (Main.netMode == NetmodeID.Server) return;   // 进度存在人物身上，服务器端暂不处理

            foreach (int type in GetDefeatedVanillaBossTypes())
            {
                if (!_vanillaSnapshot.Add(type)) continue;   // 已在快照里 → 不是本次的进度
                RecordBoss(type);
            }
        }

        // ═══════════════════════════════════════════════════════════════
        // 腿①：NPC 死亡监听（由 BossDefeatTracker 转发进来）
        // ═══════════════════════════════════════════════════════════════

        public static void RegisterKill(NPC npc)
        {
            if (Main.netMode == NetmodeID.Server) return;
            if (npc == null || npc.friendly) return;
            if (!IsMainBody(npc)) return;                    // 部位不算（骷髅王的手、月总的部件…）

            // 是 Boss 吗？
            //   原版：只认“进度 Boss”名单（南瓜月/霜月/火星/旧日军团这类事件小 Boss 不算，避免刷事件拿进度）
            //   模组：有 ModNPC 就算（别的模组加的 Boss 也计入）
            bool isBoss = npc.ModNPC != null || VanillaBossTypes.Contains(npc.type);
            if (!isBoss) return;

            RecordBoss(npc.type);
        }

        /// <summary>主体判定：realLife 指向别人 = 部位；realLife 为 -1 或指向自己 = 主体。</summary>
        private static bool IsMainBody(NPC npc)
        {
            return npc.realLife < 0 || npc.realLife == npc.whoAmI;
        }

        // ═══════════════════════════════════════════════════════════════
        // 内部
        // ═══════════════════════════════════════════════════════════════

        /// <summary>登记到本地玩家的人物存档里；有新进度时打一行日志。</summary>
        private static void RecordBoss(int bossType)
        {
            CorePlayer player = Main.LocalPlayer.GetModPlayer<CorePlayer>();
            if (!player.RecordBoss(bossType)) return;

            ModContent.GetInstance<BossProgressDetector>().Mod.Logger.Info(
                $"[宇宙增幅] Boss 进度：{player.DefeatedBossCount}/{CorePlayer.FullUnlockBossCount} 个不同 Boss"
                + $"（本次记录类型 {bossType}，已解锁效果 [{string.Join(",", player.UnlockedEffectIds)}]）");
        }

        /// <summary>当前所有“已击败”的原版 Boss 类型编号。</summary>
        private static IEnumerable<int> GetDefeatedVanillaBossTypes()
        {
            if (NPC.downedSlimeKing) yield return NPCID.KingSlime;
            if (NPC.downedBoss1) yield return NPCID.EyeofCthulhu;
            if (NPC.downedBoss2) yield return WorldGen.crimson ? NPCID.BrainofCthulhu : NPCID.EaterofWorldsHead;
            if (NPC.downedBoss3) yield return NPCID.SkeletronHead;
            if (NPC.downedQueenBee) yield return NPCID.QueenBee;
            if (NPC.downedDeerclops) yield return NPCID.Deerclops;
            if (Main.hardMode) yield return NPCID.WallofFlesh;
            if (NPC.downedQueenSlime) yield return NPCID.QueenSlimeBoss;
            if (NPC.downedMechBoss1) yield return NPCID.TheDestroyer;
            if (NPC.downedMechBoss2) yield return NPCID.Retinazer;
            if (NPC.downedMechBoss3) yield return NPCID.SkeletronPrime;
            if (NPC.downedPlantBoss) yield return NPCID.Plantera;
            if (NPC.downedGolemBoss) yield return NPCID.Golem;
            if (NPC.downedFishron) yield return NPCID.DukeFishron;
            if (NPC.downedEmpressOfLight) yield return NPCID.HallowBoss;
            if (NPC.downedAncientCultist) yield return NPCID.CultistBoss;
            if (NPC.downedMoonlord) yield return NPCID.MoonLordHead;
        }
    }
}
