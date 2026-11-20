using Terraria;
using Terraria.ModLoader;
using LLL;

namespace LLL.Systems
{
    /// <summary>
    /// 无敌帧控制系统：强制禁用玩家或NPC的受伤无敌帧。
    /// 
    /// 【无敌帧是什么？】
    /// 玩家受伤后会闪烁并短暂免疫后续伤害（原版约 0.67 秒/40帧）。
    /// NPC（怪物）受伤后也有短暂的免疫时间（防止被高频攻击瞬间秒杀）。
    /// 
    /// 禁用后：
    /// - 玩家受到连续攻击会快速掉血（高危！）
    /// - 怪物可以被高频武器（如迷你鲨）以极快速度击杀
    /// 
    /// 由 LLLServerConfig.DisablePlayerImmunity 和 DisableNPCImmunity 分别控制。
    /// ⚠️ 警告：禁用玩家无敌帧会大幅提高游戏难度，建议仅用于测试或特殊玩法。
    /// </summary>
    public class ImmunitySystem : ModSystem
    {
        /// <summary>
        /// 每帧执行，强制重置无敌帧状态。
        /// </summary>
        public override void PostUpdateEverything()
        {
            var config = ModContent.GetInstance<LLLServerConfig>();

            // ─── 禁用玩家无敌帧 ──────────────────────────────────────
            if (config.DisablePlayerImmunity)
            {
                for (int i = 0; i < Main.maxPlayers; i++)
                {
                    Player player = Main.player[i];
                    if (player.active)
                    {
                        // immuneTime：剩余无敌帧数（设为0表示立即结束）
                        // immune：无敌状态的布尔标志（设为false强制解除）
                        player.immuneTime = 0;
                        player.immune = false;
                    }
                }
            }

            // ─── 禁用NPC无敌帧 ──────────────────────────────────────
            if (config.DisableNPCImmunity)
            {
                for (int i = 0; i < Main.maxNPCs; i++)
                {
                    NPC npc = Main.npc[i];
                    if (npc.active)
                    {
                        // npc.immune[] 是一个数组，每个玩家对应一个槽位，
                        // 记录该玩家对当前NPC造成的无敌帧剩余值。
                        // 全部设为0即可让NPC可以被任何玩家的攻击连续命中。
                        for (int slot = 0; slot < npc.immune.Length; slot++)
                        {
                            npc.immune[slot] = 0;
                        }
                    }
                }
            }
        }
    }
}