using Terraria;
using Terraria.ModLoader;
using LLL;

namespace LLL.NPCs
{
    /// <summary>
    /// 低血量斩杀系统：当NPC被玩家攻击（近战/射弹）后，
    /// 检查其当前生命值是否低于设定的斩杀线，若是则直接击杀。
    /// 支持Boss和普通怪物分别设置斩杀线。
    /// 由 LLLServerConfig 中的 EnableExecute、ExecutePercentage、
    /// EnableBossExecute、BossExecutePercentage 控制。
    /// </summary>
    public class ExecuteNPC : GlobalNPC
    {
        /// <summary>
        /// 被物品（近战/工具/消耗品）击中后触发
        /// </summary>
        public override void OnHitByItem(NPC npc, Player player, Item item, NPC.HitInfo hit, int damageDone)
        {
            TryExecute(npc);
        }

        /// <summary>
        /// 被射弹击中后触发
        /// </summary>
        public override void OnHitByProjectile(NPC npc, Projectile projectile, NPC.HitInfo hit, int damageDone)
        {
            TryExecute(npc);
        }

        /// <summary>
        /// 核心斩杀判断逻辑
        /// </summary>
        private void TryExecute(NPC npc)
        {
            var config = ModContent.GetInstance<LLLServerConfig>();

            // 1. 总开关检查
            if (!config.EnableExecute)
                return;

            // 2. 排除无效目标：不活跃、已死亡、无敌、友善NPC
            if (!npc.active || npc.life <= 0 || npc.dontTakeDamage || npc.friendly)
                return;

            // 3. 计算斩杀阈值
            float executeThreshold = 0f;

            if (npc.boss)
            {
                // Boss：单独开关和斩杀线
                if (config.EnableBossExecute && config.BossExecutePercentage > 0)
                {
                    // ? BossExecutePercentage 范围 1~100（百分比）
                    //    example: 5% → 0.05，阈值 = lifeMax * 0.05
                    executeThreshold = npc.lifeMax * (config.BossExecutePercentage / 100f);
                }
            }
            else
            {
                // 普通怪物
                if (config.ExecutePercentage > 0)
                {
                    // ? ExecutePercentage 范围 1~100（百分比）
                    executeThreshold = npc.lifeMax * (config.ExecutePercentage / 100f);
                }
            }

            // 4. 执行斩杀：当生命值 < 阈值时（注意是严格小于，防止反复触发）
            //    如果希望 <= 也可以改为 <=，但严格小于更符合直觉（低于百分比时才斩杀）
            if (executeThreshold > 0 && npc.life < executeThreshold)
            {
                npc.StrikeInstantKill(); // 瞬间击杀，不触发掉落修正，直接死亡
            }
        }
    }
}